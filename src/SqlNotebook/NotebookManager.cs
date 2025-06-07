using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using SqlNotebookScript;
using SqlNotebookScript.Core;
using SqlNotebookScript.Interpreter;
using SqlNotebookScript.Interpreter.Ast;
using SqlNotebookScript.Utils;

namespace SqlNotebook;

public enum NotebookItemType
{
    Script,
    Table,
    View,
    Page,
}

public struct NotebookItem
{
    public NotebookItemType Type;
    public string Name;
    public bool IsVirtualTable;

    public NotebookItem(NotebookItemType type, string name, bool isVirtualTable = false)
    {
        Type = type;
        Name = name;
        IsVirtualTable = isVirtualTable;
    }
}

public sealed class NotebookChangeEventArgs : EventArgs
{
    public IReadOnlyList<NotebookItem> AddedItems { get; }
    public IReadOnlyList<NotebookItem> RemovedItems { get; }

    public NotebookChangeEventArgs(IReadOnlyList<NotebookItem> addedItems, IReadOnlyList<NotebookItem> removedItems)
    {
        AddedItems = addedItems;
        RemovedItems = removedItems;
    }
}

public sealed class NotebookItemRequestEventArgs : EventArgs
{
    public NotebookItem Item { get; }

    public NotebookItemRequestEventArgs(NotebookItem item)
    {
        Item = item;
    }
}

public sealed class NotebookItemRenameEventArgs : EventArgs
{
    public NotebookItem Item { get; }
    public string NewName { get; }

    public NotebookItemRenameEventArgs(NotebookItem item, string newName)
    {
        Item = item;
        NewName = newName;
    }
}

public sealed class HotkeyEventArgs : EventArgs
{
    public Keys KeyData { get; }

    public HotkeyEventArgs(Keys keyData)
    {
        KeyData = keyData;
    }
}

public sealed class NotebookManager
{
    public Notebook Notebook { get; }
    public IReadOnlyList<NotebookItem> Items { get; private set; } = new NotebookItem[0];
    public event EventHandler<NotebookChangeEventArgs> NotebookChange; // informs the ExplorerControl about changes
    public event EventHandler<NotebookItemRequestEventArgs> NotebookItemOpenRequest;
    public event EventHandler<NotebookItemRequestEventArgs> NotebookItemCloseRequest;
    public event EventHandler NotebookItemsSaveRequest;
    public event EventHandler NotebookDirty;
    public event EventHandler<NotebookItemRenameEventArgs> NotebookItemRename;
    public event EventHandler<HotkeyEventArgs> HandleHotkeyRequest;
    private readonly Slot<bool> _isTransactionOpen;

    public NotebookManager(Notebook notebook, Slot<bool> isTransactionOpen)
    {
        Notebook = notebook;
        _isTransactionOpen = isTransactionOpen;
    }

    public void CommitOpenEditors()
    {
        NotebookItemsSaveRequest?.Invoke(this, EventArgs.Empty);
    }

    public void HandleAppHotkeys(Keys keyData)
    {
        HandleHotkeyRequest?.Invoke(this, new HotkeyEventArgs(keyData));
    }

    public void Save(CancellationToken cancel)
    {
        NotebookItemsSaveRequest?.Invoke(this, EventArgs.Empty);
        using var status = WaitStatus.StartCustom(Path.GetFileName(Notebook.OriginalFilePath));
        Notebook.Save(c => status.SetProgress($"{c}% complete"), cancel);
    }

    public void SaveAs(string filePath, CancellationToken cancel)
    {
        NotebookItemsSaveRequest?.Invoke(this, EventArgs.Empty);
        using var status = WaitStatus.StartCustom(Path.GetFileName(filePath));
        Notebook.SaveAs(filePath, c => status.SetProgress($"{c}% complete"), cancel);
    }

    public void Rescan(bool notebookItemsOnly = false)
    {
        var ignoredItems = notebookItemsOnly
            ? Items.Where(x => x.Type == NotebookItemType.Table || x.Type == NotebookItemType.View).ToList()
            : new List<NotebookItem>();
        var newItems = ReadItems(notebookItemsOnly);
        var addedItems = newItems.Except(Items).ToList();
        var removedItems = Items.Except(newItems).Except(ignoredItems).ToList();
        Items = newItems.Concat(ignoredItems).ToList();
        NotebookChange?.Invoke(this, new NotebookChangeEventArgs(addedItems, removedItems));

        if (!notebookItemsOnly)
        {
            // also check the transaction status
            _isTransactionOpen.Value = Notebook.IsTransactionActive();
        }
    }

