using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SqlNotebookScript.Core.ModuleDelegates;
using SqlNotebookScript.Core.SqliteInterop;
using SqlNotebookScript.Utils;
using static SqlNotebookScript.Core.SqliteInterop.NativeMethods;

namespace SqlNotebookScript.Core.GenericModules;

public sealed class GenericModuleProvider : IDisposable
{
    private static int _nextMetadataKey = 1;

    /// <summary>
    /// This matches the *pAux we get in <see cref="GenericCreate"/>.
    /// </summary>
    private static readonly Dictionary<int, CustomTableFunction> _customTableFunctions = new();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void RemoveCustomTableFunctionDelegate(IntPtr p);

    private static readonly Lazy<(
        IntPtr Ptr,
        RemoveCustomTableFunctionDelegate Delegate
    )> _removeCustomTableFunctionFunc = new(() =>
    {
        RemoveCustomTableFunctionDelegate @delegate = p =>
        {
            _customTableFunctions.Remove((int)p);
        };
        return (Marshal.GetFunctionPointerForDelegate(@delegate), @delegate);
    });

    /// <summary>
    /// This matches the MetadataKey in <see cref="Sqlite3Vtab"/>.
    /// </summary>
    private static readonly Dictionary<int, GenericTableMetadata> _tableMetadatas = new();

    /// <summary>
    /// This matches the MetadataKey in <see cref="Sqlite3VtabCursor"/>.
    /// </summary>
    private static readonly Dictionary<int, GenericCursorMetadata> _cursorMetadatas = new();

    /// <summary>
    /// These are delegates for which we've called <see cref="Marshal.GetFunctionPointerForDelegate{TDelegate}(TDelegate)"/>.
    /// We are responsible for keeping them alive, so we'll stash them in this list.
    /// </summary>
    private static readonly List<object> _delegates = new();

