using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SqlNotebookScript;
using SqlNotebookScript.Core;
using SqlNotebookScript.Interpreter;
using SqlNotebookScript.Utils;

namespace SqlNotebookCmd;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            // Check arguments
            if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h" || args[0] == "/?"))
            {
                ShowUsage();
                return 0;
            }

            if (args.Length != 2)
            {
                ShowUsage();
                return 1;
            }

            var notebookPath = args[0];
            var scriptName = args[1];

            // Validate notebook file exists
            if (!File.Exists(notebookPath))
            {
                Console.Error.WriteLine($"Error: Notebook file does not exist: {notebookPath}");
                return 1;
            }

            // Initialize SQLite
            Notebook.InitSqlite();

            // Open the notebook
            using var notebook = Notebook.Open(notebookPath);

            // Find the script
            var scriptRecord = notebook
                .UserData.Items.OfType<ScriptNotebookItemRecord>()
                .FirstOrDefault(x => string.Equals(x.Name, scriptName, StringComparison.OrdinalIgnoreCase));

            if (scriptRecord == null)
            {
                Console.Error.WriteLine($"Error: Script '{scriptName}' not found in notebook");
                return 1;
            }

            if (string.IsNullOrEmpty(scriptRecord.Sql))
            {
                Console.Error.WriteLine($"Error: Script '{scriptName}' has no content");
                return 1;
            }

            // Parse and execute the script
            var parser = new ScriptParser(notebook);
            var script = parser.Parse(scriptRecord.Sql);
            var runner = new ScriptRunner(notebook, notebook.GetScripts());
            using var output = runner.Execute(script, new Dictionary<string, object>());

            // Output scalar result if present
            if (output.ScalarResult != null)
            {
                Console.WriteLine(ResultObjectToString(output.ScalarResult));
            }

            // Output text output
            foreach (var line in output.TextOutput)
            {
                Console.WriteLine(line);
            }

            // Output data tables in CSV format
            for (int i = 0; i < output.DataTables.Count; i++)
            {
                var table = output.DataTables[i];

                // Header row
                Console.WriteLine(string.Join(",", table.Columns));

                // Data rows
                foreach (var row in table.Rows)
                {
                    Console.WriteLine(string.Join(",", row.Select(ResultObjectToString)));
                }

                // Add blank line between tables (but not after the last one)
                if (i < output.DataTables.Count - 1)
                {
                    Console.WriteLine();
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.GetExceptionMessage()}");
            return 1;
        }
    }

    private static void ShowUsage()
    {
        Console.WriteLine("SQL Notebook Command Line Interface");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  SqlNotebookCmd <notebook-file> <script-name>");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <notebook-file>  Path to the .sqlnb file");
        Console.WriteLine("  <script-name>    Name of the script to execute");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --help, -h, /?   Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  SqlNotebookCmd \"C:\\data\\mynotebook.sqlnb\" \"MyScript\"");
        Console.WriteLine();
        Console.WriteLine("Output:");
        Console.WriteLine("  Tables are output in CSV format with headers.");
        Console.WriteLine("  Multiple tables are separated by blank lines.");
        Console.WriteLine("  On success, exit code is 0. On error, exit code is 1.");
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
