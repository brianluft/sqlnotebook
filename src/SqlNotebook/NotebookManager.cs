﻿// SQL Notebook
// Copyright (C) 2016 Brian Luft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
// OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SqlNotebookCore;
using SqlNotebookScript;
using SqlNotebookScript.Interpreter;
using SqlNotebookScript.Interpreter.Ast;
using SqlNotebookScript.Utils;

namespace SqlNotebook {
    public enum NotebookItemType {
        Note,
        Script,
        Table,
        View
    }

    public struct NotebookItem {
        public NotebookItemType Type;
        public string Name;
        public bool IsVirtualTable;
        public NotebookItem(NotebookItemType type, string name, bool isVirtualTable = false) {
            Type = type;
            Name = name;
            IsVirtualTable = isVirtualTable;
        }
    }

    public sealed class NotebookChangeEventArgs : EventArgs {
        public IReadOnlyList<NotebookItem> AddedItems { get; }
        public IReadOnlyList<NotebookItem> RemovedItems { get; }
        public NotebookChangeEventArgs(IReadOnlyList<NotebookItem> addedItems, IReadOnlyList<NotebookItem> removedItems) {
            AddedItems = addedItems;
            RemovedItems = removedItems;
        }
    }

    public sealed class NotebookItemRequestEventArgs : EventArgs {
        public NotebookItem Item { get; }
        public NotebookItemRequestEventArgs(NotebookItem item) {
            Item = item;
        }
    }

    public sealed class NotebookItemRenameEventArgs : EventArgs {
        public NotebookItem Item { get; }
        public string NewName { get; }
        public NotebookItemRenameEventArgs(NotebookItem item, string newName) {
            Item = item;
            NewName = newName;
        }
    }

    public sealed class StatusUpdateEventArgs : EventArgs {
        public string Status { get; }
        public StatusUpdateEventArgs(string status) {
            Status = status;
        }
    }

    public sealed class HotkeyEventArgs : EventArgs {
        public Keys KeyData { get; }
        public HotkeyEventArgs(Keys keyData) {
            KeyData = keyData;
        }
    }

    public sealed class NotebookManager {
        private readonly Stack<string> _statusStack = new Stack<string>();
        public Notebook Notebook { get; }
        public IReadOnlyList<NotebookItem> Items { get; private set; } = new NotebookItem[0];
        public event EventHandler<NotebookChangeEventArgs> NotebookChange; // informs the ExplorerControl about changes
        public event EventHandler<NotebookItemRequestEventArgs> NotebookItemOpenRequest;
        public event EventHandler<NotebookItemRequestEventArgs> NotebookItemCloseRequest;
        public event EventHandler NotebookItemsSaveRequest;
        public event EventHandler NotebookDirty;
        public event EventHandler<NotebookItemRenameEventArgs> NotebookItemRename;
        public event EventHandler<StatusUpdateEventArgs> StatusUpdate;
        public event EventHandler<HotkeyEventArgs> HandleHotkeyRequest;
        private readonly Slot<bool> _isTransactionOpen;
        public object HelpServerLock { get; } = new object();
        public HelpServer HelpServer { get; private set; }

        public NotebookManager(Notebook notebook, Slot<bool> isTransactionOpen) {
            Notebook = notebook;
            _isTransactionOpen = isTransactionOpen;

            Task.Run(() => {
                lock (HelpServerLock) {
                    HelpServer = new HelpServer();
                }
            });
        }

        public void CommitOpenEditors() {
            NotebookItemsSaveRequest?.Invoke(this, EventArgs.Empty);
        }

        public void HandleAppHotkeys(Keys keyData) {
            HandleHotkeyRequest?.Invoke(this, new HotkeyEventArgs(keyData));
        }

        public void Save() {
            NotebookItemsSaveRequest?.Invoke(this, EventArgs.Empty);
            Notebook.Invoke(() => {
                Notebook.Execute("VACUUM");
                Notebook.Save();
            });
        }

        public void SaveAs(string filePath) {
            NotebookItemsSaveRequest?.Invoke(this, EventArgs.Empty);
            Notebook.Invoke(() => {
                Notebook.Execute("VACUUM");
                Notebook.SaveAs(filePath);
            });
        }

        public void Rescan(bool notebookItemsOnly = false) {
            var ignoredItems = notebookItemsOnly
                ? Items.Where(x => x.Type == NotebookItemType.Table || x.Type == NotebookItemType.View).ToList()
                : new List<NotebookItem>();
            var newItems = ReadItems(notebookItemsOnly);
            var addedItems = newItems.Except(Items).ToList();
            var removedItems = Items.Except(newItems).Except(ignoredItems).ToList();
            Items = newItems.Concat(ignoredItems).ToList();
            NotebookChange?.Invoke(this, new NotebookChangeEventArgs(addedItems, removedItems));

            if (!notebookItemsOnly) {
                // also check the transaction status
                Notebook.Invoke(() => {
                    _isTransactionOpen.Value = Notebook.IsTransactionActive();
                });
            }
        }

