using System;
using System.Data.Common;
using MySql.Data.MySqlClient;
using SqlNotebook;
using SqlNotebookScript.Core;
using SqlNotebookScript.Utils;

namespace Tests;

[TestClass]
public sealed class MySqlTest
{
    [ClassInitialize]
    public static void Init(TestContext context) => GlobalInit.Init();

    private static void Execute(DbConnection connection, string sql)
    {
        using var c = connection.CreateCommand();
        c.CommandText = sql;
        c.ExecuteNonQuery();
    }

    private static string SetupMySql()
    {
        MySqlConnectionStringBuilder connectionStringBuilder = new();
        connectionStringBuilder.Server = "localhost";
        connectionStringBuilder.UserID = "root";
        connectionStringBuilder.Password = "password";
        connectionStringBuilder.Database = "sys";
        using (MySqlConnection c = new(connectionStringBuilder.ToString()))
        {
            c.Open();
            Execute(c, "DROP DATABASE IF EXISTS sqlnotebook_test");
            Execute(c, "CREATE DATABASE sqlnotebook_test");
        }

        connectionStringBuilder.Database = "sqlnotebook_test";
        using MySqlConnection connection = new(connectionStringBuilder.ToString());
        connection.Open();
        Execute(
            connection,
            @$"
            CREATE TABLE foo (
                c00 INTEGER PRIMARY KEY,
                c01 INT,
                c02 SMALLINT,
                c03 TINYINT,
                c04 MEDIUMINT,
                c05 BIGINT,
                c06 DECIMAL(19,2),
                c07 NUMERIC(19,2),
                c08 FLOAT,
                c09 DOUBLE,
                c10 BIT,
                c11 DATE,
                c12 DATETIME,
                c13 TIMESTAMP,
                c14 TIME,
                c15 YEAR,
                c16 CHAR(10),
                c17 VARCHAR(10),
                c18 BINARY(2),
                c19 VARBINARY(4),
                c20 BLOB,
                c21 TEXT,
                c22 ENUM('a', 'b', 'c'),
                c23 SET('a', 'b', 'c'),
                c24 JSON
            );
            INSERT INTO foo VALUES (
                111,
                -2147483648,
                -32768,
                -128,
                -8388608,
                -9999999999,
                123456789.01,
                123456789.01,
                1.1,
                2.2,
                1,
                '2021-01-02',
                '2021-01-02 13:04:05',
                '1970-01-01 00:00:01',
                '13:14:15',
                1901,
                'ABCDEFGHIJ',
                'ABCDEFGHIJ',
                'ab',
                'abc',
                'aaa',
                'ABC',
                'c',
                'a,b',
                '{{""foo"": ""bar""}}'
            ), (
                222, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
                NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
        "
        );
        return connectionStringBuilder.ToString();
    }

    [TestMethod]
    public void ImportDatabase_MySql()
    {
        var connectionString = SetupMySql();
        using var notebook = Notebook.New();
        NotebookManager manager = new(notebook, new());
        manager.ExecuteScript($"IMPORT DATABASE 'mysql' CONNECTION {connectionString.SingleQuote()} TABLE foo;");
        Assert.AreEqual(
            @"CREATE TABLE ""foo"" (""c00"" INTEGER, ""c01"" INTEGER, ""c02"" INTEGER, ""c03"" INTEGER, ""c04"" INTEGER, ""c05"" INTEGER, ""c06"" REAL, ""c07"" REAL, ""c08"" REAL, ""c09"" REAL, ""c10"" INTEGER, ""c11"" TEXT, ""c12"" TEXT, ""c13"" TEXT, ""c14"" TEXT, ""c15"" INTEGER, ""c16"" TEXT, ""c17"" TEXT, ""c18"" BLOB, ""c19"" BLOB, ""c20"" BLOB, ""c21"" TEXT, ""c22"" TEXT, ""c23"" TEXT, ""c24"" TEXT, PRIMARY KEY (""c00""))",
            (string)notebook.QueryValue("SELECT sql FROM sqlite_master WHERE name = 'foo';")
        );
        using var sdt = notebook.Query("SELECT * FROM foo ORDER BY c00;", Array.Empty<object>());
        Assert.AreEqual(2, sdt.Rows.Count);
        Assert.AreEqual(25, sdt.Columns.Count);
        for (var i = 0; i < 25; i++)
        {
            Assert.AreEqual($"c{i:00}", sdt.Columns[i]);
        }

        Assert.AreEqual((long)111, sdt.Rows[0][0]);
        Assert.AreEqual((long)-2147483648, sdt.Rows[0][1]);
        Assert.AreEqual((long)-32768, sdt.Rows[0][2]);
        Assert.AreEqual((long)-128, sdt.Rows[0][3]);
        Assert.AreEqual((long)-8388608, sdt.Rows[0][4]);
        Assert.AreEqual((long)-9999999999, sdt.Rows[0][5]);
        Assert.AreEqual(123456789.01d, sdt.Rows[0][6]);
        Assert.AreEqual(123456789.01d, sdt.Rows[0][7]);
        Assert.IsTrue(Math.Abs(1.1d - (double)sdt.Rows[0][8]) < 0.000001);
        Assert.IsTrue(Math.Abs(2.2d - (double)sdt.Rows[0][9]) < 0.000001);
        Assert.AreEqual((long)1, sdt.Rows[0][10]);
        Assert.AreEqual("2021-01-02 00:00:00.000", sdt.Rows[0][11]);
        Assert.AreEqual("2021-01-02 13:04:05.000", sdt.Rows[0][12]);
        Assert.AreEqual("1970-01-01 00:00:01.000", sdt.Rows[0][13]);
        Assert.AreEqual("13:14:15.000", sdt.Rows[0][14]);
        Assert.AreEqual((long)1901, sdt.Rows[0][15]);
        Assert.AreEqual("ABCDEFGHIJ", sdt.Rows[0][16]);
        Assert.AreEqual("ABCDEFGHIJ", sdt.Rows[0][17]);
        Assert.AreEqual("61-62", BitConverter.ToString((byte[])sdt.Rows[0][18]));
        Assert.AreEqual("61-62-63", BitConverter.ToString((byte[])sdt.Rows[0][19]));
        Assert.AreEqual("61-61-61", BitConverter.ToString((byte[])sdt.Rows[0][20]));
        Assert.AreEqual("ABC", sdt.Rows[0][21]);
        Assert.AreEqual("c", sdt.Rows[0][22]);
        Assert.AreEqual("a,b", sdt.Rows[0][23]);
        Assert.AreEqual("{\"foo\": \"bar\"}", sdt.Rows[0][24]);

        Assert.AreEqual((long)222, sdt.Rows[1][0]);
        for (var i = 1; i < 25; i++)
        {
            Assert.IsInstanceOfType(sdt.Rows[1][i], typeof(DBNull));
        }
    }
}
