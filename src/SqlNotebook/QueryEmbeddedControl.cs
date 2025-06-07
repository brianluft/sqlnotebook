using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SqlNotebook.Properties;
using SqlNotebookScript;
using SqlNotebookScript.Core;
using SqlNotebookScript.DataTables;
using SqlNotebookScript.Interpreter;
using SqlNotebookScript.Utils;

namespace SqlNotebook;

public partial class QueryEmbeddedControl : UserControl
{
    public const int MAX_GRID_ROWS = 10_000;
    private readonly NotebookManager _manager;
    private readonly Ui _ui;
    private ScriptOutput _output;

    // This includes tables for printed messages and scalar results. The indices match the tabs.
    private readonly List<(DataTable DataTable, SimpleDataTable Sdt)> _dataTables = new();
    private TabControl _tabs;

    public SqlTextControl TextControl { get; private set; }
    public string SqlText
    {
        get => TextControl.SqlText;
        set => TextControl.SqlText = value;
    }
    public ScriptOutput Output
    {
        get => _output;
        set
        {
            if (!ReferenceEquals(_output, value))
            {
                _output?.Dispose();
                _output = value;
                SetOutput();
            }
        }
    }
    public bool ShowSql
    {
        get => _optionsShowSqlMenu.Checked;
        set => _optionsShowSqlMenu.Checked = value;
    }
    public bool ShowResults
    {
        get => _optionsShowResultsMenu.Checked;
        set => _optionsShowResultsMenu.Checked = value;
    }
    public int MaxRows
    {
        get
        {
            foreach (ToolStripMenuItem item in _limitRowsOnPageMenu.DropDownItems)
            {
                if (item.Checked)
                {
                    return int.Parse(item.Text);
                }
            }
            return 10;
        }
        set
        {
            foreach (ToolStripMenuItem item in _limitRowsOnPageMenu.DropDownItems)
            {
                item.Checked = int.Parse(item.Text) == value;
            }
        }
    }
    public event EventHandler SqlTextChanged;
    public event EventHandler Dirty;

    public QueryEmbeddedControl(NotebookManager manager, bool isPageContext, string initialText)
    {
        InitializeComponent();
        _manager = manager;
        _toolStrip.SetMenuAppearance();

        BackColor = Color.FromArgb(250, 250, 250);

        TextControl = new SqlTextControl(false)
        {
            Dock = DockStyle.Fill,
            Padding = Padding.Empty,
            SqlText = initialText,
        };
        TextControl.SqlTextChanged += (sender, e) =>
        {
            SqlTextChanged?.Invoke(sender, e);
            Dirty?.Invoke(this, EventArgs.Empty);
        };
        TextControl.F5KeyPress += TextControl_F5KeyPress;
        _sqlPanel.Controls.Add(TextControl);

        Ui ui = new(this, false);
        ui.Init(_split, 0.3);
        ui.Init(_executeButton, Resources.ControlPlayBlue, Resources.control_play_blue32);
        ui.MarginRight(_executeButton);
        ui.Init(_sendMenu);
        ui.MarginRight(_sendMenu);
        ui.Init(_optionsMenu);
        ui.MarginRight(_optionsMenu);
        _optionsMenu.Visible = isPageContext;
        ui.Init(_transactionMenu);
        ui.MarginRight(_transactionMenu);
        ui.Init(_sendTableMenu, Resources.table, Resources.table32);
        ui.Init(_hideResultsButton, Resources.hide_detail, Resources.hide_detail32);
        _hideResultsButton.Visible = false;
        ui.Init(_showResultsButton, Resources.show_detail, Resources.show_detail32);
        _showResultsButton.Visible = false;
        _ui = ui;

        Disposed += delegate
        {
            _output?.Dispose();
        };
    }

    private void TextControl_F5KeyPress(object sender, EventArgs e)
    {
        Execute();
    }

    private void ExecuteButton_Click(object sender, EventArgs e)
    {
        Execute();
        Dirty?.Invoke(this, EventArgs.Empty);
    }

