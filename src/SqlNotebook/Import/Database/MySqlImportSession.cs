using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;
using SqlNotebook.Properties;

namespace SqlNotebook.Import.Database;

public sealed class MySqlImportSession : ImportSessionBase<MySqlConnectionStringBuilder>
{
    public override string ProductName { get; } = "MySQL";
    protected override string SqliteModuleName => "mysql";

    public override DbConnection CreateConnection()
    {
        return new MySqlConnection(_builder.ConnectionString);
    }

    protected override void ReadTableNames(IDbConnection connection)
    {
        List<(string Schema, string Table)> tableNames = new();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText =
                "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = database() ORDER BY TABLE_NAME;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                tableNames.Add((null, reader.GetString(0)));
            }
        }
        TableNames = tableNames;
    }

    protected override MySqlConnectionStringBuilder CreateBuilder(string connStr)
    {
        return new MySqlConnectionStringBuilder(connStr);
    }

    protected override string GetDisplayName()
    {
        return $"{_builder.Database} on {_builder.Server} (MySQL)";
    }

    protected override DatabaseConnectionForm.BasicOptions GetBasicOptions(MySqlConnectionStringBuilder builder)
    {
        return new DatabaseConnectionForm.BasicOptions
        {
            Server = builder.Server,
            Database = builder.Database,
            Username = builder.UserID,
            Password = builder.Password,
        };
    }

    protected override void SetBasicOptions(
        MySqlConnectionStringBuilder builder,
        DatabaseConnectionForm.BasicOptions opt
    )
    {
        builder.Server = opt.Server;
        builder.Database = opt.Database;
        builder.UserID = opt.Username;
        builder.Password = opt.Password;
    }

    protected override string GetDefaultConnectionString() => Settings.Default.MySqlLastConnectionString;

    protected override void SetDefaultConnectionString(string str) => Settings.Default.MySqlLastConnectionString = str;
}
