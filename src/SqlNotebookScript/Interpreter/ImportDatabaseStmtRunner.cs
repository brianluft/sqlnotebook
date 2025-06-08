using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;
using SqlNotebookScript.Core;
using SqlNotebookScript.Utils;

namespace SqlNotebookScript.Interpreter;

public sealed class ImportDatabaseStmtRunner
{
    private readonly Notebook _notebook;
    private readonly ScriptEnv _env;
    private readonly ScriptRunner _runner;
    private readonly Ast.ImportDatabaseStmt _stmt;
    private readonly CancellationToken _cancel;

    private readonly bool _link = false;
    private readonly bool _truncateExistingTable = false;
    private readonly bool _temporaryTable = false;
    private readonly string _vendor;
    private readonly string _srcSchemaName = "";
    private readonly string _srcTableName;
    private readonly string _sql;
    private readonly string _dstTableName;
    private readonly string _connectionString;

    // must be run from the SQLite thread
    public static void Run(
        Notebook notebook,
        ScriptEnv env,
        ScriptRunner runner,
        Ast.ImportDatabaseStmt stmt,
        CancellationToken cancel
    )
    {
        ImportDatabaseStmtRunner importer = new(notebook, env, runner, stmt, cancel);
        SqlUtil.WithTransaction(notebook, importer.Import);
    }

    private ImportDatabaseStmtRunner(
        Notebook notebook,
        ScriptEnv env,
        ScriptRunner runner,
        Ast.ImportDatabaseStmt stmt,
        CancellationToken cancel
    )
    {
        _notebook = notebook;
        _env = env;
        _runner = runner;
        _stmt = stmt;
        _cancel = cancel;

        // vendor
        var vendorObj = _runner.EvaluateExpr(_stmt.VendorExpr, _env);
        if (vendorObj is not string)
        {
            throw new Exception("IMPORT DATABASE: The 'vendor' parameter must be a string.");
        }
        _vendor = ((string)vendorObj).ToLowerInvariant();
        switch (_vendor)
        {
            case "mssql":
            case "pgsql":
            case "mysql":
            case "duckdb":
                break;
            default:
                throw new Exception($"IMPORT DATABASE: The vendor \"{_vendor}\" is not recognized.");
        }

        // src-schema-name
        if (stmt.SrcSchemaNameExprOrNull != null)
        {
            _srcSchemaName = _runner.EvaluateIdentifierOrExpr(stmt.SrcSchemaNameExprOrNull, _env);
        }

        // src-table-name, sql
        if (stmt.SrcTableNameExprOrNull != null)
        {
            _srcTableName = _runner.EvaluateIdentifierOrExpr(stmt.SrcTableNameExprOrNull, _env);
            _sql = null;
        }
        else if (stmt.SqlExprOrNull != null)
        {
            var sqlObj = _runner.EvaluateExpr(stmt.SqlExprOrNull, _env);
            if (sqlObj is not string)
            {
                throw new Exception("IMPORT DATABASE: The 'sql' parameter must be a string.");
            }
            _sql = (string)sqlObj;
            _srcTableName = null;
        }
        else
        {
            throw new Exception("IMPORT DATABASE: Internal error. No source table and no query specified.");
        }

        // dst-table-name
        if (_stmt.DstTableNameExprOrNull != null)
        {
            _dstTableName = _runner.EvaluateIdentifierOrExpr(_stmt.DstTableNameExprOrNull, _env);
        }
        else if (_stmt.SrcTableNameExprOrNull != null)
        {
            _dstTableName = _runner.EvaluateIdentifierOrExpr(_stmt.SrcTableNameExprOrNull, _env);
        }
        else if (_stmt.SqlExprOrNull != null)
        {
            throw new Exception(
                "IMPORT DATABASE: A target table name (with INTO) is required when the source is a query."
            );
        }
        else
        {
            throw new Exception("IMPORT DATABASE: Internal error. No destination table name.");
        }

        // connection-string
        var connectionStringObj = _runner.EvaluateExpr(_stmt.ConnectionStringExpr, _env);
        if (connectionStringObj is not string)
        {
            throw new Exception("IMPORT DATABASE: The 'connection-string' parameter must be a string.");
        }
        _connectionString = (string)connectionStringObj;

        // OPTIONS
        foreach (var option in _stmt.OptionsList.GetOptionKeys())
        {
            switch (option)
            {
                case "LINK":
                    _link = _stmt.OptionsList.GetOptionBool(option, _runner, _env, false);
                    break;

                case "TRUNCATE_EXISTING_TABLE":
                    _truncateExistingTable = _stmt.OptionsList.GetOptionBool(option, _runner, _env, false);
                    break;

                case "TEMPORARY_TABLE":
                    _temporaryTable = _stmt.OptionsList.GetOptionBool(option, _runner, _env, false);
                    break;

                default:
                    throw new Exception($"\"{option}\" is not a recognized option name.");
            }
        }

        // validate option LINK
        if (_link)
        {
            if (_sql != null)
            {
                throw new Exception(
                    "IMPORT DATABASE: When creating a live link, a source table name must be provided, not an SQL query."
                );
            }
            if (TableExists(_dstTableName))
            {
                throw new Exception($"IMPORT DATABASE: The table \"{_dstTableName}\" already exists.");
            }
            if (_truncateExistingTable)
            {
                throw new Exception(
                    $"IMPORT DATABASE: When creating a live link, the TRUNCATE_EXISTING_TABLE option is not valid."
                );
            }
            if (_temporaryTable)
            {
                throw new Exception(
                    $"IMPORT DATABASE: When creating a live link, the TEMPORARY_TABLE option is not valid."
                );
            }
        }
    }