        public void OpenItem(NotebookItem item) {
            NotebookItemOpenRequest?.Invoke(this, new NotebookItemRequestEventArgs(item));
        }

        public void CloseItem(NotebookItem item) {
            NotebookItemCloseRequest?.Invoke(this, new NotebookItemRequestEventArgs(item));
        }

        public void SetDirty() {
            NotebookDirty?.Invoke(this, EventArgs.Empty);
        }

        private IReadOnlyList<NotebookItem> ReadItems(bool notebookItemsOnly) {
            var items = new List<NotebookItem>();

            if (!notebookItemsOnly) {
                // tables and views
                var dt = Notebook.SpecialReadOnlyQuery(
                    "SELECT name, type, sql FROM sqlite_master WHERE type = 'table' OR type = 'view' ",
                    new Dictionary<string, object>());
                for (int i = 0; i < dt.Rows.Count; i++) {
                    var type = (string)dt.Get(i, "type") == "view" ? NotebookItemType.View : NotebookItemType.Table;
                    var isVirtualTable = dt.Get(i, "sql").ToString().StartsWith("CREATE VIRTUAL TABLE");
                    items.Add(new NotebookItem(type, (string)dt.Get(i, "name"), isVirtualTable));
                }
            }

            // notes, consoles, and scripts
            items.AddRange(
                from x in Notebook.UserData.Items
                select new NotebookItem((NotebookItemType)Enum.Parse(typeof(NotebookItemType), x.Type), x.Name)
            );

            return items;
        }

        public string NewScript() {
            return NewItem(NotebookItemType.Script.ToString());
        }

        public string NewNote(string name = null, string data = null) {
            return NewItem(NotebookItemType.Note.ToString(), name,
                data ?? "<P><FONT face=Calibri></FONT>&nbsp;</P>");
        }

        private string NewItem(string type, string name = null, string data = null) {
            if (name == null) {
                var existingNames = new HashSet<string>(Notebook.UserData.Items.Select(x => x.Name.ToLower()));
                int i;
                for (i = 1; existingNames.Contains($"{type.ToLower()}{i}"); i++) { }
                name = $"{type}{i}";
            }
            var itemRec = new NotebookItemRecord { Name = name, Data = data ?? "", Type = type };
            Notebook.UserData.Items.Add(itemRec);
            Rescan(notebookItemsOnly: true);
            SetDirty();
            return name;
        }

        public void SetItemData(string name, string data) {
            var ud = Notebook.UserData;
            var itemRec = ud.Items.FirstOrDefault(x => x.Name == name);
            if (itemRec == null) {
                throw new ArgumentException($"There is no item named \"{name}\".");
            }

            itemRec.Data = data;

            // if this is a Script, then we also need to update its list of parameters
            if (itemRec.Type == "Script") {
                ud.ScriptParameters.RemoveWhere(x => x.ScriptName == name);
                var paramRec = new ScriptParameterRecord { ScriptName = name };
                try {
                    var parser = new ScriptParser(Notebook);
                    var ast = parser.Parse(data);
                    var paramNames =
                        ast.Traverse()
                        .OfType<DeclareStmt>()
                        .Where(x => x.IsParameter)
                        .Select(x => x.VariableName);
                    foreach (var paramName in paramNames) {
                        paramRec.ParamNames.Add(paramName);
                    }
                } catch (Exception) { }
                ud.ScriptParameters.Add(paramRec);
            }
        }

        public string GetItemData(string name) {
            var itemRec = Notebook.UserData.Items.FirstOrDefault(x => x.Name == name);
            if (itemRec == null) {
                return "";
            } else {
                return itemRec.Data;
            }
        }

        public ScriptOutput ExecuteScript(string code, IReadOnlyDictionary<string, object> args = null,
        TransactionType transactionType = TransactionType.NoTransaction) =>
            ExecuteScriptEx(code, args, transactionType, out _);

        public enum TransactionType {
            Transaction,
            RollbackTransaction,
            NoTransaction
        }