    private void SetOutput()
    {
        // Remove the previous result controls, if any.
        for (var i = _split.Panel2.Controls.Count - 1; i >= 0; i--)
        {
            var control = _split.Panel2.Controls[i];
            _split.Panel2.Controls.RemoveAt(i);
            control.Dispose();
        }
        _dataTables.Clear();
        _tabs = null;

        // Now that the previous results are gone, we can bail early if there is no output.
        if (Output == null || (!Output.DataTables.Any() && Output.ScalarResult == null && !Output.TextOutput.Any()))
        {
            ShowHideResultsPane(show: false);
            return;
        }

        // Create a DataGridView for each DataTable
        var dataTables = ConvertOutputToDataTables();
        List<DataGridView> grids = new(dataTables.Count);
        foreach (var table in dataTables)
        {
            var grid = DataGridViewUtil.NewDataGridView(contextMenu: true);
            grids.Add(grid);
            grid.Dock = DockStyle.Fill;
            grid.DataSource = table.DataTable;
            grid.Margin = Padding.Empty;
        }

        // Show a tab control with one grid per tab.
        _tabs = new() { Dock = DockStyle.Fill };
        _ui.Init(_tabs);
        _split.Panel2.Controls.Add(_tabs);
        Debug.Assert(dataTables.Count == grids.Count);
        for (var i = 0; i < dataTables.Count; i++)
        {
            var (dt, sdt) = dataTables[i];
            _dataTables.Add(dataTables[i]);
            TabPage page = new(
                dt.Rows.Count == sdt.FullCount
                    ? $"Result ({sdt.FullCount:#,##0} row{(sdt.FullCount == 1 ? "" : "s")})"
                    : $"Result ({sdt.FullCount:#,##0} row{(sdt.FullCount == 1 ? "" : "s")}; {dt.Rows.Count:#,##0} shown)"
            );
            _tabs.TabPages.Add(page);
            _ui.Init(page);
            page.Controls.Add(grids[i]);
        }
        ShowHideResultsPane(show: true);

        // AutoSizeColumns doesn't work properly if we do it before the query control is shown, and that happens when
        // opening a saved output in a query page block.
        BeginInvoke(
            new Action(() =>
            {
                foreach (var grid in grids)
                {
                    grid.AutoSizeColumns(this.Scaled(500));
                }
            })
        );
    }

    private void ShowHideResultsPane(bool show)
    {
        if (_split.Panel2.Controls.Count == 0)
        {
            // There is nothing to show in the results panel.
            _split.Panel2Collapsed = true;
            _hideResultsButton.Visible = false;
            _showResultsButton.Visible = false;
        }
        else
        {
            _split.Panel2Collapsed = !show;
            _hideResultsButton.Visible = show;
            _showResultsButton.Visible = !show;
        }
    }

    private void Execute()
    {
        try
        {
            _manager.CommitOpenEditors();
            var sql = SqlText;
            var none = _transactionNoneMenu.Checked;
            var rollback = _transactionRollbackMenu.Checked;
            var output = WaitForm.GoWithCancel(
                TopLevelControl,
                "Script",
                "Executing script...",
                out var success,
                cancel =>
                {
                    try
                    {
                        using var status = WaitStatus.StartRows("Script output");
                        if (none)
                        {
                            return SqlUtil.WithCancellation(
                                _manager.Notebook,
                                () =>
                                {
                                    return _manager.ExecuteScript(sql, onRow: status.IncrementRows);
                                },
                                cancel
                            );
                        }
                        else
                        {
                            return SqlUtil.WithCancellableTransaction(
                                _manager.Notebook,
                                () =>
                                {
                                    return _manager.ExecuteScript(sql, onRow: status.IncrementRows);
                                },
                                rollback,
                                cancel
                            );
                        }
                    }
                    catch (UncaughtErrorScriptException uncaughtException) when (uncaughtException.Snippet != null)
                    {
                        throw new ExceptionEx(
                            uncaughtException.Message,
                            $"Error at:\r\n\"{uncaughtException.Snippet}\""
                        );
                    }
                }
            );
            _manager.SetDirty();
            _manager.Rescan();
            if (!success)
            {
                return;
            }
            ;
            Output = output;
        }
        catch (Exception ex)
        {
            Ui.ShowError(TopLevelControl, "Script Error", ex);
        }
    }

    private List<(DataTable DataTable, SimpleDataTable Sdt)> ConvertOutputToDataTables()
    {
        List<(DataTable DataTable, SimpleDataTable Sdt)> resultSets = new();
        if (Output.ScalarResult != null)
        {
            MemorySimpleDataTable sdt = new(
                new[] { "value" },
                new object[][] { new object[] { Output.ScalarResult } },
                1
            );
            var dt = sdt.ToDataTable();
            resultSets.Add((dt, sdt));
        }
        if (Output.TextOutput.Any())
        {
            MemorySimpleDataTable sdt = new(
                new[] { "message" },
                Output.TextOutput.Select(x => new object[] { x }).ToList(),
                Output.TextOutput.Count
            );
            var dt = sdt.ToDataTable();
            resultSets.Add((dt, sdt));
        }
        foreach (var sdt in Output.DataTables)
        {
            resultSets.Add((sdt.ToDataTable(MAX_GRID_ROWS), sdt));
        }
        return resultSets;
    }