    private bool TableExists(string name)
    {
        using var sdt = _notebook.Query($"PRAGMA TABLE_INFO({name.DoubleQuote()});");
        return sdt.FullCount > 0;
    }

    private void Import()
    {
        if (_link)
        {
            ImportLink();
        }
        else
        {
            ImportCopy();
        }
    }

    private void ImportLink()
    {
        using var status = WaitStatus.StartStatic(GetTruncatedDstTableName());
        if (_vendor == "mssql")
        {
            _notebook.Execute(
                $"CREATE VIRTUAL TABLE {_dstTableName.DoubleQuote()} USING mssql "
                    + $"('{_connectionString.Replace("'", "''")}', '{_srcTableName.Replace("'", "''")}', "
                    + $"'{_srcSchemaName.Replace("'", "''")}')"
            );
        }
        else
        {
            _notebook.Execute(
                $"CREATE VIRTUAL TABLE {_dstTableName.DoubleQuote()} USING {_vendor} "
                    + $"('{_connectionString.Replace("'", "''")}', '{_srcTableName.Replace("'", "''")}')"
            );
        }
    }

    private string RemoteQuote(string name)
    {
        if (_vendor == "mysql")
        {
            return $"`{name.Replace("`", "``")}`";
        }
        else
        {
            return name.DoubleQuote();
        }
    }

    private string GetTruncatedDstTableName() =>
        _dstTableName.Length > 50 ? _dstTableName[..50] + "..." : _dstTableName;

    private void ImportCopy()
    {
        IDbConnection srcConnection = null;
        IDbCommand srcCommand = null;
        IDataReader reader = null;

        using var status = WaitStatus.StartRows(GetTruncatedDstTableName());
        try
        {
            srcConnection = _vendor switch
            {
                "mssql" => new SqlConnection(_connectionString),
                "pgsql" => new NpgsqlConnection(_connectionString),
                "mysql" => new MySqlConnection(_connectionString),
                "duckdb" => new DuckDBConnection(_connectionString),
                _ => throw new Exception("IMPORT DATABASE: Internal error. Invalid vendor."),
            };
            srcConnection.Open();

            var pkColumns = GetSourcePrimaryKey(srcConnection);

            srcCommand = srcConnection.CreateCommand();
            if (_sql != null)
            {
                srcCommand.CommandText = _sql;
            }
            else
            {
                var quotedSourceTable =
                    (_vendor == "mssql" || _vendor == "pgsql") && !string.IsNullOrEmpty(_srcSchemaName)
                        ? $"{RemoteQuote(_srcSchemaName)}.{RemoteQuote(_srcTableName)}"
                        : RemoteQuote(_srcTableName);
                srcCommand.CommandText = $"SELECT * FROM {quotedSourceTable}";
            }
            reader = srcCommand.ExecuteReader();

            ImportCopyCore(reader, pkColumns, status);
        }
        finally
        {
            // Dispose on another thread; it can take time.
            _ = Task.Run(
                () =>
                {
                    reader?.Dispose();
                    srcCommand?.Dispose();
                    srcConnection?.Dispose();
                },
                CancellationToken.None
            );
        }
    }

    private List<string> GetSourcePrimaryKey(IDbConnection connection)
    {
        try
        {
            using var command = connection.CreateCommand();
            if (_vendor == "pgsql")
            {
                command.CommandText =
                    @$"
SELECT k.column_name
FROM information_schema.table_constraints c, information_schema.key_column_usage k
WHERE
    c.constraint_type = 'PRIMARY KEY' AND
    c.table_schema = current_schema() AND
    c.table_name = @srcTableName AND
    k.constraint_catalog = c.constraint_catalog AND
    k.constraint_schema = c.constraint_schema AND
    k.constraint_name = c.constraint_name
ORDER BY k.ordinal_position;";
            }
            else if (_vendor == "mssql")
            {
                var schema = string.IsNullOrEmpty(_srcSchemaName) ? "dbo" : _srcSchemaName;
                command.CommandText =
                    @$"
SELECT k.COLUMN_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS c, INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
WHERE
    c.CONSTRAINT_TYPE = 'PRIMARY KEY' AND
    c.TABLE_SCHEMA = @srcSchemaName AND
    c.TABLE_NAME = @srcTableName AND
    k.CONSTRAINT_CATALOG = c.CONSTRAINT_CATALOG AND
    k.CONSTRAINT_SCHEMA = c.CONSTRAINT_SCHEMA AND
    k.CONSTRAINT_NAME = c.CONSTRAINT_NAME
ORDER BY k.ORDINAL_POSITION;";
            }
            else if (_vendor == "mysql")
            {
                command.CommandText =
                    @$"
SELECT k.COLUMN_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS c, INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
WHERE
    c.CONSTRAINT_TYPE = 'PRIMARY KEY' AND
    c.TABLE_SCHEMA = database() AND
    c.TABLE_NAME = @srcTableName AND
    k.CONSTRAINT_CATALOG = c.CONSTRAINT_CATALOG AND
    k.CONSTRAINT_SCHEMA = c.CONSTRAINT_SCHEMA AND
    k.CONSTRAINT_NAME = c.CONSTRAINT_NAME AND
    k.TABLE_SCHEMA = c.TABLE_SCHEMA AND
    k.TABLE_NAME = c.TABLE_NAME
ORDER BY k.ORDINAL_POSITION;";
            }
            else if (_vendor == "duckdb")
            {
                // DuckDB doesn't have the same information_schema structure
                // Return empty list for now - primary key detection is not critical
                return new();
            }
            else
            {
                throw new NotImplementedException();
            }

            var srcTableName = command.CreateParameter();
            srcTableName.ParameterName = "@srcTableName";
            srcTableName.DbType = DbType.String;
            srcTableName.Value = _srcTableName;
            command.Parameters.Add(srcTableName);

            if (_vendor == "mssql")
            {
                var srcSchemaName = command.CreateParameter();
                srcSchemaName.ParameterName = "@srcSchemaName";
                srcSchemaName.DbType = DbType.String;
                srcSchemaName.Value = string.IsNullOrEmpty(_srcSchemaName) ? "dbo" : _srcSchemaName;
                command.Parameters.Add(srcSchemaName);
            }

            using var reader = command.ExecuteReader();
            List<string> list = new();
            while (reader.Read())
            {
                list.Add(reader.GetString(0));
            }
            return list;
        }
        catch
        {
            return new();
        }
    }

    private void ImportCopyCore(IDataReader reader, List<string> pkColumns, WaitStatus.InFlightRows status)
    {
        List<string> srcColumnNames = new();
        List<Type> srcColumnTypes = new();
        for (var i = 0; i < reader.FieldCount; i++)
        {
            srcColumnNames.Add(reader.GetName(i));
            srcColumnTypes.Add(reader.GetFieldType(i));
        }

        // Create table
        List<string> dstCreateColumns = new();
        var pkColumnsSet = pkColumns.ToHashSet();
        var pkColumnsSeen = 0;
        for (int i = 0; i < srcColumnNames.Count; i++)
        {
            var t = srcColumnTypes[i];
            string sqlType;
            if (
                t == typeof(short)
                || t == typeof(ushort)
                || t == typeof(int)
                || t == typeof(uint)
                || t == typeof(long)
                || t == typeof(ulong)
                || t == typeof(byte)
                || t == typeof(sbyte)
                || t == typeof(bool)
            )
            {
                sqlType = "INTEGER";
            }
            else if (t == typeof(float) || t == typeof(double) || t == typeof(decimal))
            {
                sqlType = "REAL";
            }
            else if (t == typeof(byte[]) || t == typeof(BitArray))
            {
                sqlType = "BLOB";
            }
            else
            {
                sqlType = "TEXT";
            }
            var columnName = srcColumnNames[i];
            dstCreateColumns.Add("\"" + columnName.Replace("\"", "\"\"") + "\" " + sqlType);
            if (pkColumnsSet.Contains(columnName))
            {
                pkColumnsSeen++;
            }
        }
        var pk =
            pkColumnsSeen > 0 && pkColumnsSeen == pkColumns.Count
                ? $", PRIMARY KEY ({string.Join(", ", pkColumns.Select(x => x.DoubleQuote()))})"
                : "";
        _notebook.Execute(
            $"CREATE {(_temporaryTable ? "TEMPORARY" : "")} TABLE IF NOT EXISTS {_dstTableName.DoubleQuote()} "
                + $"({string.Join(", ", dstCreateColumns)}{pk})"
        );

        if (_truncateExistingTable)
        {
            _notebook.Execute($"DELETE FROM {_dstTableName.DoubleQuote()};");
        }

        // Copy data
        var insertSql =
            $"INSERT INTO {_dstTableName.DoubleQuote()} VALUES ({string.Join(", ", Enumerable.Range(0, reader.FieldCount).Select(x => "?"))})";
        using var insertStmt = _notebook.Prepare(insertSql);

        object[][] NewBuffer()
        {
            const int CAPACITY = 100; // empirically determined
            var buffer = new object[CAPACITY][];
            for (var i = 0; i < CAPACITY; i++)
            {
                buffer[i] = new object[reader.FieldCount];
            }
            return buffer;
        }

        void Produce(object[][] buffer, out int batchCount)
        {
            batchCount = 0;
            while (batchCount < buffer.Length && reader.Read())
            {
                var thisBuffer = buffer[batchCount];
                reader.GetValues(thisBuffer);
                for (var i = 0; i < thisBuffer.Length; i++)
                {
                    thisBuffer[i] = thisBuffer[i] switch
                    {
                        null or DBNull => null,
                        short or ushort or int or uint or long or ulong or byte or sbyte or bool => Convert.ToInt64(
                            thisBuffer[i]
                        ),
                        float or double or decimal => Convert.ToDouble(thisBuffer[i]),
                        DateTime a => $"{a:yyyy-MM-dd HH:mm:ss.fff}",
                        DateTimeOffset a => $"{a.UtcDateTime:yyyy-MM-dd HH:mm:ss.fff}",
                        TimeSpan a => (a < TimeSpan.Zero ? "-" : "")
                            + a.ToString(a < TimeSpan.FromDays(1) ? @"hh\:mm\:ss\.fff" : @"d\.hh\:mm\:ss\.fff"),
                        byte[] a => a,
                        BitArray a => BitArrayToSqlArray(a),
                        _ => Convert.ToString(thisBuffer[i]),
                    };
                }
                batchCount++;
            }
        }

        void Consume(object[][] buffer, int batchCount, long totalSoFar)
        {
            for (var i = 0; i < batchCount; i++)
            {
                insertStmt.ExecuteStream(buffer[i], null, null, _cancel);
                status.IncrementRows();
            }
        }

        OverlappedProducerConsumer.Go(NewBuffer, Produce, Consume, _cancel);
    }

    private static byte[] BitArrayToSqlArray(BitArray a)
    {
        List<object> objectList = new(a.Count);
        for (var i = 0; i < a.Count; i++)
        {
            objectList.Add((long)(a[i] ? 1 : 0));
        }
        return ArrayUtil.ConvertToSqlArray(objectList);
    }
}
