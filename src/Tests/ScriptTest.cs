using System;
using System.IO;
using System.Linq;
using System.Text;
using SqlNotebook;
using SqlNotebookScript.Core;
using SqlNotebookScript.Utils;

namespace Tests;

[TestClass]
public sealed partial class ScriptTest
{
    [ClassInitialize]
    public static void Init(TestContext context) => GlobalInit.Init();

    [TestMethod]
    public void TestSaveStatement()
    {
        // Make a temp directory to store notebook files in.
        var tempDir = Path.Combine(Path.GetTempPath(), "SqlNotebookSaveTest");
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }
        Directory.CreateDirectory(tempDir);

        try
        {
            // Test 1: Try to save an untitled notebook without filename - should fail
            {
                using Notebook notebook = Notebook.New();
                NotebookManager manager = new(notebook, new());

                // Clear default items
                foreach (var item in manager.Items.ToList())
                {
                    manager.DeleteItem(item);
                }

                // Add a test script
                manager.NewItem(NotebookItemType.Script, "TestScript", "SAVE;");

                // Execute should fail
                Exception thrownException = null;
                try
                {
                    using var output = manager.ExecuteScript("SAVE;");
                    Assert.Fail("Expected an exception but none was thrown");
                }
                catch (Exception ex)
                {
                    thrownException = ex;
                }

                Assert.IsTrue(
                    thrownException.Message.Contains("SAVE: No filename was specified and the notebook is untitled")
                );
            }

            // Test 2: Save with filename and verify by reopening
            var testFilePath = Path.Combine(tempDir, "test1.sqlnb");
            {
                using Notebook notebook = Notebook.New();
                NotebookManager manager = new(notebook, new());

                // Clear default items and add test data
                foreach (var item in manager.Items.ToList())
                {
                    manager.DeleteItem(item);
                }

                manager.NewItem(
                    NotebookItemType.Script,
                    "Script1",
                    "CREATE TABLE test (id INTEGER, name TEXT); INSERT INTO test VALUES (1, 'Hello');"
                );
                manager.NewItem(NotebookItemType.Script, "SaveScript", $"SAVE '{testFilePath}';");

                // Execute the save
                using var output = manager.ExecuteScript($"SAVE '{testFilePath}';");

                // Verify file was created
                Assert.IsTrue(File.Exists(testFilePath), "Save file should exist");
            }

            // Reopen and verify the data is there
            {
                using var reopenedNotebook = Notebook.Open(testFilePath);
                var scripts = reopenedNotebook.GetScripts();

                Assert.IsTrue(scripts.ContainsKey("script1"), "Script1 should exist");
                Assert.IsTrue(scripts.ContainsKey("savescript"), "SaveScript should exist");
                Assert.IsTrue(
                    scripts["script1"].Contains("CREATE TABLE test"),
                    "Script1 should contain the CREATE TABLE statement"
                );
            }

            // Test 3: Save without filename after setting the filename - should work
            var testFilePath2 = Path.Combine(tempDir, "test2.sqlnb");
            {
                using Notebook notebook = Notebook.New();
                NotebookManager manager = new(notebook, new());

                // Clear default items and add test data
                foreach (var item in manager.Items.ToList())
                {
                    manager.DeleteItem(item);
                }

                manager.NewItem(
                    NotebookItemType.Script,
                    "Script2",
                    "CREATE TABLE test2 (value TEXT); INSERT INTO test2 VALUES ('World');"
                );

                // First save with filename to establish the path
                using (var output = manager.ExecuteScript($"SAVE '{testFilePath2}';"))
                {
                    // Verify file was created
                    Assert.IsTrue(File.Exists(testFilePath2), "First save should create file");
                }

                // Add more data
                manager.NewItem(NotebookItemType.Script, "Script3", "INSERT INTO test2 VALUES ('Updated');");

                // Save without filename - should work now
                using (var output = manager.ExecuteScript("SAVE;"))
                {
                    // Should not throw an exception
                }

                // Verify the changes are persisted
                using var reopenedNotebook = Notebook.Open(testFilePath2);
                var scripts = reopenedNotebook.GetScripts();

                Assert.IsTrue(scripts.ContainsKey("script2"), "Script2 should exist");
                Assert.IsTrue(scripts.ContainsKey("script3"), "Script3 should exist");
                Assert.IsTrue(scripts["script3"].Contains("Updated"), "Script3 should contain the update");
            }

            // Test 4: Error handling - invalid filename
            {
                using Notebook notebook = Notebook.New();
                NotebookManager manager = new(notebook, new());

                var invalidPath = Path.Combine(tempDir, "nonexistent", "invalid.sqlnb");

                Exception thrownException = null;
                try
                {
                    using var output = manager.ExecuteScript($"SAVE '{invalidPath}';");
                    Assert.Fail("Expected an exception but none was thrown");
                }
                catch (Exception ex)
                {
                    thrownException = ex;
                }

                Assert.IsTrue(thrownException.Message.StartsWith("SAVE:"), "Error should be prefixed with SAVE:");
            }
        }
        finally
        {
            // Clean up
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    private void TestScript(string scriptRelativePath)
    {
        // Make a temp directory to store notebook files in.
        var tempDir = Path.Combine(Path.GetTempPath(), "SqlNotebookScriptTest");
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }

        // Find the Tests directory by searching in the filesystem above this assembly.
        var testsDir = TestUtil.GetTestsDir();

        // Locate the files directory.
        var filesDir = Path.Combine(testsDir, "files");

        // Locate the .sql file.
        var scriptsDir = Path.Combine(testsDir, "scripts");
        var scriptFilePath = Path.Combine(scriptsDir, scriptRelativePath);
        Directory.CreateDirectory(tempDir);
        string expectedOutput = "",
            actualOutput;
        try
        {
            // Parse the file into SQL text(s) and expected output text.
            var scriptFileText = File.ReadAllText(scriptFilePath)
                .Replace("<TEMP>", tempDir)
                .Replace("<FILES>", filesDir)
                .Replace("\r\n", "\n") // Normalize to LF first
                .Replace("\n", "\r\n"); // Convert all to CRLF
            const string OUTPUT_SEPARATOR = "\r\n--output--\r\n";
            if (scriptFileText.Contains(OUTPUT_SEPARATOR))
            {
                var parts = scriptFileText.Split(OUTPUT_SEPARATOR, 2);
                scriptFileText = parts[0];
                expectedOutput = parts[1];
            }
            const string SCRIPT_SEPARATOR = "\r\n--script--\r\n";
            var sqls = scriptFileText.Split(SCRIPT_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

            // Run the SQL.
            using Notebook notebook = Notebook.New();
            NotebookManager manager = new(notebook, new());
            foreach (var item in manager.Items.ToList())
            {
                manager.DeleteItem(item);
            }
            var scriptNumber = 1;
            foreach (var sql in sqls)
            {
                manager.NewItem(NotebookItemType.Script, $"Script{scriptNumber++}", sql);
            }
            using var output = manager.ExecuteScript(sqls[0]);

            // Convert the actual ScriptOutput to text.
            StringBuilder sb = new();
            if (output.ScalarResult != null)
            {
                sb.AppendLine(ResultObjectToString(output.ScalarResult));
            }
            foreach (var line in output.TextOutput)
            {
                sb.AppendLine(line);
            }
            foreach (var table in output.DataTables)
            {
                sb.AppendLine(string.Join(",", table.Columns));
                foreach (var row in table.Rows)
                {
                    sb.AppendLine(string.Join(",", row.Select(ResultObjectToString)));
                }
                sb.AppendLine("-");
            }
            actualOutput = sb.ToString();
        }
        catch (Exception ex)
        {
            throw new Exception($"{scriptFilePath[scriptsDir.Length..]} failed. {ex.GetExceptionMessage()}", ex);
        }
        Assert.AreEqual(expectedOutput, actualOutput, scriptFilePath[scriptsDir.Length..]);

        // Only delete the temp dir on success so that we can look at the files on failure.
        Directory.Delete(tempDir, true);
    }

    private static string ResultObjectToString(object obj) =>
        obj switch
        {
            DBNull => "null",
            double x => $"{x:0.####}",
            byte[] x => BlobUtil.ToString(x),
            _ => $"{obj}",
        };
}
