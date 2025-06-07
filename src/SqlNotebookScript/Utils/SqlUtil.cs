using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using SqlNotebookScript.Core;
using SqlNotebookScript.Interpreter;
using static SqlNotebookScript.Core.SqliteInterop.NativeMethods;
using Ast = SqlNotebookScript.Interpreter.Ast;

namespace SqlNotebookScript.Utils;

public static class SqlUtil
{
    public static string EscapeAmpersand(this string str) => str.Replace("&", "&&");

    public static string DoubleQuote(this string str)
    {
        return $"\"{str.Replace("\"", "\"\"")}\"";
    }

    public static string SingleQuote(this string str)
    {
        return $"'{str.Replace("'", "''")}'";
    }

    public static int? IndexOf(this IReadOnlyList<string> haystack, string needle)
    {
        var n = haystack.Count;
        for (int i = 0; i < n; i++)
        {
            if (haystack[i] == needle)
            {
                return i;
            }
        }
        return null;
    }

    public static void Import(
        IReadOnlyList<string> srcColNames,
        IEnumerable<object[]> dataRows,
        Ast.ImportTable importTable,
        bool temporaryTable,
        bool truncateExistingTable,
        bool stopAtFirstBlankRow,
        IfConversionFails ifConversionFails,
        BlankValuesOption blankValuesMethod,
        Notebook notebook,
        ScriptRunner runner,
        ScriptEnv env
    )
    {
        var dstTableName = runner.EvaluateIdentifierOrExpr(importTable.TableName, env);
        var mappings = GetImportColumnMappings(importTable.ImportColumns, runner, env, srcColNames);
        if (!mappings.Any())
        {
            throw new Exception("No columns chosen for import.");
        }
        CreateOrTruncateTable(mappings, dstTableName, temporaryTable, truncateExistingTable, notebook);
        VerifyColumnsExist(mappings.Select(x => x.DstColumnName), dstTableName, notebook);
        InsertDataRows(
            dataRows,
            srcColNames,
            mappings,
            dstTableName,
            ifConversionFails,
            stopAtFirstBlankRow,
            blankValuesMethod,
            notebook
        );
    }

    public static string GetInsertSql(string tableName, IReadOnlyList<ImportColumnMapping> mappings)
    {
        var columns = string.Join(", ", mappings.Select(x => x.DstColumnName.DoubleQuote()));
        var values = string.Join(", ", Enumerable.Range(1, mappings.Count).Select(x => $"?"));
        return $"INSERT INTO {tableName.DoubleQuote()} ({columns}) VALUES ({values})";
    }

    public static string GetInsertSql(string tableName, int numColumns)
    {
        var values = string.Join(", ", Enumerable.Range(1, numColumns).Select(x => $"?"));
        return $"INSERT INTO {tableName.DoubleQuote()} VALUES ({values})";
    }

    public static void VerifyColumnsExist(IEnumerable<string> colNames, string tableName, Notebook notebook)
    {
        using var tableInfo = notebook.Query($"PRAGMA TABLE_INFO ({tableName.DoubleQuote()})");
        var nameColIndex = tableInfo.GetIndex("name");
        var actualColNames = tableInfo.Rows.Select(x => x[nameColIndex].ToString().ToLowerInvariant()).ToHashSet();
        foreach (var name in colNames)
        {
            if (!actualColNames.Contains(name.ToLowerInvariant()))
            {
                throw new Exception($"The table \"{tableName}\" does not contain the column \"{name}\".");
            }
        }
    }

    public static string GetErrorMessage(this Exception self)
    {
        var uncaught = self as UncaughtErrorScriptException;
        if (uncaught != null)
        {
            return uncaught.ErrorMessage.ToString();
        }
        else
        {
            return self.GetExceptionMessage();
        }
    }

    public static bool TryParseValue(object rawValue, Ast.TypeConversion conversion, out object parsedValue)
    {
        parsedValue = null;
        if (rawValue == null)
        {
            return true;
        }
        switch (conversion)
        {
            case Ast.TypeConversion.Text:
                parsedValue = rawValue.ToString();
                return true;

            case Ast.TypeConversion.Integer:
            {
                if (rawValue is string s)
                {
                    if (long.TryParse(s, out var num))
                    {
                        parsedValue = num;
                        return true;
                    }
                }
                else if (rawValue is int || rawValue is long || rawValue is double)
                {
                    parsedValue = Convert.ToInt64(rawValue); // Round floating points.
                    return true;
                }
                return false;
            }

            case Ast.TypeConversion.Real:
            {
                if (rawValue is string s)
                {
                    if (double.TryParse(s, out var num))
                    {
                        parsedValue = num;
                        return true;
                    }
                }
                else if (rawValue is int || rawValue is long || rawValue is double)
                {
                    parsedValue = Convert.ToDouble(rawValue);
                    return true;
                }
                return false;
            }

            case Ast.TypeConversion.Date:
                DateTime dateValue;
                if (DateTime.TryParse(rawValue.ToString(), out dateValue))
                {
                    parsedValue = DateTimeUtil.FormatDate(dateValue.Date);
                    return true;
                }
                else
                {
                    return false;
                }

            case Ast.TypeConversion.DateTime:
                DateTime dateTimeValue;
                if (DateTime.TryParse(rawValue.ToString(), out dateTimeValue))
                {
                    parsedValue = DateTimeUtil.FormatDateTime(dateTimeValue);
                    return true;
                }
                else
                {
                    return false;
                }

            default:
                throw new Exception($"Internal error: unknown type conversion \"{conversion}\".");
        }
    }

    public readonly record struct ImportColumnMapping
    {
        public string SrcColumnName { get; init; }
        public string DstColumnName { get; init; }
        public Ast.ImportColumn ImportColumn { get; init; }
    }

    public static List<ImportColumnMapping> GetImportColumnMappings(
        IEnumerable<Ast.ImportColumn> importColumns,
        ScriptRunner runner,
        ScriptEnv env,
        IReadOnlyList<string> srcColNames
    )
    {
        // If there is no column list specified, then all columns are imported with default settings.
        // Just fake a list of ImportColumn objects in that case.
        if (!importColumns.Any())
        {
            importColumns = srcColNames
                .Select(x => new Ast.ImportColumn
                {
                    ColumnName = new Ast.IdentifierOrExpr { Identifier = x },
                    TypeConversion = Ast.TypeConversion.Text,
                })
                .ToList();
        }

        // Evaluate all the provided column names.
        Dictionary<Ast.ImportColumn, string> importColumnNames = new();
        Dictionary<Ast.ImportColumn, string> importColumnAsNames = new();
        foreach (var c in importColumns)
        {
            importColumnNames.Add(c, runner.EvaluateIdentifierOrExpr(c.ColumnName, env));
            if (c.AsName != null)
            {
                importColumnAsNames.Add(c, runner.EvaluateIdentifierOrExpr(c.AsName, env));
            }
        }

        var mappings = (
            from c in importColumns
            let srcColName = importColumnNames[c]
            let dstColName = importColumnAsNames.TryGetValue(c, out var asName) ? asName : srcColName
            select new ImportColumnMapping
            {
                SrcColumnName = srcColName,
                DstColumnName = dstColName,
                ImportColumn = c,
            }
        ).ToList();

        // Ensure there aren't any duplicate destination column names.
        var duplicateDstColName = (
            from c in importColumns
            let dstColName = importColumnAsNames.TryGetValue(c, out var asName) ? asName : importColumnNames[c]
            group dstColName by dstColName.ToLowerInvariant() into sameNameGroup
            where sameNameGroup.Count() > 1
            select sameNameGroup.First()
        ).FirstOrDefault();
        if (duplicateDstColName != null)
        {
            throw new Exception(
                $"The target column name \"{duplicateDstColName}\" was specified multiple times in the column list."
            );
        }

        return mappings;
    }

    public static void CreateOrTruncateTable(
        IReadOnlyList<ImportColumnMapping> mappings,
        string dstTableName,
        bool temporaryTable,
        bool truncateExistingTable,
        Notebook notebook
    )
    {
        // create the table if it doesn't already exist.
        var columnDefs = new List<string>();
        foreach (var mapping in mappings)
        {
            var name = mapping.DstColumnName;
            string sqlType = "";
            if (mapping.ImportColumn.TypeConversion.HasValue)
            {
                sqlType = mapping.ImportColumn.TypeConversion.Value switch
                {
                    Ast.TypeConversion.Integer => "integer",
                    Ast.TypeConversion.Real => "real",
                    _ => "text",
                };
            }

            columnDefs.Add($"{name.DoubleQuote()} {sqlType}");
        }
        var columnDefsList = string.Join(", ", columnDefs);
        var sql =
            $"CREATE {(temporaryTable ? "TEMPORARY" : "")} TABLE IF NOT EXISTS "
            + $"{dstTableName.DoubleQuote()} ({columnDefsList})";
        notebook.Execute(sql);

        if (truncateExistingTable)
        {
            notebook.Execute($"DELETE FROM {dstTableName.DoubleQuote()}");
        }
    }

    public static void InsertDataRows(
        IEnumerable<object[]> rows,
        IReadOnlyList<string> srcColNames,
        IReadOnlyList<ImportColumnMapping> mappings,
        string dstTableName,
        IfConversionFails ifConversionFails,
        bool stopAtFirstBlankRow,
        BlankValuesOption blankValuesMethod,
        Notebook notebook
    )
    {
        // Find the source column index for each source mapping.
        var sourceColumnIndices = new int[mappings.Count];
        for (var i = 0; i < mappings.Count; i++)
        {
            var srcColIndex = -1;
            for (var j = 0; j < srcColNames.Count; j++)
            {
                if (mappings[i].SrcColumnName.Equals(srcColNames[j], StringComparison.OrdinalIgnoreCase))
                {
                    srcColIndex = j;
                    break;
                }
            }
            if (srcColIndex == -1)
            {
                throw new Exception($"There is no source column named \"{mappings[i].SrcColumnName}\".");
            }
            sourceColumnIndices[i] = srcColIndex;
        }

        using var status = WaitStatus.StartRows(dstTableName);
        var insertSql = GetInsertSql(dstTableName, mappings);
        var insertArgs = new List<object>();
        foreach (var row in rows)
        {
            if (stopAtFirstBlankRow)
            {
                var isBlank = true;
                for (var j = 0; j < row.Length; j++)
                {
                    var value = row[j];
                    if (
                        value != null
                        && value is not DBNull
                        && (value is not string s || !string.IsNullOrWhiteSpace(s))
                    )
                    {
                        isBlank = false;
                        break;
                    }
                }
                if (isBlank)
                {
                    break;
                }
            }

            insertArgs.Clear();
            var skipRow = false;
            for (var mappingIndex = 0; mappingIndex < mappings.Count; mappingIndex++)
            {
                var srcColIndex = sourceColumnIndices[mappingIndex];
                var originalValue = srcColIndex < row.Length ? row[srcColIndex] : "";
                var typeConversion = mappings[mappingIndex].ImportColumn.TypeConversion ?? Ast.TypeConversion.Text;

                var isBlank = (originalValue is string s && s.Length == 0) || originalValue is null or DBNull;
                if (isBlank)
                {
                    switch (blankValuesMethod)
                    {
                        case BlankValuesOption.Null:
                        case BlankValuesOption.EmptyStringOrNull when (typeConversion != Ast.TypeConversion.Text):
                            insertArgs.Add(DBNull.Value);
                            continue;

                        case BlankValuesOption.EmptyString:
                        case BlankValuesOption.EmptyStringOrNull when (typeConversion == Ast.TypeConversion.Text):
                            originalValue = "";
                            break;
                    }
                }

                if (TryParseValue(originalValue, typeConversion, out var converted))
                {
                    insertArgs.Add(converted);
                }
                else if (ifConversionFails == IfConversionFails.ImportAsText)
                {
                    insertArgs.Add(originalValue.ToString());
                }
                else if (ifConversionFails == IfConversionFails.SkipRow)
                {
                    skipRow = true;
                }
                else if (ifConversionFails == IfConversionFails.Abort)
                {
                    throw new Exception(
                        $"Failed to parse input value as type \"{typeConversion}\". Value: \"{originalValue}\"."
                    );
                }
                else
                {
                    throw new Exception(
                        $"Internal error: unknown value for IF_CONVERSION_FAILS: \"{ifConversionFails}\"."
                    );
                }
            }
            if (!skipRow)
            {
                notebook.Execute(insertSql, insertArgs);
                status.IncrementRows();
            }
        }
    }

    public static T WithCancellableTransaction<T>(Notebook notebook, Func<T> func, CancellationToken cancel)
    {
        T value = default;
        WithCancellableTransaction(notebook, action: () => value = func(), rollback: false, cancel: cancel);
        return value;
    }

    public static void WithCancellableTransaction(Notebook notebook, Action action, CancellationToken cancel) =>
        WithCancellableTransaction(notebook, action, false, cancel);

    public static T WithCancellableTransaction<T>(
        Notebook notebook,
        Func<T> func,
        bool rollback,
        CancellationToken cancel
    )
    {
        T value = default;
        WithCancellableTransaction(notebook, action: () => value = func(), rollback, cancel);
        return value;
    }

    public static void WithCancellableTransaction(
        Notebook notebook,
        Action action,
        bool rollback,
        CancellationToken cancel
    )
    {
        if (notebook.IsTransactionActive())
        {
            throw new InvalidOperationException("There is already a transaction in progress.");
        }
        notebook.Execute("BEGIN");
        cancel.Register(() => notebook.BeginUserCancel());
        try
        {
            action();
            notebook.Execute(rollback ? "ROLLBACK" : "COMMIT");
        }
        catch
        {
            notebook.EndUserCancel();
            notebook.Execute("ROLLBACK");
            throw;
        }
    }

    public static T WithCancellation<T>(Notebook notebook, Func<T> func, CancellationToken cancel)
    {
        T value = default;
        WithCancellation(notebook, action: () => value = func(), cancel: cancel);
        return value;
    }

    public static void WithCancellation(Notebook notebook, Action action, CancellationToken cancel)
    {
        cancel.Register(() => notebook.BeginUserCancel());
        try
        {
            action();
        }
        finally
        {
            notebook.EndUserCancel();
        }
    }

    public static void WithTransaction(Notebook notebook, Action action) =>
        WithTransactionCore(notebook, action, rollback: false);

    public static void WithRollbackTransaction(Notebook notebook, Action action) =>
        WithTransactionCore(notebook, action, rollback: true);

    private static void WithTransactionCore(Notebook notebook, Action action, bool rollback)
    {
        var didBeginTransaction = false;
        if (!notebook.IsTransactionActive())
        {
            notebook.Execute("BEGIN");
            didBeginTransaction = true;
        }
        try
        {
            action();
            if (didBeginTransaction)
            {
                notebook.Execute(rollback ? "ROLLBACK" : "COMMIT");
            }
        }
        catch
        {
            notebook.EndUserCancel();
            if (didBeginTransaction)
            {
                notebook.Execute("ROLLBACK");
            }
            throw;
        }
    }

    public static T WithTransaction<T>(Notebook notebook, Func<T> func) =>
        WithTransactionCore(notebook, func, rollback: false);

    public static T WithRollbackTransaction<T>(Notebook notebook, Func<T> func) =>
        WithTransactionCore(notebook, func, rollback: true);

    private static T WithTransactionCore<T>(Notebook notebook, Func<T> func, bool rollback)
    {
        var value = default(T);
        WithTransactionCore(
            notebook,
            () =>
            {
                value = func();
            },
            rollback
        );
        return value;
    }

    public static object GetArg(
        IntPtr value /* sqlite3_value* */
    ) =>
        sqlite3_value_type(value) switch
        {
            SQLITE_INTEGER => sqlite3_value_int64(value),
            SQLITE_FLOAT => sqlite3_value_double(value),
            SQLITE_NULL => DBNull.Value,
            SQLITE_TEXT => Marshal.PtrToStringUni(sqlite3_value_text16(value)),
            SQLITE_BLOB => ReadBlobValue(value),
            _ => throw new Exception("Data type not supported."),
        };

    private static object ReadBlobValue(IntPtr value)
    {
        var cb = sqlite3_value_bytes(value);
        var inputArrayNative = sqlite3_value_blob(value);
        var outputArray = new byte[cb];
        Marshal.Copy(inputArrayNative, outputArray, 0, cb);
        return outputArray;
    }
}
