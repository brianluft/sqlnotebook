using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using SqlNotebookScript.Core;
using SqlNotebookScript.Interpreter;
using SqlNotebookScript.Utils;

namespace SqlNotebookScript;

public sealed class NotebookUserData : IDisposable
{
    private const int ITEM_TYPE_SCRIPT = 1;
    private const int ITEM_TYPE_PAGE = 2;

    public List<NotebookItemRecord> Items { get; set; } = new();

    public List<string> GetScriptParameters(string scriptName)
    {
        var item = Items.FirstOrDefault(x => x.Name == scriptName);
        return ((ScriptNotebookItemRecord)item)?.Parameters;
    }

    public static NotebookUserData Load(Notebook notebook)
    {
        NotebookUserData userData = new() { Items = new() };
        SqlUtil.WithTransaction(
            notebook,
            () =>
            {
                CreateTables(notebook);
                using var items = notebook.Query("SELECT * FROM _sqlnotebook_items;");
                using var scripts = notebook.Query("SELECT * FROM _sqlnotebook_scripts;");
                var scriptsById = scripts.Rows.ToDictionary(x => Convert.ToInt32(x[0]));
                using var scriptParameters = notebook.Query("SELECT * FROM _sqlnotebook_script_parameters;");
                var scriptParametersById = scriptParameters.Rows.ToLookup(x => Convert.ToInt32(x[0]));
                using var pageTextBlocks = notebook.Query("SELECT * FROM _sqlnotebook_page_text_blocks;");
                var pageTextBlocksById = pageTextBlocks.Rows.ToLookup(x => Convert.ToInt32(x[0]));
                using var pageQueryBlocks = notebook.Query("SELECT * FROM _sqlnotebook_page_query_blocks;");
                var pageQueryBlocksById = pageQueryBlocks.Rows.ToLookup(x => Convert.ToInt32(x[0]));

                foreach (var itemRow in items.Rows)
                {
                    var itemId = Convert.ToInt32(itemRow[0]);
                    var itemName = Convert.ToString(itemRow[1]);
                    var itemType = Convert.ToInt32(itemRow[2]);
                    switch (itemType)
                    {
                        case ITEM_TYPE_PAGE:
                            userData.Items.Add(
                                LoadPage(itemName, pageTextBlocksById[itemId], pageQueryBlocksById[itemId])
                            );
                            break;

                        case ITEM_TYPE_SCRIPT:
                            userData.Items.Add(LoadScript(itemName, scriptsById[itemId], scriptParametersById[itemId]));
                            break;

                        default:
                            break; // Ignore it.
                    }
                }
            }
        );
        return userData;
    }

    private static PageNotebookItemRecord LoadPage(
        string itemName,
        IEnumerable<object[]> textBlockRows,
        IEnumerable<object[]> queryBlockRows
    )
    {
        PageNotebookItemRecord page = new() { Name = itemName, Blocks = new() };
        var textBlockRowsByIndex = textBlockRows.ToDictionary(x => Convert.ToInt32(x[1]));
        var queryBlockRowsByIndex = queryBlockRows.ToDictionary(x => Convert.ToInt32(x[1]));
        var sortedIndices = textBlockRowsByIndex.Keys.Concat(queryBlockRowsByIndex.Keys).OrderBy(x => x);
        foreach (var index in sortedIndices)
        {
            if (textBlockRowsByIndex.TryGetValue(index, out var textBlockRow))
            {
                page.Blocks.Add(new TextPageBlockRecord { Content = Convert.ToString(textBlockRow[2]) });
            }
            else if (queryBlockRowsByIndex.TryGetValue(index, out var queryBlockRow))
            {
                var output =
                    queryBlockRow[3] != null && queryBlockRow[3] is not DBNull
                        ? ScriptOutput.FromBytes((byte[])queryBlockRow[3])
                        : null;
                page.Blocks.Add(
                    new QueryPageBlockRecord
                    {
                        Sql = Convert.ToString(queryBlockRow[2]),
                        Output = output,
                        Options = JsonSerializer.Deserialize<QueryPageBlockOptions>(Convert.ToString(queryBlockRow[4])),
                    }
                );
            }
        }
        return page;
    }

    private static ScriptNotebookItemRecord LoadScript(
        string itemName,
        object[] scriptRow,
        IEnumerable<object[]> scriptParameterRows
    )
    {
        ScriptNotebookItemRecord script = new()
        {
            Name = itemName,
            Sql = Convert.ToString(scriptRow[1]),
            Parameters = new(),
        };
        var scriptParameterRowsByIndex = scriptParameterRows.ToDictionary(x => Convert.ToInt32(x[1]));
        var sortedIndices = scriptParameterRowsByIndex.Keys.OrderBy(x => x);
        foreach (var index in sortedIndices)
        {
            var scriptParameterRow = scriptParameterRowsByIndex[index];
            script.Parameters.Add(Convert.ToString(scriptParameterRow[2]));
        }
        return script;
    }

