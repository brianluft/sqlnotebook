using System;
using System.Runtime.InteropServices;
using static SqlNotebookScript.Core.SqliteInterop.NativeMethods;

namespace SqlNotebookScript.Core.SqliteInterop;

public static class SqliteUtil
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeDelegate(IntPtr p);

    private static readonly Lazy<(IntPtr Ptr, FreeDelegate Delegate)> _freeFunc = new(() =>
    {
        FreeDelegate @delegate = Marshal.FreeHGlobal;
        return (Marshal.GetFunctionPointerForDelegate(@delegate), @delegate);
    });

    public static void ThrowIfError(IntPtr sqlite, int result, string sql = null)
    {
        if (result != SQLITE_OK)
        {
            if (sqlite == IntPtr.Zero)
            {
                throw new SqliteException($"SQLite error {result}");
            }

            var messageUnmanaged = sqlite3_errmsg16(sqlite);
            var message = Marshal.PtrToStringUni(messageUnmanaged);

            SqliteException ex = new(message);

            var errorOffset = sqlite3_error_offset(sqlite);
            if (errorOffset >= 0 && sql != null)
            {
                var snippet = sql[errorOffset..];
                if (snippet.Length > 30)
                {
                    snippet = snippet[..30] + "...";
                }
                ex.Snippet = snippet;
            }

            throw ex;
        }
    }

    public static void Result(
        IntPtr ctx, // sqlite3_context*
        object value
    )
    {
        if (value is null)
        {
            sqlite3_result_null(ctx);
            return;
        }
        switch (value)
        {
            case DBNull:
                sqlite3_result_null(ctx);
                break;
            case short x:
                sqlite3_result_int(ctx, x);
                break;
            case int x:
                sqlite3_result_int(ctx, x);
                break;
            case long x:
                sqlite3_result_int64(ctx, x);
                break;
            case byte x:
                sqlite3_result_int(ctx, x);
                break;
            case float x:
                sqlite3_result_double(ctx, x);
                break;
            case double x:
                sqlite3_result_double(ctx, x);
                break;
            case string x:
                ResultText16(ctx, x);
                break;
            case char x:
                ResultText16(ctx, x.ToString());
                break;
            case bool x:
                sqlite3_result_int(ctx, x ? 1 : 0);
                break;
            case DateTime x:
                ResultText16(ctx, x.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                break;
            case DateTimeOffset x:
                ResultText16(ctx, x.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                break;
            case byte[] x:
                ResultBlob(ctx, x);
                break;
            default:
                ResultText16(ctx, value.ToString());
                break;
        }
    }

    public static void ResultError(
        IntPtr ctx, // sqlite3_context*
        string message
    )
    {
        var messageUnmanaged = Marshal.StringToHGlobalUni(message);
        try
        {
            sqlite3_result_error16(ctx, messageUnmanaged, -1);
        }
        finally
        {
            Marshal.FreeHGlobal(messageUnmanaged);
        }
    }

    public static void ResultText16(
        IntPtr ctx, // sqlite3_context*
        string str
    )
    {
        var strNative16 = Marshal.StringToHGlobalUni(str);
        var lenB = str.Length * 2;
        sqlite3_result_text16(ctx, strNative16, lenB, _freeFunc.Value.Ptr);
    }

    public static void ResultBlob(
        IntPtr ctx, // sqlite3_context*
        byte[] bytes
    )
    {
        var bytesNative = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, bytesNative, bytes.Length);
        sqlite3_result_blob64(ctx, bytesNative, (ulong)bytes.Length, _freeFunc.Value.Ptr);
    }
}
