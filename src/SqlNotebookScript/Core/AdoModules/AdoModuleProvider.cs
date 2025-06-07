using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;
using SqlNotebookScript.Core.ModuleDelegates;
using SqlNotebookScript.Core.SqliteInterop;
using SqlNotebookScript.Utils;
using static SqlNotebookScript.Core.SqliteInterop.NativeMethods;

namespace SqlNotebookScript.Core.AdoModules;

public sealed class PostgreSqlAdoModuleProvider : AdoModuleProvider
{
    protected override IDbConnection CreateConnection(string connectionString) =>
        new NpgsqlConnection(connectionString);

    protected override string SelectRandomSampleSql => "SELECT * FROM {0} ORDER BY RANDOM() LIMIT 5000;";
    protected override string SelectRandomSampleSqlFallback => null;
    protected override string ModuleName => "pgsql";
    protected override char EscapeChar => '"';
}

public sealed class SqlServerAdoModuleProvider : AdoModuleProvider
{
    protected override IDbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    protected override string SelectRandomSampleSql => "SELECT * FROM {0} TABLESAMPLE (5000 ROWS);";
    protected override string SelectRandomSampleSqlFallback => "SELECT TOP 5000 * FROM {0}";
    protected override string ModuleName => "mssql";
    protected override char EscapeChar => '"';
}

public sealed class MySqlAdoModuleProvider : AdoModuleProvider
{
    protected override IDbConnection CreateConnection(string connectionString) => new MySqlConnection(connectionString);

    protected override string SelectRandomSampleSql => "SELECT * FROM {0} ORDER BY RAND() LIMIT 5000;";
    protected override string SelectRandomSampleSqlFallback => null;
    protected override string ModuleName => "mysql";
    protected override char EscapeChar => '`';
}

public abstract class AdoModuleProvider : IDisposable
{
#if DEBUG
    private const bool DEBUG = true;
#else
    private const bool DEBUG = false;
#endif

    private static readonly UTF8Encoding _utf8 = new(false);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeDelegate(IntPtr p);

    private static readonly Lazy<(IntPtr Ptr, FreeDelegate Delegate)> _freeFunc = new(() =>
    {
        FreeDelegate @delegate = Marshal.FreeHGlobal;
        return (Marshal.GetFunctionPointerForDelegate(@delegate), @delegate);
    });

    protected static int _nextMetadataKey = 1;

    /// <summary>
    /// This matches the MetadataKey in <see cref="Sqlite3Vtab"/>.
    /// </summary>
    protected static readonly Dictionary<int, AdoTableMetadata> _tableMetadatas = new();

    /// <summary>
    /// This matches the MetadataKey in <see cref="Sqlite3VtabCursor"/>.
    /// </summary>
    protected static readonly Dictionary<int, AdoCursorMetadata> _cursorMetadatas = new();

    /// <summary>
    /// This matches the *pAux we receive in <see cref="AdoCreate"/>.
    /// </summary>
    protected static readonly Dictionary<int, AdoCreateInfo> _adoCreateInfos = new();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void RemoveCreateInfoDelegate(IntPtr p);

    private static readonly Lazy<(IntPtr Ptr, RemoveCreateInfoDelegate Delegate)> _removeCreateInfoFunc = new(() =>
    {
        RemoveCreateInfoDelegate @delegate = p =>
        {
            _adoCreateInfos.Remove((int)p);
        };
        return (Marshal.GetFunctionPointerForDelegate(@delegate), @delegate);
    });

    /// <summary>
    /// These are delegates for which we've called <see cref="Marshal.GetFunctionPointerForDelegate{TDelegate}(TDelegate)"/>.
    /// We are responsible for keeping them alive, so we'll stash them in this list.
    /// </summary>
    private static readonly List<object> _delegates = new();