    public void Save(Notebook notebook)
    {
        SqlUtil.WithTransaction(
            notebook,
            () =>
            {
                CreateTables(notebook);
                var itemId = 1;
                foreach (var item in Items)
                {
                    switch (item)
                    {
                        case ScriptNotebookItemRecord script:
                            SaveScript(notebook, script, itemId);
                            break;
                        case PageNotebookItemRecord page:
                            SavePage(notebook, page, itemId);
                            break;
                        default:
                            Debug.Fail("Unknown item type.");
                            break;
                    }
                    itemId++;
                }
            }
        );
    }

    private static void SaveScript(Notebook notebook, ScriptNotebookItemRecord script, int itemId)
    {
        notebook.Execute(
            "INSERT INTO _sqlnotebook_items VALUES (?, ?, ?);",
            new object[] { itemId, script.Name, ITEM_TYPE_SCRIPT }
        );
        notebook.Execute("INSERT INTO _sqlnotebook_scripts VALUES (?, ?);", new object[] { itemId, script.Sql });
        var parameterIndex = 0;
        foreach (var parameterName in script.Parameters)
        {
            notebook.Execute(
                "INSERT INTO _sqlnotebook_script_parameters VALUES (?, ?, ?);",
                new object[] { itemId, parameterIndex, parameterName }
            );
            parameterIndex++;
        }
    }

    private static void SavePage(Notebook notebook, PageNotebookItemRecord page, int itemId)
    {
        notebook.Execute(
            "INSERT INTO _sqlnotebook_items VALUES (?, ?, ?);",
            new object[] { itemId, page.Name, ITEM_TYPE_PAGE }
        );
        var index = 0;
        foreach (var block in page.Blocks)
        {
            switch (block)
            {
                case TextPageBlockRecord textBlock:
                    notebook.Execute(
                        "INSERT INTO _sqlnotebook_page_text_blocks VALUES (?, ?, ?);",
                        new object[] { itemId, index, textBlock.Content }
                    );
                    break;
                case QueryPageBlockRecord queryBlock:
                    var optionsJson = JsonSerializer.Serialize(queryBlock.Options);
                    notebook.Execute(
                        "INSERT INTO _sqlnotebook_page_query_blocks VALUES (?, ?, ?, ?, ?);",
                        new object[] { itemId, index, queryBlock.Sql, queryBlock.Output?.GetBytes(), optionsJson }
                    );
                    break;
                default:
                    Debug.Fail("Unknown block type");
                    break;
            }
            index++;
        }
    }

    private static void CreateTables(Notebook notebook)
    {
        notebook.Execute(
            @"CREATE TABLE IF NOT EXISTS _sqlnotebook_items (
    id INTEGER NOT NULL,
    name TEXT NOT NULL,
    type INTEGER NOT NULL,
    PRIMARY KEY (id)
);"
        );

        notebook.Execute(
            @"CREATE TABLE IF NOT EXISTS _sqlnotebook_scripts (
    item_id INTEGER NOT NULL,
    content TEXT NOT NULL,
    PRIMARY KEY (item_id),
    FOREIGN KEY (item_id) REFERENCES _sqlnotebook_items (id)
);"
        );

        notebook.Execute(
            @"CREATE TABLE IF NOT EXISTS _sqlnotebook_script_parameters (
    item_id INTEGER NOT NULL,
    parameter_index INTEGER NOT NULL,
    name TEXT NOT NULL,
    PRIMARY KEY (item_id, parameter_index),
    FOREIGN KEY (item_id) REFERENCES _sqlnotebook_scripts (item_id)
);"
        );

        notebook.Execute(
            @"CREATE TABLE IF NOT EXISTS _sqlnotebook_page_text_blocks (
    item_id INTEGER NOT NULL,
    block_index INTEGER NOT NULL,
    content TEXT NOT NULL,
    PRIMARY KEY (item_id, block_index),
    FOREIGN KEY (item_id) REFERENCES _sqlnotebook_items (id)
);"
        );

        notebook.Execute(
            @"CREATE TABLE IF NOT EXISTS _sqlnotebook_page_query_blocks (
    item_id INTEGER NOT NULL,
    block_index INTEGER NOT NULL,
    content TEXT NOT NULL,
    output BLOB NULL,
    options_json TEXT NOT NULL,
    PRIMARY KEY (item_id, block_index),
    FOREIGN KEY (item_id) REFERENCES _sqlnotebook_items (id)
);"
        );
    }

    public static void DropTables(Notebook notebook)
    {
        notebook.Execute("DROP TABLE IF EXISTS _sqlnotebook_page_query_blocks;");
        notebook.Execute("DROP TABLE IF EXISTS _sqlnotebook_page_text_blocks;");
        notebook.Execute("DROP TABLE IF EXISTS _sqlnotebook_script_parameters;");
        notebook.Execute("DROP TABLE IF EXISTS _sqlnotebook_scripts");
        ;
        notebook.Execute("DROP TABLE IF EXISTS _sqlnotebook_items;");
    }

    public void Dispose()
    {
        foreach (var item in Items)
        {
            if (item is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
