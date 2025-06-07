using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Tests;

[TestClass]
public sealed class SqlNotebookCmdTest
{
    [ClassInitialize]
    public static void Init(TestContext context) => GlobalInit.Init();

    [TestMethod]
    public void TestCliScript1()
    {
        // Find the Tests directory and the test notebook
        var testsDir = TestUtil.GetTestsDir();
        var testNotebookPath = Path.Combine(testsDir, "files", "cli_test.sqlnb");

        // Find SqlNotebookCmd.exe in the same directory as Tests.exe
        var testsExePath = Assembly.GetExecutingAssembly().Location;
        var testsExeDir = Path.GetDirectoryName(testsExePath);
        var cmdExePath = Path.Combine(testsExeDir, "SqlNotebookCmd.exe");

        Assert.IsTrue(File.Exists(cmdExePath), $"SqlNotebookCmd.exe not found at: {cmdExePath}");
        Assert.IsTrue(File.Exists(testNotebookPath), $"Test notebook not found at: {testNotebookPath}");

        // Run SqlNotebookCmd with the test notebook and Script1
        using var process = new Process();
        process.StartInfo.FileName = cmdExePath;
        process.StartInfo.Arguments = $"\"{testNotebookPath}\" \"Script1\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // Check exit code
        Assert.AreEqual(0, process.ExitCode, $"Expected exit code 0, got {process.ExitCode}. Stderr: {stderr}");

        // Check output matches expected format
        var expectedOutput =
            @"id,name
1,Hello
2,World

foo,bar
3,4";

        Assert.AreEqual(expectedOutput, stdout.TrimEnd(), "Output does not match expected format");
    }

    [TestMethod]
    public void TestCliScriptNotFound()
    {
        // Find the Tests directory and the test notebook
        var testsDir = TestUtil.GetTestsDir();
        var testNotebookPath = Path.Combine(testsDir, "files", "cli_test.sqlnb");

        // Find SqlNotebookCmd.exe in the same directory as Tests.exe
        var testsExePath = Assembly.GetExecutingAssembly().Location;
        var testsExeDir = Path.GetDirectoryName(testsExePath);
        var cmdExePath = Path.Combine(testsExeDir, "SqlNotebookCmd.exe");

        // Run SqlNotebookCmd with a non-existent script
        using var process = new Process();
        process.StartInfo.FileName = cmdExePath;
        process.StartInfo.Arguments = $"\"{testNotebookPath}\" \"NonExistentScript\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // Check exit code is 1 (error)
        Assert.AreEqual(1, process.ExitCode, "Expected exit code 1 for non-existent script");

        // Check error message
        Assert.IsTrue(
            stderr.Contains("Script 'NonExistentScript' not found"),
            $"Expected error message about script not found, got: {stderr}"
        );
    }

    [TestMethod]
    public void TestCliFileNotFound()
    {
        // Find SqlNotebookCmd.exe in the same directory as Tests.exe
        var testsExePath = Assembly.GetExecutingAssembly().Location;
        var testsExeDir = Path.GetDirectoryName(testsExePath);
        var cmdExePath = Path.Combine(testsExeDir, "SqlNotebookCmd.exe");

        // Run SqlNotebookCmd with a non-existent notebook file
        using var process = new Process();
        process.StartInfo.FileName = cmdExePath;
        process.StartInfo.Arguments = "\"nonexistent.sqlnb\" \"Script1\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // Check exit code is 1 (error)
        Assert.AreEqual(1, process.ExitCode, "Expected exit code 1 for non-existent file");

        // Check error message
        Assert.IsTrue(
            stderr.Contains("Notebook file does not exist"),
            $"Expected error message about file not found, got: {stderr}"
        );
    }

    [TestMethod]
    public void TestCliHelp()
    {
        // Find SqlNotebookCmd.exe in the same directory as Tests.exe
        var testsExePath = Assembly.GetExecutingAssembly().Location;
        var testsExeDir = Path.GetDirectoryName(testsExePath);
        var cmdExePath = Path.Combine(testsExeDir, "SqlNotebookCmd.exe");

        // Run SqlNotebookCmd with --help
        using var process = new Process();
        process.StartInfo.FileName = cmdExePath;
        process.StartInfo.Arguments = "--help";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // Check exit code is 0 (success)
        Assert.AreEqual(0, process.ExitCode, "Expected exit code 0 for help");

        // Check help text contains usage information
        Assert.IsTrue(
            stdout.Contains("SQL Notebook Command Line Interface"),
            $"Expected help text to contain title, got: {stdout}"
        );
        Assert.IsTrue(stdout.Contains("Usage:"), $"Expected help text to contain usage, got: {stdout}");
    }
}
