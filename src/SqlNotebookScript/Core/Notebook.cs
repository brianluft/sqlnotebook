using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SqlNotebookScript.Core.AdoModules;
using SqlNotebookScript.Core.GenericModules;
using SqlNotebookScript.Core.SqliteInterop;
using SqlNotebookScript.DataTables;
using SqlNotebookScript.Utils;
using static SqlNotebookScript.Core.SqliteInterop.NativeMethods;

namespace SqlNotebookScript.Core;

public sealed class Notebook : IDisposable
{
    private const int CURRENT_FILE_VERSION = 2;

    // We disable all synchronization in SQLite, so this lock protects any call into SQLite.
    private static readonly object _lock = new();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ExecuteGenericFunctionDelegate(IntPtr a, int b, IntPtr c);

    private static readonly Lazy<(IntPtr Ptr, ExecuteGenericFunctionDelegate Delegate)> _executeGenericFunctionFunc =
        new(() =>
        {
            ExecuteGenericFunctionDelegate @delegate = ExecuteGenericFunction;
            return (Marshal.GetFunctionPointerForDelegate(@delegate), @delegate);
        });

    // Just hang onto these. They are used as unmanaged callbacks.
    private readonly List<object> _delegates = new();

    private bool _disposedValue;
    private List<AdoModuleProvider> _adoModuleProviders = new();
    private List<GenericModuleProvider> _genericModuleProviders = new();
    public string OriginalFilePath { get; set; }
    private readonly string _workingCopyFilePath;
    private IntPtr _sqlite; // sqlite3*

    public NotebookUserData UserData { get; set; }

    // This is the last error message thrown by user code.
    public static string ErrorMessage { get; set; }

    // If the AdoModuleProvider gets an error with an underlying query, there's no way to pass it up through SQLite.
    // So SQLite returns a generic logic error and we store a more specific error message here.
    public static string SqliteVtabErrorMessage { get; set; }

    public static bool CancelInProgress { get; set; } = false;

    public static Notebook New() => new(null, true, null, CancellationToken.None);

    public static Notebook Open(
        string filePath,
        Action<int> onPercentComplete = null,
        CancellationToken cancel = default
    ) => new(filePath, false, onPercentComplete, cancel);

