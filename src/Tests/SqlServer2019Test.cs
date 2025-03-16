using System;
using System.Data.Common;
using Microsoft.Data;
using Microsoft.Data.SqlClient;
using SqlNotebook;
using SqlNotebookScript.Core;
using SqlNotebookScript.Utils;

namespace Tests;

[TestClass]
public sealed class SqlServer2019Test
{
    [ClassInitialize]
    public static void Init(TestContext context) => GlobalInit.Init();

    private static void Execute(DbConnection connection, string sql)
    {
        using var c = connection.CreateCommand();
        c.CommandText = sql;
        c.ExecuteNonQuery();
    }

    private static string SetupSqlServer2019(bool caseSensitive)
    {
        SqlConnection connection =
            new(@"Data Source=localhost\SQLEXPRESS;Initial Catalog=master;Integrated Security=True;Encrypt=False");
        connection.Open();
        var collateCase = caseSensitive ? "CS" : "CI";
        Execute(connection, $"DROP DATABASE IF EXISTS sqlnotebook_test_{collateCase};");
        Execute(
            connection,
            $"CREATE DATABASE sqlnotebook_test_{collateCase} COLLATE SQL_Latin1_General_CP1_{collateCase}_AS;"
        );
        Execute(
            connection,
            @$"
            USE sqlnotebook_test_{collateCase};
            CREATE TABLE foo (
                a INT PRIMARY KEY,
                b VARCHAR(100),
                c BIT,
                d DATE,
                e DATETIME,
                f DATETIMEOFFSET,
                g VARBINARY(MAX)
            );
            INSERT INTO foo VALUES
                (111, 'HELLO', 1, '2010-02-03', '2013-04-05 06:45:15.123', '2013-04-05 06:45:15.123 -04:00', 123456),
                (222, NULL, NULL, NULL, NULL, NULL, NULL);
        "
        );
        return @$"Data Source=localhost\SQLEXPRESS;Initial Catalog=sqlnotebook_test_{collateCase};Integrated Security=True;Encrypt=False";
    }

    [TestMethod]
    public void ImportDatabase_SqlServer2019_CaseSensitive()
    {
        ImportDatabase_SqlServer2019(true);
    }

    [TestMethod]
    public void ImportDatabase_SqlServer2019_CaseInsensitive()
    {
        ImportDatabase_SqlServer2019(false);
    }

    public void ImportDatabase_SqlServer2019(bool caseSensitive)
    {
        var connectionString = SetupSqlServer2019(caseSensitive);
        using var notebook = Notebook.New();
        NotebookManager manager = new(notebook, new());
        manager.ExecuteScript($"IMPORT DATABASE 'mssql' CONNECTION {connectionString.SingleQuote()} TABLE foo;");
        Assert.AreEqual(
            @"CREATE TABLE ""foo"" (""a"" INTEGER, ""b"" TEXT, ""c"" INTEGER, ""d"" TEXT, ""e"" TEXT, ""f"" TEXT, ""g"" BLOB, PRIMARY KEY (""a""))",
            (string)notebook.QueryValue("SELECT sql FROM sqlite_master WHERE name = 'foo';")
        );
        using var sdt = notebook.Query("SELECT * FROM foo ORDER BY a;", Array.Empty<object>());
        Assert.AreEqual(2, sdt.Rows.Count);
        Assert.AreEqual(7, sdt.Columns.Count);
        Assert.AreEqual("a", sdt.Columns[0]);
        Assert.AreEqual("b", sdt.Columns[1]);
        Assert.AreEqual("c", sdt.Columns[2]);
        Assert.AreEqual("d", sdt.Columns[3]);
        Assert.AreEqual("e", sdt.Columns[4]);
        Assert.AreEqual("f", sdt.Columns[5]);
        Assert.AreEqual("g", sdt.Columns[6]);

        Assert.AreEqual((long)111, sdt.Rows[0][0]);
        Assert.AreEqual("HELLO", sdt.Rows[0][1]);
        Assert.AreEqual((long)1, sdt.Rows[0][2]);
        Assert.AreEqual("2010-02-03 00:00:00.000", sdt.Rows[0][3]);
        Assert.AreEqual("2013-04-05 06:45:15.123", sdt.Rows[0][4]);
        Assert.AreEqual("2013-04-05 10:45:15.123", sdt.Rows[0][5]);
        Assert.AreEqual("00-01-E2-40", BitConverter.ToString((byte[])sdt.Rows[0][6]));

        Assert.AreEqual((long)222, sdt.Rows[1][0]);
        for (var i = 1; i <= 6; i++)
        {
            Assert.IsInstanceOfType(sdt.Rows[1][i], typeof(DBNull));
        }
    }
}