        public ScriptOutput ExecuteScriptEx(string code, IReadOnlyDictionary<string, object> args,
        TransactionType transactionType, out Dictionary<string, object> vars) {
            var env = new ScriptEnv();
            var output = Invoke(() => {
                var parser = new ScriptParser(Notebook);
                var script = parser.Parse(code);
                var runner = new ScriptRunner(Notebook, Notebook.GetScripts());
                if (transactionType == TransactionType.Transaction) {
                    return SqlUtil.WithTransaction(Notebook, 
                        () => runner.Execute(script, env, args ?? new Dictionary<string, object>()));
                } else if (transactionType == TransactionType.RollbackTransaction) {
                    return SqlUtil.WithRollbackTransaction(Notebook, 
                        () => runner.Execute(script, env, args ?? new Dictionary<string, object>()));
                } else {
                    return runner.Execute(script, env, args ?? new Dictionary<string, object>());
                }
            });
            vars = env.Vars;
            return output;
        }

        public void DeleteItem(NotebookItem item) {
            switch (item.Type) {
                case NotebookItemType.Script:
                case NotebookItemType.Note:
                    Notebook.UserData.Items.RemoveWhere(x => x.Name == item.Name);
                    break;

                case NotebookItemType.Table:
                    Notebook.Invoke(() => Notebook.Execute($"DROP TABLE {item.Name.DoubleQuote()}"));
                    break;

                case NotebookItemType.View:
                    Notebook.Invoke(() => Notebook.Execute($"DROP VIEW {item.Name.DoubleQuote()}"));
                    break;
            }
            SetDirty();
        }

        public void RenameItem(NotebookItem item, string newName) {
            var lcNewName = newName.ToLower();
            var isCaseChange = item.Name.ToLower() == lcNewName;

            switch (item.Type) {
                case NotebookItemType.Note:
                case NotebookItemType.Script:
                    if (!isCaseChange) {
                        // is the new name already in use?
                        if (Notebook.UserData.Items.Any(x => x.Name.ToLower() == lcNewName)) {
                            throw new Exception($"An item named \"{newName}\" already exists.");
                        }
                    }

                    var itemRec = Notebook.UserData.Items.Single(x => x.Name == item.Name);
                    itemRec.Name = newName;

                    if (item.Type == NotebookItemType.Script) {
                        var scriptParamRec = Notebook.UserData.ScriptParameters.SingleOrDefault(x => x.ScriptName == item.Name);
                        if (scriptParamRec != null) {
                            scriptParamRec.ScriptName = newName;
                        }
                    }
                    break;

                case NotebookItemType.Table:
                    // sqlite won't let us rename a table with just a case change, so in that situation we'll rename
                    // to something else and then rename back
                    if (isCaseChange) {
                        var tempName = $"temp_{Guid.NewGuid()}";
                        Notebook.Invoke(() => {
                            Notebook.Execute($"ALTER TABLE {item.Name.DoubleQuote()} RENAME TO {tempName.DoubleQuote()};");
                            Notebook.Execute($"ALTER TABLE {tempName.DoubleQuote()} RENAME TO {newName.DoubleQuote()};");
                        });
                    } else {
                        Notebook.Invoke(() => {
                            Notebook.Execute($"ALTER TABLE {item.Name.DoubleQuote()} RENAME TO {newName.DoubleQuote()}");
                        });
                    }
                    break;

                case NotebookItemType.View:
                    Notebook.Invoke(() => {
                        var createViewSql = (string)Notebook.QueryValue("SELECT sql FROM sqlite_master WHERE name = @old_name",
                            new Dictionary<string, object> { ["@old_name"] = item.Name });
                        var tokens = Notebook.Tokenize(createViewSql);
                        if (tokens.Count < 3 || tokens[0].Type != TokenType.Create || tokens[1].Type != TokenType.View) {
                            throw new Exception($"Unable to parse the original CREATE VIEW statement for \"{item.Name}\".");
                        }
                        var suffix = string.Join(" ", tokens.Select(x => x.Text).Skip(3)); // everything after "CREATE VIEW viewname"
                        Notebook.Execute($"DROP VIEW {item.Name.DoubleQuote()}");
                        Notebook.Execute($"CREATE VIEW {newName.DoubleQuote()} {suffix}");
                    });
                    break;
            }

            NotebookItemRename?.Invoke(this, new NotebookItemRenameEventArgs(item, newName));
            SetDirty();
        }

        public void PushStatus(string status) {
            _statusStack.Push(status);
            StatusUpdate?.Invoke(this, new StatusUpdateEventArgs(status));
        }

        public void PopStatus() {
            _statusStack.Pop();
            StatusUpdate?.Invoke(this, new StatusUpdateEventArgs(_statusStack.Any() ? _statusStack.Peek() : ""));
        }

        private T Invoke<T>(Func<T> func) {
            T value = default(T);
            Notebook.Invoke(() => {
                value = func();
            });
            return value;
        }
    }
}