    private Notebook(string filePath, bool isNew, Action<int> onPercentComplete, CancellationToken cancel)
    {
        _workingCopyFilePath = NotebookTempFiles.GetTempFilePath(".working-copy");
        if (!isNew)
        {
            try
            {
                CopyFile(filePath, _workingCopyFilePath, onPercentComplete, cancel);
            }
            catch
            {
                File.Delete(_workingCopyFilePath);
                throw;
            }
        }

        OriginalFilePath = filePath;
        Init();
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // dispose managed state (managed objects)
                UserData.Dispose();
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            if (_sqlite != IntPtr.Zero)
            {
                lock (_lock)
                {
                    var result = sqlite3_close(_sqlite);
                    Debug.Assert(result == SQLITE_OK);
                }
                _sqlite = IntPtr.Zero;
            }

            foreach (var x in _adoModuleProviders)
            {
                x.Dispose();
            }
            _adoModuleProviders = null;

            foreach (var x in _genericModuleProviders)
            {
                x.Dispose();
            }
            _genericModuleProviders = null;

            UserData = null;

            _disposedValue = true;
        }
    }

    ~Notebook()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private readonly record struct CustomFunctions(
        List<CustomTableFunction> CustomTableFunctions,
        List<CustomScalarFunction> CustomScalarFunctions
    );

    private static CustomFunctions FindCustomFunctions()
    {
        var customTableFunctionType = typeof(CustomTableFunction);
        var customScalarFunctionType = typeof(CustomScalarFunction);

        List<CustomTableFunction> customTableFunctions = new();
        List<CustomScalarFunction> customScalarFunctions = new();

        foreach (var type in typeof(CustomTableFunction).Assembly.GetExportedTypes())
        {
            if (!type.IsAbstract)
            {
                if (type.IsAssignableTo(customTableFunctionType))
                {
                    customTableFunctions.Add((CustomTableFunction)Activator.CreateInstance(type));
                }
                else if (type.IsAssignableTo(customScalarFunctionType))
                {
                    customScalarFunctions.Add((CustomScalarFunction)Activator.CreateInstance(type));
                }
            }
        }

        return new(customTableFunctions, customScalarFunctions);
    }

    // See https://sqlite.org/c3ref/initialize.html
    public static void InitSqlite()
    {
        lock (_lock)
        {
            SqliteUtil.ThrowIfError(IntPtr.Zero, sqlite3_initialize());
        }
    }

    public static void ShutdownSqlite()
    {
        lock (_lock)
        {
            SqliteUtil.ThrowIfError(IntPtr.Zero, sqlite3_shutdown());
        }
    }

    private void Init()
    {
        lock (_lock)
        {
            FileVersionMigrator.MigrateIfNeeded(_workingCopyFilePath);

            using NativeString filePathNative = new(_workingCopyFilePath);
            using NativeBuffer sqliteNative = new(IntPtr.Size);
            SqliteUtil.ThrowIfError(IntPtr.Zero, sqlite3_open(filePathNative.Ptr, sqliteNative.Ptr));
            _sqlite = Marshal.ReadIntPtr(sqliteNative.Ptr); // sqlite3*
            SqliteUtil.ThrowIfError(_sqlite, sqlite3_enable_load_extension(_sqlite, 1));

            LoadExtension(_sqlite, "crypto.dll");
            LoadExtension(_sqlite, "fuzzy.dll");
            LoadExtension(_sqlite, "stats.dll");

            SqliteUtil.ThrowIfError(IntPtr.Zero, sqlite3_series_init(_sqlite, IntPtr.Zero, IntPtr.Zero));
            SqliteUtil.ThrowIfError(IntPtr.Zero, sqlite3_uuid_init(_sqlite, IntPtr.Zero, IntPtr.Zero));

            foreach (var x in _adoModuleProviders)
            {
                x.Dispose();
            }
            _adoModuleProviders.Clear();
            _adoModuleProviders.Add(new MySqlAdoModuleProvider());
            _adoModuleProviders.Add(new PostgreSqlAdoModuleProvider());
            _adoModuleProviders.Add(new SqlServerAdoModuleProvider());
            foreach (var x in _adoModuleProviders)
            {
                x.Install(_sqlite);
            }

            var (tableFunctions, scalarFunctions) = FindCustomFunctions();
            foreach (var tableFunction in tableFunctions)
            {
                GenericModuleProvider provider = new();
                provider.Install(_sqlite, tableFunction);
                _genericModuleProviders.Add(provider);
            }
            foreach (var scalarFunction in scalarFunctions)
            {
                RegisterGenericFunction(scalarFunction);
            }

            CustomFunctionsProvider.InstallCustomFunctions(_sqlite);

            UserData = NotebookUserData.Load(this);
            NotebookUserData.DropTables(this);
            Execute("DROP TABLE IF EXISTS _sqlnotebook_version;");
        }

        static void LoadExtension(IntPtr db, string filename)
        {
            using var process = Process.GetCurrentProcess();
            var filePath = Path.Combine(Path.GetDirectoryName(process.MainModule.FileName), filename);
            if (!File.Exists(filePath))
            {
                throw new Exception($"Extension library not found: \"{filename}\".");
            }
            using NativeString filePathNative = new(filePath);
            using NativeBuffer errMsgPtrBuf = new(Marshal.SizeOf<IntPtr>());
            if (sqlite3_load_extension(db, filePathNative.Ptr, IntPtr.Zero, errMsgPtrBuf.Ptr) == SQLITE_OK)
            {
                return;
            }

            var errMsgPtr = Marshal.ReadIntPtr(errMsgPtrBuf.Ptr);
            var errMsg = Marshal.PtrToStringUTF8(errMsgPtr);
            sqlite3_free(errMsgPtr);
            throw new Exception(errMsg.Trim());
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate object GenericFunctionExecuteDelegate(IReadOnlyList<object> args);

    private void RegisterGenericFunction(CustomScalarFunction scalarFunction)
    {
        using NativeString nameNative = new(scalarFunction.Name);

        GenericFunctionExecuteDelegate @delegate = scalarFunction.Execute;
        _delegates.Add(@delegate);

        SqliteUtil.ThrowIfError(
            _sqlite,
            sqlite3_create_function_v2(
                db: _sqlite,
                zFunc: nameNative.Ptr,
                nArg: scalarFunction.ParamCount,
                enc: SQLITE_UTF16 | (scalarFunction.IsDeterministic ? SQLITE_DETERMINISTIC : 0),
                p: Marshal.GetFunctionPointerForDelegate(@delegate),
                xSFunc: _executeGenericFunctionFunc.Value.Ptr,
                xStep: IntPtr.Zero,
                xFinal: IntPtr.Zero,
                xDestroy: IntPtr.Zero
            )
        );
    }

    private static void ExecuteGenericFunction(IntPtr ctx, int argc, IntPtr argv)
    {
        try
        {
            var @delegate = Marshal.GetDelegateForFunctionPointer<GenericFunctionExecuteDelegate>(
                sqlite3_user_data(ctx)
            );
            List<object> args = new();
            for (var i = 0; i < argc; i++)
            {
                args.Add(SqlUtil.GetArg(Marshal.ReadIntPtr(argv + i * IntPtr.Size)));
            }
            var result = @delegate(args);
            SqliteUtil.Result(ctx, result);
        }
        catch (Exception ex)
        {
            NativeString messageNative16 = new(ex.GetExceptionMessage(), utf16: true);
            sqlite3_result_error16(ctx, messageNative16.Ptr, -1);
        }
    }

    public void SaveAs(string filePath, Action<int> onPercentComplete = null, CancellationToken cancel = default)
    {
        lock (_lock)
        {
            OriginalFilePath = filePath;
            Save(onPercentComplete, cancel);
        }
    }

    public void Save(Action<int> onPercentComplete = null, CancellationToken cancel = default)
    {
        lock (_lock)
        {
            Debug.Assert(OriginalFilePath != null);
            if (IsTransactionActive())
            {
                throw new Exception(
                    "A transaction is active. Execute either \"COMMIT\" or \"ROLLBACK\" to end the transaction before saving."
                );
            }

            UserData.Save(this);
            WriteFileVersion();

            SqliteUtil.ThrowIfError(_sqlite, sqlite3_close(_sqlite));
            _sqlite = IntPtr.Zero;

            try
            {
                var tempFilePath = OriginalFilePath + ".tmp";
                try
                {
                    CopyFile(_workingCopyFilePath, tempFilePath, onPercentComplete, cancel);
                    File.Move(tempFilePath, OriginalFilePath, true);
                }
                finally
                {
                    File.Delete(tempFilePath);
                }
            }
            finally
            {
                Init();
            }
        }
    }

    private static void CopyFile(string src, string dst, Action<int> onPercentComplete, CancellationToken cancel)
    {
        using var inputStream = File.Open(src, FileMode.Open, FileAccess.Read, FileShare.Read);
        var totalBytes = inputStream.Length;
        using var outputStream = File.Create(dst);

        void Produce(byte[] buffer, out int batchCount)
        {
            batchCount = inputStream.Read(buffer, 0, buffer.Length);
        }

        void Consume(byte[] buffer, int batchCount, long totalSoFar)
        {
            outputStream.Write(buffer, 0, batchCount);
            onPercentComplete?.Invoke((int)(totalSoFar * 100 / totalBytes));
        }

        OverlappedProducerConsumer.Go(() => new byte[8 * 1_048_576], Produce, Consume, cancel);
    }

    private void WriteFileVersion()
    {
        Execute("CREATE TABLE IF NOT EXISTS _sqlnotebook_version (version);");
        Execute("DELETE FROM _sqlnotebook_version;");
        Execute(
            "INSERT INTO _sqlnotebook_version (version) VALUES (@version);",
            new Dictionary<string, object> { ["@version"] = CURRENT_FILE_VERSION }
        );
    }

    public void Execute(string sql)
    {
        Execute(sql, Array.Empty<object>());
    }

    private static IReadOnlyDictionary<string, object> ToLowercaseKeys(IReadOnlyDictionary<string, object> dict)
    {
        var allLowercase = true;
        foreach (var key in dict.Keys)
        {
            if (key != key.ToLowerInvariant())
            {
                allLowercase = false;
                break;
            }
        }

        if (allLowercase)
        {
            return dict;
        }
        else
        {
            Dictionary<string, object> newDict = new();
            foreach (var pair in dict)
            {
                newDict.Add(pair.Key.ToLowerInvariant(), pair.Value);
            }
            return newDict;
        }
    }

    public void Execute(string sql, IReadOnlyDictionary<string, object> args)
    {
        using var sdt = QueryCore(sql, ToLowercaseKeys(args), null, false, _sqlite, GetCancelling, null);
    }

    public void Execute(string sql, IReadOnlyList<object> args)
    {
        using var sdt = QueryCore(sql, null, args, false, _sqlite, GetCancelling, null);
    }

    public SimpleDataTable Query(string sql)
    {
        return Query(sql, Array.Empty<object>());
    }

    public SimpleDataTable Query(string sql, IReadOnlyDictionary<string, object> args, Action onRow = null)
    {
        return QueryCore(sql, ToLowercaseKeys(args), null, true, _sqlite, GetCancelling, onRow);
    }

    public SimpleDataTable Query(string sql, IReadOnlyList<object> args)
    {
        return QueryCore(sql, null, args, true, _sqlite, GetCancelling, null);
    }

    public object QueryValue(string sql) => QueryValue(sql, Array.Empty<object>());

    public object QueryValue(string sql, IReadOnlyDictionary<string, object> args)
    {
        using var sdt = Query(sql, args);
        return GetSingleValue(sdt);
    }

    public object QueryValue(string sql, IReadOnlyList<object> args)
    {
        using var sdt = Query(sql, args);
        return GetSingleValue(sdt);
    }

    private static object GetSingleValue(SimpleDataTable dt) =>
        dt.Rows.Count == 1 && dt.Columns.Count == 1 ? dt.Rows[0].GetValue(0) : null;

    public PreparedStatement Prepare(string sql) => new(_sqlite, sql);

    private static SimpleDataTable QueryCore(
        string sql,
        IReadOnlyDictionary<string, object> namedArgs,
        IReadOnlyList<object> orderedArgs,
        bool returnResult,
        IntPtr db, // sqlite3*
        Func<bool> cancelling,
        Action onRow
    )
    {
        if (cancelling != null && cancelling())
        {
            throw new OperationCanceledException();
        }

        lock (_lock)
        {
            using PreparedStatement statement = new(db, sql);
            var argArray =
                namedArgs != null ? statement.GetArgs(namedArgs)
                : orderedArgs is object[] a ? a
                : orderedArgs.ToArray();
            return statement.Execute(argArray, returnResult, onRow, CancellationToken.None);
        }
    }

    public string GetFilePath()
    {
        return OriginalFilePath;
    }

    public static IReadOnlyList<Token> Tokenize(string input)
    {
        lock (_lock)
        {
            using NativeBuffer scratch = new(IntPtr.Size);
            List<Token> list = new();
            using NativeString inputNative = new(input);
            var tokenType = 0;
            var oldPos = 0;
            var pos = 0;
            var len = inputNative.ByteCount;
            while ((tokenType = GetToken(inputNative.Ptr, ref oldPos, ref pos, len, scratch)) > 0)
            {
                // Grab the substring from 'oldPos' to 'pos'
                var utf8TokenLength = pos - oldPos;
                var utf8TokenBytes = new byte[utf8TokenLength];
                Marshal.Copy(inputNative.Ptr + oldPos, utf8TokenBytes, 0, utf8TokenLength);
                var tokenText = Encoding.UTF8.GetString(utf8TokenBytes);

                list.Add(
                    new()
                    {
                        Type = (TokenType)tokenType,
                        Text = tokenText,
                        Utf8Start = (ulong)oldPos,
                        Utf8Length = (ulong)(pos - oldPos),
                    }
                );

                oldPos = pos;
            }
            return list;
        }
    }

    private static int GetToken(
        IntPtr str, // const char*
        ref int oldPos,
        ref int pos,
        int len,
        NativeBuffer scratch // IntPtr sized buffer
    )
    {
        const int TK_SPACE = (int)TokenType.Space;
        if (pos >= len)
        {
            return 0;
        }
        int tokenType,
            tokenLen;
        do
        {
            tokenLen = SxGetToken(str + pos, scratch.Ptr);
            tokenType = (int)Marshal.ReadIntPtr(scratch.Ptr);
            oldPos = pos;
            pos += tokenLen;
        } while (tokenType == TK_SPACE && pos < len);

        return tokenType == TK_SPACE ? 0 : tokenType;
    }

    public static void ThrowIfCancelRequested()
    {
        if (CancelInProgress)
        {
            throw new OperationCanceledException();
        }
    }

    public void BeginUserCancel()
    {
        CancelInProgress = true;
        sqlite3_interrupt(_sqlite);
    }

    public void EndUserCancel()
    {
        CancelInProgress = false;
    }

    // Perform a synchronous action while monitoring our cancellation status and passing it via CTS if it happens.
    public static void WithCancellationToken(Action<CancellationToken> action)
    {
        using CancellationTokenSource cts = new();
        Thread monitorThread = new(
            new ThreadStart(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    if (CancelInProgress)
                    {
                        cts.Cancel();
                        break;
                    }
                    cts.Token.WaitHandle.WaitOne(16);
                }
            })
        );
        monitorThread.Start();

        try
        {
            action(cts.Token);
        }
        finally
        {
            cts.Cancel();
            monitorThread.Join();
        }
    }

    public IReadOnlyDictionary<string, string> GetScripts()
    {
        lock (_lock)
        {
            Dictionary<string, string> dict = new();
            foreach (var item in UserData.Items)
            {
                if (item is ScriptNotebookItemRecord script)
                {
                    dict.Add(item.Name.ToLowerInvariant(), script.Sql);
                }
            }
            return dict;
        }
    }

    public bool IsTransactionActive()
    {
        lock (_lock)
        {
            return sqlite3_get_autocommit(_sqlite) == 0;
        }
    }

    private bool GetCancelling() => CancelInProgress;
}