    private bool _installed = false;
    private IntPtr _moduleNative; // sqlite3_module*
    private bool _disposedValue;

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
            if (_moduleNative != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_moduleNative);
            }
            _disposedValue = true;
        }
    }

    // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~GenericModuleProvider()
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
        IntPtr sqlite, // sqlite3*
        CustomTableFunction customTableFunction
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
                GenericPopulateModule(_moduleNative);

                var key = _nextMetadataKey++;
                _customTableFunctions.Add(key, customTableFunction);

                // Install the module into SQLite.
                using NativeString nameNative = new(customTableFunction.Name);
                SqliteUtil.ThrowIfError(
                    sqlite,
                    sqlite3_create_module_v2(
                        sqlite,
                        nameNative.Ptr,
                        _moduleNative,
                        (IntPtr)key,
                        _removeCustomTableFunctionFunc.Value.Ptr
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

    private static void GenericPopulateModule(
        IntPtr modulePtr // sqlite3_module*
    )
    {
        ZeroMemory(modulePtr, (IntPtr)Marshal.SizeOf<Sqlite3Module>());
        var module = Marshal.PtrToStructure<Sqlite3Module>(modulePtr);

        module.iVersion = 1;
        {
            ModuleCreateDelegate x = GenericCreate;
            _delegates.Add(x);
            module.xCreate = module.xConnect = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleBestIndexDelegate x = GenericBestIndex;
            _delegates.Add(x);
            module.xBestIndex = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleDestroyDelegate x = GenericDestroy;
            _delegates.Add(x);
            module.xDestroy = module.xDisconnect = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleOpenDelegate x = GenericOpen;
            _delegates.Add(x);
            module.xOpen = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleCloseDelegate x = GenericClose;
            _delegates.Add(x);
            module.xClose = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleFilterDelegate x = GenericFilter;
            _delegates.Add(x);
            module.xFilter = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleNextDelegate x = GenericNext;
            _delegates.Add(x);
            module.xNext = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleEofDelegate x = GenericEof;
            _delegates.Add(x);
            module.xEof = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleColumnDelegate x = GenericColumn;
            _delegates.Add(x);
            module.xColumn = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleRowidDelegate x = GenericRowid;
            _delegates.Add(x);
            module.xRowid = Marshal.GetFunctionPointerForDelegate(x);
        }
        {
            ModuleRenameDelegate x = GenericRename;
            _delegates.Add(x);
            module.xRename = Marshal.GetFunctionPointerForDelegate(x);
        }

        Marshal.StructureToPtr(module, modulePtr, false);
    }

    private static int GenericCreate(
        IntPtr db, // sqlite3*
        IntPtr pAux, // void*
        int argc,
        IntPtr argv, // const char* const*
        IntPtr ppVTab, // sqlite3_vtab**
        IntPtr pzErr // char**
    )
    {
        try
        {
            var customTableFunction = _customTableFunctions[(int)pAux];
            var metadataKey = _nextMetadataKey++;
            _tableMetadatas.Add(metadataKey, new() { CustomTableFunction = customTableFunction });
            Sqlite3Vtab vtab = new() { MetadataKey = metadataKey };
            var vtabSize = Marshal.SizeOf<Sqlite3Vtab>();
            var vtabNative = Marshal.AllocHGlobal(vtabSize);
            ZeroMemory(vtabNative, (IntPtr)vtabSize);
            Marshal.StructureToPtr(vtab, vtabNative, false);
            using NativeString sql = new(customTableFunction.CreateTableSql);
            SqliteUtil.ThrowIfError(db, sqlite3_declare_vtab(db, sql.Ptr));
            Marshal.WriteIntPtr(ppVTab, vtabNative);
            return SQLITE_OK;
        }
        catch
        {
            return SQLITE_INTERNAL;
        }
    }

    private static int GenericDestroy(
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

    private static int GenericBestIndex(
        IntPtr pVTab, // sqlite3_vtab*
        IntPtr infoPtr // sqlite3_index_info*
    )
    {
        var vtab = Marshal.PtrToStructure<Sqlite3Vtab>(pVTab);
        var vtabMeta = _tableMetadatas[vtab.MetadataKey];
        var info = Marshal.PtrToStructure<Sqlite3IndexInfo>(infoPtr);
        List<int> filter = new();

        // WHERE clause
        int argvIndex = 1;
        for (int i = 0; i < info.nConstraint; i++)
        {
            var constraintPtr = info.aConstraint + i * Marshal.SizeOf<Sqlite3IndexConstraint>();
            var constraint = Marshal.PtrToStructure<Sqlite3IndexConstraint>(constraintPtr);

            var colIndex = constraint.iColumn;
            var op = constraint.op;
            if (colIndex == -1)
            {
                continue;
            }
            else if (constraint.usable == 0)
            {
                continue;
            }
            else if (colIndex > vtabMeta.CustomTableFunction.HiddenColumnCount)
            {
                continue;
            }
            else if (op != SQLITE_INDEX_CONSTRAINT_EQ)
            {
                continue;
            }

            // set info.aConstraintUsage[i]
            Sqlite3IndexConstraintUsage constraintUsage = new() { argvIndex = argvIndex, omit = 0 };
            var constraintUsagePtr = info.aConstraintUsage + i * Marshal.SizeOf<Sqlite3IndexConstraintUsage>();
            Marshal.StructureToPtr(constraintUsage, constraintUsagePtr, false);

            filter.Add(colIndex);
            argvIndex++;
        }

        info.orderByConsumed = 0; // false
        info.idxNum = 0;
        info.idxStr = NativeString.NewUnmanagedUtf8StringWithSqliteAllocator(string.Join(",", filter));
        info.needToFreeIdxStr = 1; // true
        info.estimatedRows = 1000;
        info.estimatedCost = 1000;

        Marshal.StructureToPtr(info, infoPtr, false);
        return SQLITE_OK;
    }

    private static int GenericOpen(
        IntPtr pVTab, // sqlite3_vtab*
        IntPtr ppCursor // sqlite3_vtab_cursor**
    )
    {
        var metadataKey = ++_nextMetadataKey;

        // Prepare the cursor.
        IntPtr cursorNative;
        {
            var cursorSize = Marshal.SizeOf<Sqlite3VtabCursor>();
            cursorNative = Marshal.AllocHGlobal(cursorSize);
            ZeroMemory(cursorNative, (IntPtr)cursorSize);
            var cursor = Marshal.PtrToStructure<Sqlite3VtabCursor>(cursorNative);
            cursor.MetadataKey = metadataKey;
            Marshal.StructureToPtr(cursor, cursorNative, false);
        }

        // Prepare the cursor metadata.
        {
            var vtab = Marshal.PtrToStructure<Sqlite3Vtab>(pVTab);
            var vtabMetadata = _tableMetadatas[vtab.MetadataKey];
            _cursorMetadatas.Add(metadataKey, new() { TableMetadata = vtabMetadata });
        }

        // Write the pointer to the Sqlite3VtabCursor to *ppCursor.
        Marshal.WriteIntPtr(ppCursor, cursorNative);
        return SQLITE_OK;
    }

    private static int GenericClose(
        IntPtr pCur // sqlite3_vtab_cursor*
    )
    {
        var cursor = Marshal.PtrToStructure<Sqlite3VtabCursor>(pCur);
        _cursorMetadatas.Remove(cursor.MetadataKey);
        Marshal.FreeHGlobal(pCur);
        return SQLITE_OK;
    }

    private static int GenericNext(
        IntPtr pCur // sqlite3_vtab_cursor*
    )
    {
        var cursor = Marshal.PtrToStructure<Sqlite3VtabCursor>(pCur);
        var cursorMetadata = _cursorMetadatas[cursor.MetadataKey];
        try
        {
            cursorMetadata.Eof = !cursorMetadata.Enumerator.MoveNext();
            return SQLITE_OK;
        }
        catch (Exception ex)
        {
            SetVtabError(cursor.pVtab, ex.GetExceptionMessage());
            return SQLITE_ERROR;
        }
    }

    private static int GenericFilter(
        IntPtr pCur, // sqlite3_vtab_cursor*
        int idxNum,
        IntPtr idxStr, // const char*
        int argc,
        IntPtr argv // sqlite3_value**
    )
    {
        var cursor = Marshal.PtrToStructure<Sqlite3VtabCursor>(pCur);
        try
        {
            var cursorMetadata = _cursorMetadatas[cursor.MetadataKey];
            var customTableFunction = cursorMetadata.TableMetadata.CustomTableFunction;
            List<int> filter = new();
            foreach (var numStr in Marshal.PtrToStringUTF8(idxStr).Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                filter.Add(int.Parse(numStr));
            }

            var hiddenCount = customTableFunction.HiddenColumnCount;
            var hiddenValues = new object[hiddenCount];
            for (var i = 0; i < argc; i++)
            {
                var filterI = filter[i];
                if (filterI >= 0 && filterI < hiddenValues.Length)
                {
                    hiddenValues[filterI] = SqlUtil.GetArg(Marshal.ReadIntPtr(argv + i * IntPtr.Size));
                }
                else
                {
                    System.Diagnostics.Debugger.Break();
                }
            }

            cursorMetadata.Enumerator = customTableFunction.Execute(hiddenValues).GetEnumerator();
            cursorMetadata.Eof = false;
            return GenericNext(pCur);
        }
        catch (Exception ex)
        {
            SetVtabError(cursor.pVtab, ex.GetExceptionMessage());
            return SQLITE_ERROR;
        }
    }

    private static int GenericEof(
        IntPtr pCur // sqlite3_vtab_cursor*
    )
    {
        var cursor = Marshal.PtrToStructure<Sqlite3VtabCursor>(pCur);
        var cursorMetadata = _cursorMetadatas[cursor.MetadataKey];
        return cursorMetadata.Eof ? 1 : 0;
    }

    private static int GenericColumn(
        IntPtr pCur, // sqlite3_vtab_cursor*
        IntPtr ctx, // sqlite3_context*
        int n
    )
    {
        var cursor = Marshal.PtrToStructure<Sqlite3VtabCursor>(pCur);
        try
        {
            var cursorMetadata = _cursorMetadatas[cursor.MetadataKey];
            var row = cursorMetadata.Enumerator.Current;
            SqliteUtil.Result(ctx, row[n]);
            return SQLITE_OK;
        }
        catch (Exception ex)
        {
            SetVtabError(cursor.pVtab, ex.GetExceptionMessage());
            return SQLITE_ERROR;
        }
    }

    private static int GenericRowid(
        IntPtr pCur, // sqlite3_vtab_cursor*
        IntPtr pRowid // sqlite3_int64*
    )
    {
        var cursor = Marshal.PtrToStructure<Sqlite3VtabCursor>(pCur);
        try
        {
            var cursorMetadata = _cursorMetadatas[cursor.MetadataKey];
            var arr = cursorMetadata.Enumerator.Current;
            var hash = 13;
            foreach (var value in arr)
            {
                hash = (hash * 7) + value.GetHashCode();
            }
            Marshal.WriteInt64(pRowid, hash);
            return SQLITE_OK;
        }
        catch (Exception ex)
        {
            SetVtabError(cursor.pVtab, ex.GetExceptionMessage());
            return SQLITE_ERROR;
        }
    }

    private static int GenericRename(
        IntPtr pVtab, // sqlite3_vtab*
        IntPtr zNew // const char*
    )
    {
        // don't care
        return SQLITE_OK;
    }

    private static void SetVtabError(IntPtr vtabNative, string message)
    {
        var vtab = Marshal.PtrToStructure<Sqlite3Vtab>(vtabNative);
        if (vtab.zErrMsg != IntPtr.Zero)
        {
            sqlite3_free(vtab.zErrMsg);
        }
        vtab.zErrMsg = NativeString.NewUnmanagedUtf8StringWithSqliteAllocator(message);
    }
}
