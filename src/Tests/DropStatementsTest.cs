using System;
using System.IO;
using System.Linq;
using SqlNotebook;
using SqlNotebookScript.Core;

namespace Tests;

[TestClass]
public sealed class DropStatementsTest
{
    [ClassInitialize]
    public static void Init(TestContext context) => GlobalInit.Init();

    [TestMethod]
    public void TestDropScript()
    {
        using Notebook notebook = Notebook.New();
        NotebookManager manager = new(notebook, new());

        // Clear default items
        foreach (var item in manager.Items.ToList())
        {
            manager.DeleteItem(item);
        }

        // Create a test script
        var scriptName = "TestScript";
        manager.NewItem(NotebookItemType.Script, scriptName, "PRINT 'Hello World';");

        // Verify the script exists
        Assert.IsTrue(manager.Items.Any(x => x.Name == scriptName && x.Type == NotebookItemType.Script));
        Assert.IsTrue(notebook.UserData.Items.Any(x => x.Name == scriptName));

        // Create another script that will drop the first one
        var dropperScript = "DROP SCRIPT TestScript;";
        manager.NewItem(NotebookItemType.Script, "DropperScript", dropperScript);

        // Execute the DROP SCRIPT statement
        using var output = manager.ExecuteScript(dropperScript);

        // Refresh the manager's view of items
        manager.Rescan(notebookItemsOnly: true);

        // Verify the script was deleted
        Assert.IsTrue(!manager.Items.Any(x => x.Name == scriptName && x.Type == NotebookItemType.Script));
        Assert.IsTrue(!notebook.UserData.Items.Any(x => x.Name == scriptName));
    }

    [TestMethod]
    public void TestDropPage()
    {
        using Notebook notebook = Notebook.New();
        NotebookManager manager = new(notebook, new());

        // Clear default items
        foreach (var item in manager.Items.ToList())
        {
            manager.DeleteItem(item);
        }

        // Create a test page
        var pageName = "TestPage";
        manager.NewItem(NotebookItemType.Page, pageName);

        // Verify the page exists
        Assert.IsTrue(manager.Items.Any(x => x.Name == pageName && x.Type == NotebookItemType.Page));
        Assert.IsTrue(notebook.UserData.Items.Any(x => x.Name == pageName));

        // Create a script that will drop the page
        var dropperScript = "DROP PAGE TestPage;";
        manager.NewItem(NotebookItemType.Script, "DropperScript", dropperScript);

        // Execute the DROP PAGE statement
        using var output = manager.ExecuteScript(dropperScript);

        // Refresh the manager's view of items
        manager.Rescan(notebookItemsOnly: true);

        // Verify the page was deleted
        Assert.IsTrue(!manager.Items.Any(x => x.Name == pageName && x.Type == NotebookItemType.Page));
        Assert.IsTrue(!notebook.UserData.Items.Any(x => x.Name == pageName));
    }

    [TestMethod]
    public void TestDropScriptWithExpression()
    {
        using Notebook notebook = Notebook.New();
        NotebookManager manager = new(notebook, new());

        // Clear default items
        foreach (var item in manager.Items.ToList())
        {
            manager.DeleteItem(item);
        }

        // Create a test script
        var scriptName = "TestScript123";
        manager.NewItem(NotebookItemType.Script, scriptName, "PRINT 'Hello World';");

        // Verify the script exists
        Assert.IsTrue(manager.Items.Any(x => x.Name == scriptName && x.Type == NotebookItemType.Script));

        // Create another script that will drop the first one using an expression
        var dropperScript = "DROP SCRIPT ('Test' || 'Script' || '123');";
        manager.NewItem(NotebookItemType.Script, "DropperScript", dropperScript);

        // Execute the DROP SCRIPT statement
        using var output = manager.ExecuteScript(dropperScript);

        // Refresh the manager's view of items
        manager.Rescan(notebookItemsOnly: true);

        // Verify the script was deleted
        Assert.IsTrue(!manager.Items.Any(x => x.Name == scriptName && x.Type == NotebookItemType.Script));
    }