    private bool _installed = false;
    private IntPtr _moduleNative; // sqlite3_module*
    private AdoCreateInfo _createInfo;
    private bool _disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // dispose managed state (managed objects)
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            if (_moduleNative != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_moduleNative);
            }
            _disposedValue = true;
        }
    }

    // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~AdoModuleProvider()
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

    public void Install(
        IntPtr sqlite // sqlite3*
    )
    {
        if (!_installed)
        {
            // Prepare the sqlite3_module.
            var moduleSize = Marshal.SizeOf<Sqlite3Module>();
            _moduleNative = Marshal.AllocHGlobal(moduleSize);
            try
            {
                ZeroMemory(_moduleNative, (IntPtr)moduleSize);
                AdoPopulateModule(_moduleNative);

                // Prepare the AdoCreateInfo.
                var createInfoKey = _nextMetadataKey++;
                _createInfo = new()
                {
                    ConnectionCreator = CreateConnection,
                    SelectRandomSampleSql = SelectRandomSampleSql,
                    SelectRandomSampleSqlFallback = SelectRandomSampleSqlFallback,
                    EscapeChar = EscapeChar,
                };
                _adoCreateInfos.Add(createInfoKey, _createInfo);

                // Install the module into SQLite.
                using NativeString nameNative = new(ModuleName);
                SqliteUtil.ThrowIfError(
                    sqlite,
                    sqlite3_create_module_v2(
                        sqlite,
                        nameNative.Ptr,
                        _moduleNative,
                        (IntPtr)createInfoKey,
                        _removeCreateInfoFunc.Value.Ptr
                    )
                );
            }
            catch
            {
                Marshal.FreeHGlobal(_moduleNative);
                throw;
            }

            _installed = true;
        }
    }

    protected abstract IDbConnection CreateConnection(string connectionString);
    protected abstract string SelectRandomSampleSql { get; }
    protected abstract string SelectRandomSampleSqlFallback { get; }
    protected abstract string ModuleName { get; }
    protected abstract char EscapeChar { get; }

    protected static void AdoPopulateModule(
        IntPtr modulePtr // sqlite3_module*
    )
    {
        ZeroMemory(modulePtr, (IntPtr)Marshal.SizeOf<Sqlite3Module>());
        var module = Marshal.PtrToStructure<Sqlite3Module>(modulePtr);

        module.iVersion = 1;
        {
            ModuleCreateDelegate x = AdoCreate;
            _delegates.Add(x);
            module.xCreate = module.xConnect = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleBestIndexDelegate x = AdoBestIndex;
            _delegates.Add(x);
            module.xBestIndex = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleDestroyDelegate x = AdoDestroy;
            _delegates.Add(x);
            module.xDestroy = module.xDisconnect = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleOpenDelegate x = AdoOpen;
            _delegates.Add(x);
            module.xOpen = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleCloseDelegate x = AdoClose;
            _delegates.Add(x);
            module.xClose = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleFilterDelegate x = AdoFilter;
            _delegates.Add(x);
            module.xFilter = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleNextDelegate x = AdoNext;
            _delegates.Add(x);
            module.xNext = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleEofDelegate x = AdoEof;
            _delegates.Add(x);
            module.xEof = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleColumnDelegate x = AdoColumn;
            _delegates.Add(x);
            module.xColumn = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleRowidDelegate x = AdoRowid;
            _delegates.Add(x);
            module.xRowid = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleRenameDelegate x = AdoRename;
            _delegates.Add(x);
            module.xRename = Marshal.GetFunctionPointerForDelegate(x);
        }

        Marshal.StructureToPtr(module, modulePtr, false);
    }

    private static int AdoCreate(
        IntPtr db, // sqlite3*
        IntPtr pAux, // void*
        int argc,
        IntPtr argv, // const char* const*
        IntPtr ppVTab, // sqlite3_vtab**
        IntPtr pzErr // char**
    )
    {
        // argv[3]: connectionString
        // argv[4]: table name
        var vtabNative = IntPtr.Zero; // AdoTable*
        var adoCreateInfo = _adoCreateInfos[(int)pAux];

        try
        {
            if (argc != 5 && argc != 6)
            {
                throw new Exception(
                    "Syntax: CREATE VIRTUAL TABLE <name> USING <driver> ('<connection string>', 'table name', ['schema name']);"
                );
            }

            var connectionString = TrimSingleQuote(GetArgvString(argv, 3));
            var adoTableName = TrimSingleQuote(GetArgvString(argv, 4));
            var adoSchemaName = argc == 6 ? TrimSingleQuote(GetArgvString(argv, 5)) : "";
            var adoQuotedCombinedName =
                adoSchemaName.Length > 0
                    ? $"{Quote(adoSchemaName, adoCreateInfo.EscapeChar)}.{Quote(adoTableName, adoCreateInfo.EscapeChar)}"
                    : Quote(adoTableName, adoCreateInfo.EscapeChar);
            var connection = adoCreateInfo.ConnectionCreator.Invoke(connectionString);
            connection.Open();

            // Ensure the table exists and detect the column names.
            List<string> columnNames = new();
            List<Type> columnTypes = new();
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {adoQuotedCombinedName} WHERE 1 = 0";
                using var reader = command.ExecuteReader();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    columnNames.Add(reader.GetName(i));
                    columnTypes.Add(reader.GetFieldType(i));
                }
            }

            // Create the sqlite3_vtab/AdoTable structure.
            int adoTableMetadataKey;
            {
                var adoTableSize = Marshal.SizeOf<Sqlite3Vtab>();
                adoTableMetadataKey = _nextMetadataKey++;
                vtabNative = Marshal.AllocHGlobal(adoTableSize); // matching free is in AdoDestroy
                ZeroMemory(vtabNative, (IntPtr)adoTableSize);
                var vtab = Marshal.PtrToStructure<Sqlite3Vtab>(vtabNative);
                vtab.MetadataKey = adoTableMetadataKey;
                Marshal.StructureToPtr(vtab, vtabNative, false);
            }

            // Take a sample of rows and compute some basic statistics.
            Dictionary<string, double> estimatedRowsPercentByColumn = new();
            int sampleSize = 0;
            {
                IDbCommand command = null;
                IDataReader reader = null;
                try
                {
                    command = connection.CreateCommand();
                    command.CommandText = adoCreateInfo.SelectRandomSampleSql.Replace("{0}", adoQuotedCombinedName);
                    reader = command.ExecuteReader();
                }
                catch (Exception ex)
                    when (ex.Message.Contains("TABLESAMPLE") && adoCreateInfo.SelectRandomSampleSqlFallback != null)
                {
                    // On MSSQL we can't use TABLESAMPLE for views, so detect that error and run the fallback instead.
                    command?.Dispose();
                    command = connection.CreateCommand();
                    command.CommandText = adoCreateInfo.SelectRandomSampleSqlFallback.Replace(
                        "{0}",
                        adoQuotedCombinedName
                    );
                    reader = command.ExecuteReader();
                }

                using (command)
                using (reader)
                {
                    var colCount = columnNames.Count;

                    // hash code => number of appearances of that hash in the column
                    var colDicts = new Dictionary<int, int>[colCount];
                    for (var i = 0; i < colCount; i++)
                    {
                        colDicts[i] = new();
                    }

                    var row = new object[colCount];
                    while (reader.Read())
                    {
                        sampleSize++;
                        reader.GetValues(row);
                        for (int i = 0; i < colCount; i++)
                        {
                            var colDict = colDicts[i];
                            int hash = row[i] == null ? 0 : row[i].GetHashCode();
                            if (colDict.TryGetValue(hash, out var count))
                            {
                                colDict[hash] = count + 1;
                            }
                            else
                            {
                                colDict[hash] = 1;
                            }
                        }
                    }

                    // This is the average number of rows we expect any arbitrary value to appear in the column.
                    // For instance, if the column is a list of 500 coin flips 0 or 1, an average around 250 is
                    // expected.
                    for (int i = 0; i < colCount; i++)
                    {
                        var name = columnNames[i];
                        var value = sampleSize > 0 ? (Enumerable.Average(colDicts[i].Values) / sampleSize) : 1.0;
                        estimatedRowsPercentByColumn[name] = value;
                    }
                }
            }

            _tableMetadatas.Add(
                adoTableMetadataKey,
                new()
                {
                    ConnectionString = connectionString,
                    AdoTableName = adoTableName,
                    AdoSchemaName = adoSchemaName,
                    ColumnNames = columnNames,
                    ConnectionCreator = adoCreateInfo.ConnectionCreator,
                    InitialRowCount = sampleSize,
                    EstimatedRowsPercentByColumn = estimatedRowsPercentByColumn,
                    EscapeChar = adoCreateInfo.EscapeChar,
                }
            );

            // Set the virtual table schema (CREATE TABLE statement).
            List<string> columnLines = new();
            for (int i = 0; i < columnNames.Count; i++)
            {
                var t = columnTypes[i];
                string sqlType;
                if (
                    t == typeof(short)
                    || t == typeof(int)
                    || t == typeof(long)
                    || t == typeof(byte)
                    || t == typeof(bool)
                )
                {
                    sqlType = "integer";
                }
                else if (t == typeof(float) || t == typeof(double) || t == typeof(decimal))
                {
                    sqlType = "real";
                }
                else
                {
                    sqlType = "text";
                }
                columnLines.Add("\"" + columnNames[i].Replace("\"", "\"\"") + "\" " + sqlType);
            }
            var createSql = "CREATE TABLE a (" + string.Join(", ", columnLines) + ")";
            using NativeString createSqlNative = new(createSql);
            SqliteUtil.ThrowIfError(db, sqlite3_declare_vtab(db, createSqlNative.Ptr));

            Marshal.WriteIntPtr(ppVTab, vtabNative);
            return SQLITE_OK;
        }
        catch (Exception ex)
        {
            if (vtabNative != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(vtabNative);
            }

            // Allocate an unmanaged error string using sqlite3_malloc
            var messageUtf8Bytes = _utf8.GetBytes(ex.GetErrorMessage());
            var messageNative = sqlite3_malloc(messageUtf8Bytes.Length + 1);
            Marshal.Copy(messageUtf8Bytes, 0, messageNative, messageUtf8Bytes.Length);
            Marshal.WriteByte(messageNative + messageUtf8Bytes.Length, 0);

            // Equivalent of: *pzErr = messageUnmanaged;
            // SQLite will take ownership of messageUnmanaged.
            Marshal.WriteIntPtr(pzErr, messageNative);

            return SQLITE_ERROR;
        }
    }

    private static int AdoDestroy(
        IntPtr pVTab // sqlite3_vtab*
    )
    {
        if (pVTab != IntPtr.Zero)
        {
            var vtab = Marshal.PtrToStructure<Sqlite3Vtab>(pVTab);
            _tableMetadatas.Remove(vtab.MetadataKey);

            Marshal.FreeHGlobal(pVTab);
        }

        return SQLITE_OK;
    }

    private static int AdoBestIndex(
        IntPtr pVTab, // sqlite3_vtab*
        IntPtr infoPtr // sqlite3_index_info*
    )
    {
        var vtab = Marshal.PtrToStructure<Sqlite3Vtab>(pVTab);
        var vtabMeta = _tableMetadatas[vtab.MetadataKey];
        var info = Marshal.PtrToStructure<Sqlite3IndexInfo>(infoPtr);

        // Build a query corresponding to the request.
        StringBuilder sb = new();
        sb.Append("SELECT * FROM ");
        var schemaName = vtabMeta.AdoSchemaName;
        var tableName = vtabMeta.AdoTableName;
        var adoQuotedCombinedName =
            schemaName.Length > 0
                ? $"{Quote(schemaName, vtabMeta.EscapeChar)}.{Quote(tableName, vtabMeta.EscapeChar)}"
                : Quote(tableName, vtabMeta.EscapeChar);
        sb.Append(adoQuotedCombinedName);

        // WHERE clause
        var argvIndex = 1;
        var estimatedRowsPercent = 1d;
        if (info.nConstraint > 0)
        {
            List<string> terms = new();
            for (int i = 0; i < info.nConstraint; i++)
            {
                // get info.aConstraint[i]
                var constraintPtr = info.aConstraint + i * Marshal.SizeOf<Sqlite3IndexConstraint>();
                var constraint = Marshal.PtrToStructure<Sqlite3IndexConstraint>(constraintPtr);

                if (constraint.iColumn == -1)
                {
                    continue; // rowid instead of a column. we don't support this type of constraint.
                }
                else if (constraint.usable == 0)
                {
                    continue;
                }

                string op;
                switch (constraint.op)
                {
                    case SQLITE_INDEX_CONSTRAINT_EQ:
                        op = " = ";
                        break;
                    case SQLITE_INDEX_CONSTRAINT_GT:
                        op = " > ";
                        break;
                    case SQLITE_INDEX_CONSTRAINT_LE:
                        op = " <= ";
                        break;
                    case SQLITE_INDEX_CONSTRAINT_LT:
                        op = " < ";
                        break;
                    case SQLITE_INDEX_CONSTRAINT_GE:
                        op = " >= ";
                        break;
                    case SQLITE_INDEX_CONSTRAINT_LIKE:
                        op = " LIKE ";
                        break;
                    default:
                        continue; // we don't support this operator
                }

                // set info.aConstraintUsage[i]
                Sqlite3IndexConstraintUsage constraintUsage = new() { argvIndex = argvIndex, omit = 1 };
                var constraintUsagePtr = info.aConstraintUsage + i * Marshal.SizeOf<Sqlite3IndexConstraintUsage>();
                Marshal.StructureToPtr(constraintUsage, constraintUsagePtr, false);

                var columnName = vtabMeta.ColumnNames[constraint.iColumn];
                terms.Add("\"" + columnName.Replace("\"", "\"\"") + "\"" + op + "@arg" + argvIndex);
                argvIndex++;

                estimatedRowsPercent *= vtabMeta.EstimatedRowsPercentByColumn[columnName];
            }
            if (terms.Count > 0)
            {
                sb.Append(" WHERE ");
                sb.Append(string.Join(" AND ", terms));
            }
        }

        // ORDER BY clause
        if (info.nOrderBy > 0)
        {
            sb.Append(" ORDER BY ");
            List<string> terms = new();
            for (int i = 0; i < info.nOrderBy; i++)
            {
                // get info.aOrderBy[i]
                var orderByPtr = info.aOrderBy + i * Marshal.SizeOf<Sqlite3IndexOrderBy>();
                var orderBy = Marshal.PtrToStructure<Sqlite3IndexOrderBy>(orderByPtr);

                terms.Add(vtabMeta.ColumnNames[orderBy.iColumn] + (orderBy.desc != 0 ? " DESC" : ""));
            }
            sb.Append(string.Join(", ", terms));
            info.orderByConsumed = 1;
        }

        info.idxNum = 0;
        info.idxStr = NativeString.NewUnmanagedUtf8StringWithSqliteAllocator(sb.ToString());
        info.needToFreeIdxStr = 1;
        info.estimatedRows = Math.Max(1, (long)(estimatedRowsPercent * vtabMeta.InitialRowCount));

        // the large constant is to make sure remote queries are always considered vastly more expensive than local
        // queries, while not affecting the relative cost of different remote query plans
        info.estimatedCost = (double)info.estimatedRows + 10000000;

        Marshal.StructureToPtr(info, infoPtr, false);
        return SQLITE_OK;
    }

    private static int AdoOpen(
        IntPtr pVTab, // sqlite3_vtab*
        IntPtr ppCursor // sqlite3_vtab_cursor**
    )
    {
        var metadataKey = ++_nextMetadataKey;

        // Prepare the cursor.
        IntPtr cursorNative;
        {
            var adoCursorSize = Marshal.SizeOf<Sqlite3VtabCursor>();
            cursorNative = Marshal.AllocHGlobal(adoCursorSize);
            ZeroMemory(cursorNative, (IntPtr)adoCursorSize);
            var cursor = Marshal.PtrToStructure<Sqlite3VtabCursor>(cursorNative);
            cursor.MetadataKey = metadataKey;
            Marshal.StructureToPtr(cursor, cursorNative, false);
        }

        // Prepare the cursor metadata.
        {
            var adoTable = Marshal.PtrToStructure<Sqlite3Vtab>(pVTab);
            var adoTableMetadata = _tableMetadatas[adoTable.MetadataKey];
            var connection = adoTableMetadata.ConnectionCreator(adoTableMetadata.ConnectionString);
            connection.Open();
            _cursorMetadatas.Add(metadataKey, new() { TableMetadata = adoTableMetadata, Connection = connection });
        }

        // Write the pointer to the Sqlite3VtabCursor to *ppCursor.
        Marshal.WriteIntPtr(ppCursor, cursorNative);
        return SQLITE_OK;
    }

    private static int AdoClose(
        IntPtr pCur // sqlite3_vtab_cursor*
    )
    {
        var adoCursor = Marshal.PtrToStructure<Sqlite3VtabCursor>(pCur);
        var adoCursorMetadata = _cursorMetadatas[adoCursor.MetadataKey];
        _cursorMetadatas.Remove(adoCursor.MetadataKey);

        // Don't block for these because it could take awhile.
        Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            adoCursorMetadata.Reader?.Dispose();
            adoCursorMetadata.Command?.Dispose();
            adoCursorMetadata.Connection?.Dispose();
            Debug.WriteLine($"Disposing ADO cursor took {stopwatch.Elapsed}");
        });

        Marshal.FreeHGlobal(pCur);
        return SQLITE_OK;
    }

    private static int AdoFilter(
        IntPtr pCur, // sqlite3_vtab_cursor*
        int idxNum,
        IntPtr idxStr, // const char*
        int argc,
        IntPtr argv // sqlite3_value**
    )
    {
        var sql = Marshal.PtrToStringUTF8(idxStr);
        var cursor = Marshal.PtrToStructure<Sqlite3VtabCursor>(pCur);
        var vtab = Marshal.PtrToStructure<Sqlite3Vtab>(cursor.pVtab);

        try
        {
            var cursorMetadata = _cursorMetadatas[cursor.MetadataKey];
            var args = new object[argc];

            for (int i = 0; i < argc; i++)
            {
                var argvI = argv + i * IntPtr.Size;
                args[i] = SqlUtil.GetArg(Marshal.ReadIntPtr(argvI));
            }

            cursorMetadata.Reader?.Dispose();
            cursorMetadata.Reader = null;

            if (cursorMetadata.ReaderSql != null && sql == cursorMetadata.ReaderSql)
            {
                // sqlite is issuing new arguments for the same statement
                for (int i = 0; i < argc; i++)
                {
                    var parameter = (IDataParameter)cursorMetadata.Command.Parameters[i];
                    parameter.Value = args[i];
                }
            }
            else
            {
                // brand new statement
                cursorMetadata.Command?.Dispose();
                cursorMetadata.Command = cursorMetadata.Connection.CreateCommand();
                cursorMetadata.Command.CommandText = sql;

                for (int i = 0; i < argc; i++)
                {
                    var varName = "@arg" + (i + 1).ToString();
                    var parameter = cursorMetadata.Command.CreateParameter();
                    parameter.ParameterName = varName;
                    parameter.Value = args[i];
                    cursorMetadata.Command.Parameters.Add(parameter);
                }
            }

            // Run ExecuteReader() on the thread pool so we can respond to sqlite interruption here by walking away.
            var readerTask = Task.Run(() => cursorMetadata.Command.ExecuteReader());

            while (!readerTask.IsCompleted)
            {
                if (Notebook.CancelInProgress)
                {
                    return SQLITE_INTERRUPT;
                }
                Thread.Sleep(0);
            }

            cursorMetadata.Reader = readerTask.GetAwaiter().GetResult();
            cursorMetadata.ReaderSql = sql;
            cursorMetadata.IsEof = !cursorMetadata.Reader.Read();
            return SQLITE_OK;
        }
        catch (Exception ex)
        {
            Notebook.SqliteVtabErrorMessage = ex.GetExceptionMessage();
            return SQLITE_ERROR;
        }
    }

    private static int AdoNext(
        IntPtr pCur // sqlite3_vtab_cursor*
    )
    {
        var cursor = Marshal.PtrToStructure<Sqlite3VtabCursor>(pCur);
        var cursorMetadata = _cursorMetadatas[cursor.MetadataKey];
        cursorMetadata.IsEof = !cursorMetadata.Reader.Read();
        return SQLITE_OK;
    }

    private static int AdoEof(
        IntPtr pCur // sqlite3_vtab_cursor*
    )
    {
        var cursor = Marshal.PtrToStructure<Sqlite3VtabCursor>(pCur);
        var cursorMetadata = _cursorMetadatas[cursor.MetadataKey];
        return cursorMetadata.IsEof ? 1 : 0;
    }

    private static void ResultText16(
        IntPtr ctx, // sqlite3_context*
        string str
    )
    {
        var wstrUnmanaged = Marshal.StringToHGlobalUni(str);
        var lenB = str.Length * 2;
        sqlite3_result_text16(ctx, wstrUnmanaged, lenB, _freeFunc.Value.Ptr);
    }

    private static int AdoColumn(
        IntPtr pCur, // sqlite3_vtab_cursor*
        IntPtr ctx, // sqlite3_context*
        int n
    )
    {
        try
        {
            var cursor = Marshal.PtrToStructure<Sqlite3VtabCursor>(pCur);
            var cursorMetadata = _cursorMetadatas[cursor.MetadataKey];
            if (cursorMetadata.IsEof)
            {
                return SQLITE_ERROR;
            }
            var type = cursorMetadata.Reader.GetFieldType(n);
            if (cursorMetadata.Reader.IsDBNull(n))
            {
                sqlite3_result_null(ctx);
            }
            else if (type == typeof(short))
            {
                sqlite3_result_int(ctx, cursorMetadata.Reader.GetInt16(n));
            }
            else if (type == typeof(int))
            {
                sqlite3_result_int(ctx, cursorMetadata.Reader.GetInt32(n));
            }
            else if (type == typeof(long))
            {
                sqlite3_result_int64(ctx, cursorMetadata.Reader.GetInt64(n));
            }
            else if (type == typeof(byte))
            {
                sqlite3_result_int(ctx, cursorMetadata.Reader.GetByte(n));
            }
            else if (type == typeof(float))
            {
                sqlite3_result_double(ctx, cursorMetadata.Reader.GetFloat(n));
            }
            else if (type == typeof(double))
            {
                sqlite3_result_double(ctx, cursorMetadata.Reader.GetDouble(n));
            }
            else if (type == typeof(decimal))
            {
                sqlite3_result_double(ctx, (double)cursorMetadata.Reader.GetDecimal(n));
            }
            else if (type == typeof(string))
            {
                ResultText16(ctx, cursorMetadata.Reader.GetString(n));
            }
            else if (type == typeof(char))
            {
                ResultText16(ctx, new string(cursorMetadata.Reader.GetChar(n), 1));
            }
            else if (type == typeof(bool))
            {
                sqlite3_result_int(ctx, cursorMetadata.Reader.GetBoolean(n) ? 1 : 0);
            }
            else if (type == typeof(DateTime))
            {
                ResultText16(ctx, cursorMetadata.Reader.GetDateTime(n).ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }
            else if (type == typeof(DateTimeOffset))
            {
                ResultText16(
                    ctx,
                    ((DateTimeOffset)cursorMetadata.Reader.GetValue(n)).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
                );
            }
            else
            {
                ResultText16(ctx, cursorMetadata.Reader.GetValue(n).ToString());
            }
            return SQLITE_OK;
        }
        catch
        {
            return SQLITE_ERROR;
        }
    }

    private static int AdoRowid(
        IntPtr pCur, // sqlite3_vtab_cursor*
        IntPtr pRowid // sqlite3_int64*
    )
    {
        var cursor = Marshal.PtrToStructure<Sqlite3VtabCursor>(pCur);
        var cursorMetadata = _cursorMetadatas[cursor.MetadataKey];
        var values = new object[cursorMetadata.TableMetadata.ColumnNames.Count];
        cursorMetadata.Reader.GetValues(values);
        long hash = 13;
        foreach (var value in values)
        {
            hash = (hash * 7) + value.GetHashCode();
        }

        Marshal.WriteIntPtr(pRowid, (IntPtr)hash); // *pRowid = hash;
        return SQLITE_OK;
    }

    private static int AdoRename(
        IntPtr pVtab, // sqlite3_vtab*
        IntPtr zNew // const char*
    )
    {
        // don't care
        return SQLITE_OK;
    }

    private static string GetArgvString(IntPtr argv, int index)
    {
        // argv is const char**, i.e. an array of string pointers
        var stringPtr = argv + index * IntPtr.Size; // char*
        return Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(stringPtr));
    }

    private static string TrimSingleQuote(string s)
    {
        var start = s.StartsWith("'");
        var end = s.EndsWith("'");
        if (start && end)
        {
            return s[1..^1];
        }
        else if (start)
        {
            return s[1..];
        }
        else if (end)
        {
            return s[..^1];
        }
        else
        {
            return s;
        }
    }

    private static string Quote(string s, char ch) => ch + s.Replace($"{ch}", $"{ch}{ch}") + ch;
}
