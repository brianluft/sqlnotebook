using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SqlNotebookScript.Core;
using SqlNotebookScript.Interpreter;
using SqlNotebookScript.Utils;

namespace Tests;

[TestClass]
public sealed class SqliteGrammarTest
{
    [ClassInitialize]
    public static void Init(TestContext context) => GlobalInit.Init();

    [TestMethod]
    public void TestSqlStatements()
    {
        var filePath = Path.Combine(Path.GetTempPath(), "SqliteGrammarTestDb");
        File.WriteAllBytes(filePath, Array.Empty<byte>());

        // these are other SQLite DBs we will attach to, to test schema names
        var otherFilePath = Path.Combine(Path.GetTempPath(), "SqliteGrammarOtherDb");
        File.WriteAllBytes(otherFilePath, Resources.OtherDb);

        var other2FilePath = Path.Combine(Path.GetTempPath(), "SqliteGrammarOtherDb2");
        File.WriteAllBytes(other2FilePath, Resources.OtherDb);

        try
        {
            using var notebook = Notebook.New();
            notebook.Execute("ATTACH DATABASE ? AS other", new[] { filePath });

            var stmts =
                from line in Resources.SqlTests.Replace('\r', '\n').Split('\n')
                let shouldFail = line.StartsWith("<ERR>")
                let trimLine = line.Replace("<ERR>", "").Trim()
                where trimLine.Any() && !trimLine.StartsWith("--")
                select new { Cmd = trimLine, ShouldFail = shouldFail };

            var errors = new List<string>();

            foreach (var stmt in stmts)
            {
                // try first in SQLite directly to validate the test
                try
                {
                    notebook.Execute(
                        stmt.Cmd,
                        new Dictionary<string, object>
                        {
                            ["@other2_path"] = other2FilePath,
                            ["@int"] = 1,
                            ["@str"] = "hello",
                        }
                    );
                    if (stmt.ShouldFail)
                    {
                        errors.Add($"Should have failed, but SQLite accepted.\n{stmt.Cmd}");
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    if (!stmt.ShouldFail)
                    {
                        errors.Add(
                            $"Should have passed, but SQLite rejected.\n{stmt.Cmd}\n"
                                + $"SQLite error: "
                                + ex.GetExceptionMessage()
                        );
                        continue;
                    }
                }

                // then try with our parser to see if we match SQLite
                var tokens = Notebook.Tokenize(stmt.Cmd);
                var q = new TokenQueue(tokens, notebook);
                var result = SqliteParser.ReadStmt(q, out var ast);
                var success = result.InvalidMessage == null && result.IsValid && tokens.Count == result.NumValidTokens;
                if (stmt.ShouldFail && success)
                {
                    errors.Add($"Should have failed, but we accepted.\n{stmt.Cmd}");
                    continue;
                }
                else if (!stmt.ShouldFail && !success)
                {
                    errors.Add(
                        $"Should have passed, but we rejected.\n{stmt.Cmd}\n"
                            + $"InvalidMessage: {result.InvalidMessage}\nIsValid: {result.IsValid}\n"
                            + $"NumValidTokens: {result.NumValidTokens}"
                    );
                    continue;
                }
            }

            if (errors.Any())
            {
                Assert.Fail(string.Join("\n\n", errors));
            }
        }
        finally
        {
            File.Delete(filePath);
            File.Delete(otherFilePath);
            File.Delete(other2FilePath);
        }
    }
}