    public void OpenItem(NotebookItem item)
    {
        NotebookItemOpenRequest?.Invoke(this, new NotebookItemRequestEventArgs(item));
    }

    public void CloseItem(NotebookItem item)
    {
        NotebookItemCloseRequest?.Invoke(this, new NotebookItemRequestEventArgs(item));
    }

    public void SetDirty()
    {
        NotebookDirty?.Invoke(this, EventArgs.Empty);
    }

    private IReadOnlyList<NotebookItem> ReadItems(bool notebookItemsOnly)
    {
        var items = new List<NotebookItem>();

        if (!notebookItemsOnly)
        {
            // tables and views
            using var dt = Notebook.Query(
                "SELECT name, type, sql FROM sqlite_master WHERE type = 'table' OR type = 'view' ",
                new Dictionary<string, object>()
            );
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var type = (string)dt.Get(i, "type") == "view" ? NotebookItemType.View : NotebookItemType.Table;
                var isVirtualTable = dt.Get(i, "sql").ToString().StartsWith("CREATE VIRTUAL TABLE");
                var name = (string)dt.Get(i, "name");
                if (name.StartsWith("_sqlnotebook_"))
                {
                    continue;
                }
                items.Add(new NotebookItem(type, name, isVirtualTable));
            }
        }

        items.AddRange(
            from x in Notebook.UserData.Items
            let type = x switch
            {
                PageNotebookItemRecord => NotebookItemType.Page,
                ScriptNotebookItemRecord => NotebookItemType.Script,
                _ => throw new NotImplementedException(),
            }
            select new NotebookItem(type, x.Name)
        );

