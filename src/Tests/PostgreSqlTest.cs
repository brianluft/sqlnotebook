using System;
using System.Data.Common;
using Npgsql;
using SqlNotebook;
using SqlNotebookScript.Core;
using SqlNotebookScript.Utils;

namespace Tests;

[TestClass]
public sealed class PostgreSqlTest
{
    [ClassInitialize]
    public static void Init(TestContext context) => GlobalInit.Init();

    private static void Execute(DbConnection connection, string sql)
    {
        using var c = connection.CreateCommand();
        c.CommandText = sql;
        c.ExecuteNonQuery();
    }

    private static string SetupPostgreSql()
    {
        NpgsqlConnectionStringBuilder connectionStringBuilder = new();
        connectionStringBuilder.Host = "localhost";
        connectionStringBuilder.Username = "postgres";
        connectionStringBuilder.Password = "password";
        connectionStringBuilder.Database = "postgres";
        using (NpgsqlConnection c = new(connectionStringBuilder.ToString()))
        {
            c.Open();
            Execute(c, "DROP DATABASE IF EXISTS sqlnotebook_test");
            Execute(c, "CREATE DATABASE sqlnotebook_test");
        }

        connectionStringBuilder.Database = "sqlnotebook_test";
        using NpgsqlConnection connection = new(connectionStringBuilder.ToString());
        connection.Open();
        Execute(
            connection,
            @$"
            CREATE TABLE foo (
                a integer PRIMARY KEY,
                b character varying (100),
                c bit,
                d date,
                e timestamp without time zone,
                f timestamp with time zone,
                g bit varying,
                h json,
                i money,
                j text,
                k bigint,
                l boolean,
                m bytea
            );
            INSERT INTO foo VALUES
                (111, 'HELLO', 1::bit, '2010-02-03', '2013-04-05 06:45:15.123', '2013-04-05 06:45:15.123 -04:00', B'1101',
                    '{{""foo"": ""bar""}}', 123.45, 'foo', 1234567890, true, '\xA1B2C3'),
                (222, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
        "
        );
        return connectionStringBuilder.ToString();
    }

    [TestMethod]
    public void ImportDatabase_PostgreSql()
    {
        var connectionString = SetupPostgreSql();
        using var notebook = Notebook.New();
        NotebookManager manager = new(notebook, new());
        manager.ExecuteScript($"IMPORT DATABASE 'pgsql' CONNECTION {connectionString.SingleQuote()} TABLE foo;");
        Assert.AreEqual(
            @"CREATE TABLE ""foo"" (""a"" INTEGER, ""b"" TEXT, ""c"" INTEGER, ""d"" TEXT, ""e"" TEXT, ""f"" TEXT, ""g"" BLOB, ""h"" TEXT, ""i"" REAL, ""j"" TEXT, ""k"" INTEGER, ""l"" INTEGER, ""m"" BLOB, PRIMARY KEY (""a""))",
            (string)notebook.QueryValue("SELECT sql FROM sqlite_master WHERE name = 'foo';")
        );
        using var sdt = notebook.Query("SELECT * FROM foo ORDER BY a;", Array.Empty<object>());
        Assert.AreEqual(2, sdt.Rows.Count);
        Assert.AreEqual(13, sdt.Columns.Count);
        Assert.AreEqual("a", sdt.Columns[0]);
        Assert.AreEqual("b", sdt.Columns[1]);
        Assert.AreEqual("c", sdt.Columns[2]);
        Assert.AreEqual("d", sdt.Columns[3]);
        Assert.AreEqual("e", sdt.Columns[4]);
        Assert.AreEqual("f", sdt.Columns[5]);
        Assert.AreEqual("g", sdt.Columns[6]);
        Assert.AreEqual("h", sdt.Columns[7]);
        Assert.AreEqual("i", sdt.Columns[8]);
        Assert.AreEqual("j", sdt.Columns[9]);
        Assert.AreEqual("k", sdt.Columns[10]);
        Assert.AreEqual("l", sdt.Columns[11]);
        Assert.AreEqual("m", sdt.Columns[12]);

        Assert.AreEqual((long)111, sdt.Rows[0][0]);
        Assert.AreEqual("HELLO", sdt.Rows[0][1]);
        Assert.AreEqual((long)1, sdt.Rows[0][2]);
        Assert.AreEqual("2010-02-03 00:00:00.000", sdt.Rows[0][3]);
        Assert.AreEqual("2013-04-05 06:45:15.123", sdt.Rows[0][4]);
        Assert.AreEqual("2013-04-05 10:45:15.123", sdt.Rows[0][5]);
        Assert.IsInstanceOfType(sdt.Rows[0][6], typeof(byte[]));
        var g = ArrayUtil.GetArrayElements((byte[])sdt.Rows[0][6]);
        Assert.AreEqual("1 1 0 1", string.Join(' ', g));
        Assert.AreEqual("{\"foo\": \"bar\"}", sdt.Rows[0][7]);
        Assert.AreEqual(123.45d, sdt.Rows[0][8]);
        Assert.AreEqual("foo", sdt.Rows[0][9]);
        Assert.AreEqual((long)1234567890, sdt.Rows[0][10]);
        Assert.AreEqual((long)1, sdt.Rows[0][11]);
        Assert.AreEqual("A1-B2-C3", BitConverter.ToString((byte[])sdt.Rows[0][12]));

        Assert.AreEqual((long)222, sdt.Rows[1][0]);
        for (var i = 1; i <= 12; i++)
        {
            Assert.IsInstanceOfType(sdt.Rows[1][i], typeof(DBNull));
        }
    }
}
