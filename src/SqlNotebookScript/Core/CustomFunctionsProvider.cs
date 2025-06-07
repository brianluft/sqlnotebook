using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SqlNotebookScript.Core.SqliteInterop;

namespace SqlNotebookScript.Core;

public static class CustomFunctionsProvider
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void InvokeAction(IntPtr context, int argc, IntPtr argv);

    /// <remarks>We need to keep these alive since they are unmanaged callbacks.</remarks>
    private static readonly List<InvokeAction> _delegates = new();

    public static void InstallCustomFunctions(IntPtr sqlite)
    {
        RegisterCustomFunction(sqlite, "error_message", 0, ErrorMessage, false);
    }

    private static void RegisterCustomFunction(
        IntPtr sqlite, // sqlite3*
        string functionName,
        int numArgs,
        InvokeAction action,
        bool deterministic
    )
    {
        _delegates.Add(action);
        using NativeString functionNameNative = new(functionName);
        SqliteUtil.ThrowIfError(
            sqlite,
            NativeMethods.sqlite3_create_function_v2(
                db: sqlite,
                zFunc: functionNameNative.Ptr,
                nArg: numArgs,
                enc: NativeMethods.SQLITE_UTF16 | (deterministic ? NativeMethods.SQLITE_DETERMINISTIC : 0),
                p: IntPtr.Zero,
                xSFunc: Marshal.GetFunctionPointerForDelegate(action),
                xStep: IntPtr.Zero,
                xFinal: IntPtr.Zero,
                xDestroy: IntPtr.Zero
            )
        );
    }

    private static void ErrorMessage(IntPtr ctx, int argc, IntPtr argv)
    {
        SqliteUtil.Result(ctx, Notebook.ErrorMessage);
    }
}
