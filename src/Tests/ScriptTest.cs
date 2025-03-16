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
                .Replace("<FILES>", filesDir);
            const string OUTPUT_SEPARATOR = "\r\n--output--\r\n";
            if (scriptFileText.Contains(OUTPUT_SEPARATOR))
            {
                var parts = scriptFileText.Split(OUTPUT_SEPARATOR);
                Assert.AreEqual(parts.Length, 2);
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
            _ => $"{obj}"
        };
}
