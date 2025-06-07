using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SqlNotebookScript.Core.SqliteInterop;
using SqlNotebookScript.DataTables;
using SqlNotebookScript.Utils;
using static SqlNotebookScript.Core.SqliteInterop.NativeMethods;

namespace SqlNotebookScript.Core;

public sealed class PreparedStatement : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeDelegate(IntPtr p);

    private static readonly Lazy<(IntPtr Ptr, FreeDelegate Delegate)> _freeFunc = new(() =>
    {
        FreeDelegate @delegate = Marshal.FreeHGlobal;
        return (Marshal.GetFunctionPointerForDelegate(@delegate), @delegate);
    });

    private readonly IntPtr _sqlite; // sqlite3* - do not dispose
    private readonly string _sql;
    private readonly IntPtr _stmt; // sqlite3_stmt* - dispose
    private readonly int _paramCount;
    private readonly List<string> _paramNames;
    private bool _disposedValue;

    public PreparedStatement(IntPtr sqlite, string sql)
    {
        _sqlite = sqlite;
        _sql = sql;
        using NativeString sqlNative = new(sql);
        using NativeBuffer stmtNative = new(IntPtr.Size);
        SqliteUtil.ThrowIfError(
            sqlite,
            sqlite3_prepare_v2(sqlite, sqlNative.Ptr, sqlNative.ByteCount, stmtNative.Ptr, IntPtr.Zero),
            sql
        );
        _stmt = Marshal.ReadIntPtr(stmtNative.Ptr);
        if (_stmt == IntPtr.Zero)
        {
            throw new Exception("Invalid statement.");
        }
        _paramCount = sqlite3_bind_parameter_count(_stmt);
        _paramNames = new(_paramCount);
        for (var i = 1; i <= _paramCount; i++)
        {
            var paramNameNative = sqlite3_bind_parameter_name(_stmt, i);
            if (paramNameNative == IntPtr.Zero)
            {
                _paramNames.Add(null);
            }
            else
            {
                _paramNames.Add(Marshal.PtrToStringUTF8(paramNameNative));
            }
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // dispose managed state (managed objects)
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            if (_stmt != IntPtr.Zero)
            {
                _ = sqlite3_finalize(_stmt);
            }
            _disposedValue = true;
        }
    }

    // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~PreparedStatement()
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

    public object[] GetArgs(IReadOnlyDictionary<string, object> dict)
    {
        var args = new object[_paramCount];
        var provided = new bool[_paramCount];
        foreach (var pair in dict)
        {
            for (var paramIndex = 0; paramIndex < _paramCount; paramIndex++)
            {
                if (pair.Key.Equals(_paramNames[paramIndex], StringComparison.OrdinalIgnoreCase))
                {
                    args[paramIndex] = pair.Value;
                    provided[paramIndex] = true;
                    break;
                }
            }
        }
        for (var i = 0; i < _paramCount; i++)
        {
            if (!provided[i])
            {
                throw new Exception($"Missing value for parameter \"{_paramNames[i]}\".");
            }
        }
        return args;
    }

    public SimpleDataTable Execute(object[] args, bool returnResult, CancellationToken cancel) =>
        Execute(args, returnResult, null, cancel);

    public SimpleDataTable Execute(object[] args, bool returnResult, Action onRow, CancellationToken cancel)
    {
        if (args.Length != _paramCount)
        {
            throw new ArgumentOutOfRangeException(
                $"Expected {_paramCount} argument(s), but {args.Length} were provided."
            );
        }
        BindArguments(args);
        GetColumns(returnResult, out var columnNames, out var columnCount);
        using SimpleDataTableBuilder builder = new(columnNames);
        ReadResultsIntoTable(returnResult, columnCount, builder, out var fullCount, onRow, cancel);
        return builder.Build(fullCount);
    }

    public void ExecuteStream(
        object[] args,
        Action<List<string>> onHeader,
        Action<object[]> onRow,
        CancellationToken cancel
    )
    {
        if (args.Length != _paramCount)
        {
            throw new ArgumentOutOfRangeException(
                $"Expected {_paramCount} argument(s), but {args.Length} were provided."
            );
        }
        BindArguments(args);
        GetColumns(true, out var columnNames, out var columnCount);
        onHeader?.Invoke(columnNames);
        ReadResultsStream(columnCount, onRow, cancel);
    }

    public void ExecuteNoOutput(object[] args, CancellationToken cancel) => ExecuteStream(args, null, null, cancel);

    private void BindArguments(object[] args)
    {
        Notebook.SqliteVtabErrorMessage = null;
        SqliteUtil.ThrowIfError(_sqlite, sqlite3_reset(_stmt));
        SqliteUtil.ThrowIfError(_sqlite, sqlite3_clear_bindings(_stmt));
        for (var i = 1; i <= _paramCount; i++)
        {
            var value = args[i - 1];
            if (value == null)
            {
                SqliteUtil.ThrowIfError(_sqlite, sqlite3_bind_null(_stmt, i));
            }
            else
            {
                switch (value)
                {
                    case DBNull:
                        SqliteUtil.ThrowIfError(_sqlite, sqlite3_bind_null(_stmt, i));
                        break;
                    case int intValue:
                        SqliteUtil.ThrowIfError(_sqlite, sqlite3_bind_int(_stmt, i, intValue));
                        break;
                    case long longValue:
                        SqliteUtil.ThrowIfError(_sqlite, sqlite3_bind_int64(_stmt, i, longValue));
                        break;
                    case double dblValue:
                        SqliteUtil.ThrowIfError(_sqlite, sqlite3_bind_double(_stmt, i, dblValue));
                        break;
                    case byte[] arr:
                    {
                        // SQLite will take ownership of this blob and call our free function when it's done.
                        var copy = Marshal.AllocHGlobal(arr.Length);
                        Marshal.Copy(arr, 0, copy, arr.Length);
                        SqliteUtil.ThrowIfError(
                            _sqlite,
                            sqlite3_bind_blob64(_stmt, i, copy, (ulong)arr.Length, _freeFunc.Value.Ptr)
                        );
                        break;
                    }
                    default:
                    {
                        var strValue = value.ToString();
                        var bytes = Encoding.Unicode.GetBytes(strValue);

                        // SQLite will take ownership of this string and call our free function when it's done.
                        var strNative = Marshal.AllocHGlobal(bytes.Length + 1);
                        Marshal.Copy(bytes, 0, strNative, bytes.Length);
                        Marshal.WriteByte(strNative + bytes.Length, 0);

                        SqliteUtil.ThrowIfError(
                            _sqlite,
                            sqlite3_bind_text16(_stmt, i, strNative, bytes.Length, _freeFunc.Value.Ptr)
                        );
                        break;
                    }
                }
            }
        }
    }

    private void GetColumns(bool returnResult, out List<string> columnNames, out int columnCount)
    {
        columnNames = new();
        columnCount = 0;
        if (returnResult)
        {
            columnCount = sqlite3_column_count(_stmt);
            for (int i = 0; i < columnCount; i++)
            {
                var columnName = Marshal.PtrToStringUni(sqlite3_column_name16(_stmt, i));
                columnNames.Add(columnName);
            }
        }
    }

    private void ReadResultsIntoTable(
        bool returnResult,
        int columnCount,
        SimpleDataTableBuilder builder,
        out long fullCount,
        Action onRow,
        CancellationToken cancel
    )
    {
        fullCount = 0;
        var rowData = new object[columnCount];
        while (true)
        {
            cancel.ThrowIfCancellationRequested();

            int ret = sqlite3_step(_stmt);
            if (ret == SQLITE_DONE)
            {
                break;
            }

            if (ret == SQLITE_ROW)
            {
                onRow?.Invoke();
                fullCount++;
                if (returnResult)
                {
                    if (fullCount >= int.MaxValue)
                    {
                        // Keep counting, but don't read the rows.
                        continue;
                    }
                    for (int i = 0; i < columnCount; i++)
                    {
                        rowData[i] = GetCellValue(i);
                    }
                    builder.AddRow(rowData);
                }
                continue;
            }

            ThrowError(ret);
        }
    }

    private void ReadResultsStream(int columnCount, Action<object[]> onRow, CancellationToken cancel)
    {
        var rowData = new object[columnCount];
        while (true)
        {
            cancel.ThrowIfCancellationRequested();

            int ret = sqlite3_step(_stmt);
            if (ret == SQLITE_DONE)
            {
                break;
            }

            if (ret == SQLITE_ROW)
            {
                for (int i = 0; i < columnCount; i++)
                {
                    rowData[i] = GetCellValue(i);
                }
                onRow?.Invoke(rowData);
                continue;
            }

            ThrowError(ret);
        }
    }

    private void ThrowError(int ret)
    {
        if (ret == SQLITE_ERROR)
        {
            if (Notebook.SqliteVtabErrorMessage != null)
            {
                var vtabMessage = Notebook.SqliteVtabErrorMessage;
                Notebook.SqliteVtabErrorMessage = null;
                throw new ExceptionEx("A remote database query failed.", vtabMessage);
            }
            SqliteUtil.ThrowIfError(_sqlite, SQLITE_ERROR, _sql);
        }
        else if (ret == SQLITE_INTERRUPT)
        {
            throw new OperationCanceledException("SQL query canceled by the user.");
        }
        else
        {
            SqliteUtil.ThrowIfError(_sqlite, ret, _sql);
        }
    }

    private object GetCellValue(int i)
    {
        object cellValue;
        switch (sqlite3_column_type(_stmt, i))
        {
            case SQLITE_INTEGER:
                cellValue = sqlite3_column_int64(_stmt, i);
                break;
            case SQLITE_FLOAT:
                cellValue = sqlite3_column_double(_stmt, i);
                break;
            case SQLITE_TEXT:
                cellValue = Marshal.PtrToStringUni(sqlite3_column_text16(_stmt, i));
                break;
            case SQLITE_BLOB:
            {
                var cb = sqlite3_column_bytes(_stmt, i);
                var sourceBuffer = sqlite3_column_blob(_stmt, i);
                var copy = new byte[cb];
                if (cb > 0)
                    Marshal.Copy(sourceBuffer, copy, 0, cb);
                cellValue = copy;
                break;
            }
            case SQLITE_NULL:
                cellValue = DBNull.Value;
                break;
            default:
                throw new InvalidOperationException("Unrecognized result from sqlite3_column_type().");
        }

        return cellValue;
    }
}