    [TestMethod]
    public void TestDropNonexistentScript()
    {
        using Notebook notebook = Notebook.New();
        NotebookManager manager = new(notebook, new());

        // Clear default items
        foreach (var item in manager.Items.ToList())
        {
            manager.DeleteItem(item);
        }

        // Create a script that tries to drop a nonexistent script
        var dropperScript = "DROP SCRIPT NonexistentScript;";
        manager.NewItem(NotebookItemType.Script, "DropperScript", dropperScript);

        // Execute the DROP SCRIPT statement - should throw an exception
        Exception thrownException = null;
        try
        {
            using var output = manager.ExecuteScript(dropperScript);
            Assert.Fail("Expected an exception but none was thrown");
        }
        catch (Exception ex)
        {
            thrownException = ex;
        }

        Assert.IsTrue(thrownException.Message.Contains("There is no script named \"NonexistentScript\""));
    }

    [TestMethod]
    public void TestDropNonexistentPage()
    {
        using Notebook notebook = Notebook.New();
        NotebookManager manager = new(notebook, new());

        // Clear default items
        foreach (var item in manager.Items.ToList())
        {
            manager.DeleteItem(item);
        }

        // Create a script that tries to drop a nonexistent page
        var dropperScript = "DROP PAGE NonexistentPage;";
        manager.NewItem(NotebookItemType.Script, "DropperScript", dropperScript);

        // Execute the DROP PAGE statement - should throw an exception
        Exception thrownException = null;
        try
        {
            using var output = manager.ExecuteScript(dropperScript);
            Assert.Fail("Expected an exception but none was thrown");
        }
        catch (Exception ex)
        {
            thrownException = ex;
        }

        Assert.IsTrue(thrownException.Message.Contains("There is no page named \"NonexistentPage\""));
    }

    [TestMethod]
    public void TestDropScriptCaseInsensitive()
    {
        using Notebook notebook = Notebook.New();
        NotebookManager manager = new(notebook, new());

        // Clear default items
        foreach (var item in manager.Items.ToList())
        {
            manager.DeleteItem(item);
        }

        // Create a test script
        var scriptName = "TestScript";
        manager.NewItem(NotebookItemType.Script, scriptName, "PRINT 'Hello World';");

        // Verify the script exists
        Assert.IsTrue(manager.Items.Any(x => x.Name == scriptName && x.Type == NotebookItemType.Script));

        // Create another script that will drop the first one using different case
        var dropperScript = "DROP SCRIPT testscript;";
        manager.NewItem(NotebookItemType.Script, "DropperScript", dropperScript);

        // Execute the DROP SCRIPT statement
        using var output = manager.ExecuteScript(dropperScript);

        // Refresh the manager's view of items
        manager.Rescan(notebookItemsOnly: true);

        // Verify the script was deleted (case-insensitive)
        Assert.IsTrue(!manager.Items.Any(x => x.Name == scriptName && x.Type == NotebookItemType.Script));
    }

    [TestMethod]
    public void TestDropPageCaseInsensitive()
    {
        using Notebook notebook = Notebook.New();
        NotebookManager manager = new(notebook, new());

        // Clear default items
        foreach (var item in manager.Items.ToList())
        {
            manager.DeleteItem(item);
        }

        // Create a test page
        var pageName = "TestPage";
        manager.NewItem(NotebookItemType.Page, pageName);

        // Verify the page exists
        Assert.IsTrue(manager.Items.Any(x => x.Name == pageName && x.Type == NotebookItemType.Page));

        // Create a script that will drop the page using different case
        var dropperScript = "DROP PAGE testpage;";
        manager.NewItem(NotebookItemType.Script, "DropperScript", dropperScript);

        // Execute the DROP PAGE statement
        using var output = manager.ExecuteScript(dropperScript);

        // Refresh the manager's view of items
        manager.Rescan(notebookItemsOnly: true);

        // Verify the page was deleted (case-insensitive)
        Assert.IsTrue(!manager.Items.Any(x => x.Name == pageName && x.Type == NotebookItemType.Page));
    }
}