    private void SendTableMenu_Click(object sender, EventArgs e)
    {
        if (_dataTables == null || !_dataTables.Any())
        {
            Ui.ShowError(
                TopLevelControl,
                "Send to Table",
                "There are no data table results to send.",
                "Make sure to execute the query by pressing F5 first."
            );
            return;
        }

        var (dt, sdt) = _dataTables[_tabs.SelectedIndex];

        using SendToTableForm f = new(
            "results",
            _manager
                .Items.Where(x => x.Type == NotebookItemType.Table || x.Type == NotebookItemType.View)
                .Select(x => x.Name)
                .ToList()
        );
        if (f.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }
        var name = f.SelectedName;

        var rerun = sdt.FullCount != sdt.Rows.Count;
        if (rerun)
        {
            var choice = Ui.ShowTaskDialog(
                this,
                "The query will be executed. Proceed?",
                "Send to Table",
                new[] { Ui.OK, Ui.CANCEL }
            );
            if (choice != Ui.OK)
            {
                return;
            }
        }

        var columnDefs = dt
            .Columns.Cast<DataColumn>()
            .Select(x => $"{x.ColumnName.DoubleQuote()} {GetSqlNameForDbType(x.DataType)}");
        var createSql = $"CREATE TABLE {name.DoubleQuote()} ({string.Join(", ", columnDefs)})";

        var insertSql = SqlUtil.GetInsertSql(name, dt.Columns.Count);

        var sql = SqlText;
        var tabIndex = _tabs.SelectedIndex;
        WaitForm.GoWithCancel(
            TopLevelControl,
            "Send",
            "Sending results to table...",
            out var success,
            cancel =>
            {
                SqlUtil.WithCancellableTransaction(
                    _manager.Notebook,
                    () =>
                    {
                        _manager.ExecuteScriptNoOutput(createSql);
                        using var insertStmt = _manager.Notebook.Prepare(insertSql);
                        if (!rerun)
                        {
                            using var status = WaitStatus.StartRows(name);
                            foreach (var row in sdt.Rows)
                            {
                                cancel.ThrowIfCancellationRequested();
                                insertStmt.ExecuteNoOutput(row, cancel);
                                status.IncrementRows();
                            }
                            return;
                        }

                        ScriptOutput output;
                        using (var status = WaitStatus.StartRows("Script output"))
                        {
                            output = _manager.ExecuteScript(sql, onRow: status.IncrementRows);
                        }

                        using (var status = WaitStatus.StartRows(name))
                        {
                            var scalarResultTabIndex = output.ScalarResult != null ? 0 : -1;
                            var textTabIndex =
                                output.ScalarResult == null && output.TextOutput.Any() ? 0
                                : output.ScalarResult != null && output.TextOutput.Any() ? 1
                                : -1;
                            var tablesTabOffset =
                                (output.ScalarResult != null ? 1 : 0) + (output.TextOutput.Any() ? 1 : 0);

                            if (tabIndex == scalarResultTabIndex)
                            {
                                insertStmt.ExecuteNoOutput(new object[] { output.ScalarResult }, cancel);
                            }
                            else if (tabIndex == textTabIndex)
                            {
                                foreach (var line in output.TextOutput)
                                {
                                    cancel.ThrowIfCancellationRequested();
                                    insertStmt.ExecuteNoOutput(new object[] { line }, cancel);
                                }
                            }
                            else
                            {
                                var table = output.DataTables[tabIndex - tablesTabOffset];
                                foreach (var row in table.Rows)
                                {
                                    cancel.ThrowIfCancellationRequested();
                                    insertStmt.ExecuteNoOutput(row, cancel);
                                    status.IncrementRows();
                                }
                            }
                        }
                    },
                    cancel
                );
            }
        );
        _manager.SetDirty();
        _manager.Rescan();
    }

    private static string GetSqlNameForDbType(Type type)
    {
        if (type == typeof(string))
        {
            return "TEXT";
        }
        else if (type == typeof(int) || type == typeof(long))
        {
            return "INTEGER";
        }
        else if (type == typeof(double))
        {
            return "FLOAT";
        }
        else if (type == typeof(byte[]))
        {
            return "BLOB";
        }
        else
        {
            return "ANY";
        }
    }

    private void HideResultsButton_Click(object sender, EventArgs e)
    {
        ShowHideResultsPane(false);
    }

    private void ShowResultsButton_Click(object sender, EventArgs e)
    {
        ShowHideResultsPane(true);
    }

    private void LimitsRowsMenu_Click(object sender, EventArgs e)
    {
        foreach (ToolStripMenuItem item in _limitRowsOnPageMenu.DropDownItems)
        {
            item.Checked = ReferenceEquals(sender, item);
        }
        Dirty?.Invoke(this, EventArgs.Empty);
    }

    private void AppearanceMenu_Click(object sender, EventArgs e)
    {
        Dirty?.Invoke(this, EventArgs.Empty);
    }

    private void TransactionCommitMenu_Click(object sender, EventArgs e)
    {
        _transactionCommitMenu.Checked = true;
        _transactionRollbackMenu.Checked = false;
        _transactionNoneMenu.Checked = false;
        _transactionMenu.Text = "Transaction: Commit";
    }

    private void TransactionRollbackMenu_Click(object sender, EventArgs e)
    {
        _transactionCommitMenu.Checked = false;
        _transactionRollbackMenu.Checked = true;
        _transactionNoneMenu.Checked = false;
        _transactionMenu.Text = "Transaction: Rollback";
    }

    private void TransactionNoneMenu_Click(object sender, EventArgs e)
    {
        _transactionCommitMenu.Checked = false;
        _transactionRollbackMenu.Checked = false;
        _transactionNoneMenu.Checked = true;
        _transactionMenu.Text = "Transaction: None";
    }
}
