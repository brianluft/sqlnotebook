using System.IO;
using System.Linq;
using SqlNotebookScript;
using SqlNotebookScript.Core;

namespace Tests;

[TestClass]
public sealed class FileVersionTest
{
    [ClassInitialize]
    public static void Init(TestContext context) => GlobalInit.Init();

    [TestMethod]
    public void TestOpenV1()
    {
        var filePath = Path.Combine(TestUtil.GetTestsDir(), "files", "v1.sqlnb");
        using var notebook = Notebook.Open(filePath);
        Assert.AreEqual(2, notebook.UserData.Items.Count);
        var items = notebook.UserData.Items.OrderBy(x => x.Name).ToList();

        Assert.IsInstanceOfType(items[0], typeof(PageNotebookItemRecord));
        Assert.AreEqual("Note1", items[0].Name);
        var note1 = (PageNotebookItemRecord)items[0];
        Assert.AreEqual(1, note1.Blocks.Count);
        Assert.IsInstanceOfType(note1.Blocks[0], typeof(TextPageBlockRecord));
        var textBlock = (TextPageBlockRecord)note1.Blocks[0];
        Assert.AreEqual(
            @"Hello world!

1
2
3

4
5
6

7
8
9",
            textBlock.Content
        );

        Assert.IsInstanceOfType(items[1], typeof(ScriptNotebookItemRecord));
        Assert.AreEqual("Script1", items[1].Name);
        var script1 = (ScriptNotebookItemRecord)items[1];
        Assert.AreEqual(1, script1.Parameters.Count);
        Assert.AreEqual("@a", script1.Parameters[0]);
        Assert.AreEqual(
            @"DECLARE PARAMETER @a;
SELECT * FROM sqlite_master
",
            script1.Sql
        );
    }
}