        return items;
    }

    public string NewScript() => NewItem(NotebookItemType.Script);

    public string NewPage() => NewItem(NotebookItemType.Page);

    public string NewItem(NotebookItemType type, string name = null, string data = null)
    {
        var existingNames = Notebook.UserData.Items.Select(x => x.Name.ToLowerInvariant()).ToHashSet();
        if (name == null)
        {
            int i;
            for (i = 1; existingNames.Contains($"{type.ToString().ToLowerInvariant()}{i}"); i++) { }
            name = $"{type}{i}";
        }
        else if (existingNames.Contains(name.ToLowerInvariant()))
        {
            throw new Exception($"There is already a notebook item named \"{name}\".");
        }
        switch (type)
        {
            case NotebookItemType.Page:
                Notebook.UserData.Items.Add(new PageNotebookItemRecord { Name = name, Blocks = new() });
                break;

            case NotebookItemType.Script:
                Notebook.UserData.Items.Add(
                    new ScriptNotebookItemRecord
                    {
                        Name = name,
                        Sql = data ?? "",
                        Parameters = new(),
                    }
                );
                break;

            default:
                throw new NotImplementedException();
        }
        Rescan(notebookItemsOnly: true);
        SetDirty();
        return name;
    }

    public void SetItemData(string name, NotebookItemRecord record)
    {
        record.Name = name;
        var ud = Notebook.UserData;
        var oldItemRecord = ud.Items.FirstOrDefault(x => x.Name == name);
        if (oldItemRecord != null)
        {
            ud.Items.Remove(oldItemRecord);
            if (oldItemRecord is IDisposable disposable)
                disposable.Dispose();
        }
        ud.Items.Add(record);

        // if this is a Script, then we also need to update its list of parameters
        if (record is ScriptNotebookItemRecord script)
        {
            if (script.Parameters == null)
            {
                script.Parameters = new();
            }
            else
            {
                script.Parameters.Clear();
            }
            try
            {
                var parser = new ScriptParser(Notebook);
                var ast = parser.Parse(script.Sql);
                var paramNames = ast.Traverse()
                    .OfType<DeclareStmt>()
                    .Where(x => x.IsParameter)
                    .Select(x => x.VariableName);
                foreach (var paramName in paramNames)
                {
                    script.Parameters.Add(paramName);
                }
            }
            catch (Exception) { }
        }
    }

    public NotebookItemRecord GetItemData(string name) => Notebook.UserData.Items.FirstOrDefault(x => x.Name == name);

    public void ExecuteScriptNoOutput(
        string code,
        IReadOnlyDictionary<string, object> args = null,
        TransactionType transactionType = TransactionType.NoTransaction
    )
    {
        using var output = ExecuteScriptEx(code, args, transactionType, out _, null);
    }

    public ScriptOutput ExecuteScript(
        string code,
        IReadOnlyDictionary<string, object> args = null,
        TransactionType transactionType = TransactionType.NoTransaction
    )
    {
        return ExecuteScriptEx(code, args, transactionType, out _, null);
    }

    public ScriptOutput ExecuteScript(
        string code,
        Action onRow,
        IReadOnlyDictionary<string, object> args = null,
        TransactionType transactionType = TransactionType.NoTransaction
    ) => ExecuteScriptEx(code, args, transactionType, out _, onRow);

    public enum TransactionType
    {
        Transaction,
        RollbackTransaction,
        NoTransaction,
    }

    private ScriptOutput ExecuteScriptEx(
        string code,
        IReadOnlyDictionary<string, object> args,
        TransactionType transactionType,
        out Dictionary<string, object> vars,
        Action onRow
    )
    {
        var env = new ScriptEnv { OnRow = onRow };
        ScriptOutput output;
        var parser = new ScriptParser(Notebook);
        var script = parser.Parse(code);
        var runner = new ScriptRunner(Notebook, Notebook.GetScripts());
        if (transactionType == TransactionType.Transaction)
        {
            output = SqlUtil.WithTransaction(
                Notebook,
                () => runner.Execute(script, env, args ?? new Dictionary<string, object>())
            );
        }
        else if (transactionType == TransactionType.RollbackTransaction)
        {
            output = SqlUtil.WithRollbackTransaction(
                Notebook,
                () => runner.Execute(script, env, args ?? new Dictionary<string, object>())
            );
        }
        else
        {
            output = runner.Execute(script, env, args ?? new Dictionary<string, object>());
        }
        vars = env.Vars;
        return output;
    }

    public void DeleteItem(NotebookItem item)
    {
        switch (item.Type)
        {
            case NotebookItemType.Table:
                Notebook.Execute($"DROP TABLE {item.Name.DoubleQuote()}");
                break;

            case NotebookItemType.View:
                Notebook.Execute($"DROP VIEW {item.Name.DoubleQuote()}");
                break;

            case NotebookItemType.Script:
            case NotebookItemType.Page:
            {
                var itemRecord = Notebook.UserData.Items.FirstOrDefault(x => x.Name == item.Name);
                if (itemRecord != null)
                {
                    Notebook.UserData.Items.Remove(itemRecord);
                    if (itemRecord is IDisposable disposable)
                        disposable.Dispose();
                }
                break;
            }
        }
        SetDirty();
    }

    public void RenameItem(NotebookItem item, string newName)
    {
        var lcNewName = newName.ToLower();
        var isCaseChange = item.Name.ToLower() == lcNewName;

        switch (item.Type)
        {
            case NotebookItemType.Page:
            case NotebookItemType.Script:
                if (!isCaseChange)
                {
                    // is the new name already in use?
                    if (Notebook.UserData.Items.Any(x => x.Name.ToLower() == lcNewName))
                    {
                        throw new Exception($"An item named \"{newName}\" already exists.");
                    }
                }

                var itemRec = Notebook.UserData.Items.Single(x => x.Name == item.Name);
                itemRec.Name = newName;
                break;

            case NotebookItemType.Table:
                // sqlite won't let us rename a table with just a case change, so in that situation we'll rename
                // to something else and then rename back
                if (isCaseChange)
                {
                    var tempName = $"temp_{Guid.NewGuid()}";
                    Notebook.Execute($"ALTER TABLE {item.Name.DoubleQuote()} RENAME TO {tempName.DoubleQuote()};");
                    Notebook.Execute($"ALTER TABLE {tempName.DoubleQuote()} RENAME TO {newName.DoubleQuote()};");
                }
                else
                {
                    Notebook.Execute($"ALTER TABLE {item.Name.DoubleQuote()} RENAME TO {newName.DoubleQuote()}");
                }
                break;

            case NotebookItemType.View:
                var createViewSql = (string)
                    Notebook.QueryValue(
                        "SELECT sql FROM sqlite_master WHERE name = @old_name",
                        new Dictionary<string, object> { ["@old_name"] = item.Name }
                    );
                var tokens = Notebook.Tokenize(createViewSql);
                if (tokens.Count < 3 || tokens[0].Type != TokenType.Create || tokens[1].Type != TokenType.View)
                {
                    throw new Exception($"Unable to parse the original CREATE VIEW statement for \"{item.Name}\".");
                }
                var suffix = string.Join(" ", tokens.Select(x => x.Text).Skip(3)); // everything after "CREATE VIEW viewname"
                Notebook.Execute($"DROP VIEW {item.Name.DoubleQuote()}");
                Notebook.Execute($"CREATE VIEW {newName.DoubleQuote()} {suffix}");
                break;
        }

        NotebookItemRename?.Invoke(this, new NotebookItemRenameEventArgs(item, newName));
        SetDirty();
    }
}
