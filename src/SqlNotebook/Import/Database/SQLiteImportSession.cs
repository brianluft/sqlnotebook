using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using SqlNotebook.Properties;

namespace SqlNotebook.Import.Database;

public sealed class SQLiteImportSession : IImportSession
{
    private readonly string _filePath;
    private List<(string Schema, string Table)> _tableNames = new();

    public SQLiteImportSession(string filePath)
    {
        _filePath = filePath;
    }

    public IReadOnlyList<(string Schema, string Table)> TableNames => _tableNames;

    public bool FromConnectForm(IWin32Window owner)
    {
        // For SQLite files, we don't need a connection form since we already have the file path
        // Just try to read the table names to validate the file
        try
        {
            ReadTableNames();
            return true;
        }
        catch (Exception ex)
        {
            Ui.ShowError(owner, "SQLite Import Error", $"Unable to read tables from SQLite file:\n{ex.Message}");
            return false;
        }
    }

    private void ReadTableNames()
    {
        using var connection = new SqliteConnection($"Data Source={_filePath}");
        connection.Open();

        List<(string Schema, string Table)> tableNames = new();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            tableNames.Add((null, reader.GetString(0)));
        }
        _tableNames = tableNames;
    }

    public DbConnection CreateConnection()
    {
        return new SqliteConnection($"Data Source={_filePath}");
    }

    public DatabaseConnectionForm.BasicOptions GetBasicOptions(DbConnectionStringBuilder builder)
    {
        return new DatabaseConnectionForm.BasicOptions
        {
            Server = _filePath,
            Database = System.IO.Path.GetFileNameWithoutExtension(_filePath),
            Username = "",
            Password = "",
        };
    }

    public void SetBasicOptions(DbConnectionStringBuilder builder, DatabaseConnectionForm.BasicOptions opt)
    {
        // For SQLite, the "server" field contains the file path
        // No other options are relevant for file-based databases
    }

    public void Clear(DbConnectionStringBuilder builder)
    {
        builder.Clear();
    }

    public string GenerateSql(IEnumerable<SourceTable> selectedTables, bool link)
    {
        var statements = new List<string>();

        foreach (var table in selectedTables)
        {
            if (table.SourceIsTable)
            {
                var importSql =
                    $"IMPORT DATABASE 'sqlite'\nCONNECTION 'Data Source={_filePath.Replace("'", "''")}'\nTABLE {table.SourceTableName}";
                if (!string.Equals(table.SourceTableName, table.TargetTableName, StringComparison.OrdinalIgnoreCase))
                {
                    importSql += $"\nINTO {table.TargetTableName}";
                }
                if (link)
                {
                    importSql += "\nOPTIONS (LINK: 1)";
                }
                importSql += ";";
                statements.Add(importSql);
            }
            else if (table.SourceIsSql)
            {
                var importSql =
                    $"IMPORT DATABASE 'sqlite'\nCONNECTION 'Data Source={_filePath.Replace("'", "''")}'\nSQL '{table.SourceSql.Replace("'", "''")}'";
                importSql += $"\nINTO {table.TargetTableName}";
                if (link)
                {
                    importSql += "\nOPTIONS (LINK: 1)";
                }
                importSql += ";";
                statements.Add(importSql);
            }
        }

        return string.Join("\n\n", statements);
    }
}
