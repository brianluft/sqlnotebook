using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SqlNotebook.Properties;
using SqlNotebookScript.Utils;

namespace SqlNotebook.Import.Database;

public partial class DatabaseImportTablesForm : ZForm
{
    private readonly NotebookManager _manager;
    private readonly IImportSession _session;
    private readonly DataTable _dataTable;
    private readonly DataGridView _srcGrid;
    private readonly DataGridView _dstGrid;
    private readonly DataView _srcView;

    private static DataTable GetTables(IImportSession session)
    {
        DataTable dt = new();
        dt.Columns.Add("source_table", typeof(SourceTable));
        dt.Columns.Add("display_name", typeof(string));
        dt.Columns.Add("to_be_imported", typeof(bool));
        dt.BeginLoadData();
        foreach (var (schema, table) in session.TableNames)
        {
            var sourceTable = SourceTable.FromTable(schema, table);
            dt.LoadDataRow(new object[] { sourceTable, sourceTable.DisplayText, false }, true);
        }
        dt.EndLoadData();
        return dt;
    }

    public DatabaseImportTablesForm(NotebookManager manager, IImportSession session)
    {
        InitializeComponent();
        _manager = manager;
        _session = session;

        _dataTable = GetTables(session);

        // Source grid
        _srcGrid = DataGridViewUtil.NewDataGridView(
            autoGenerateColumns: false,
            allowSort: false,
            userColors: false,
            columnHeadersVisible: false
        );
        _srcGrid.Dock = DockStyle.Fill;
        _srcGrid.CellBorderStyle = DataGridViewCellBorderStyle.None;
        _srcGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _srcPanel.Controls.Add(_srcGrid);
        DataGridViewTextBoxColumn srcNameColumn = new()
        {
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            DataPropertyName = "display_name",
        };
        _srcGrid.Columns.Add(srcNameColumn);
        _srcView = _dataTable.AsDataView();
        _srcView.RowFilter = "to_be_imported = 0";
        _srcView.Sort = "display_name";
        _srcGrid.DataSource = _srcView;
        _srcGrid.SelectionChanged += SrcGrid_SelectionChanged;

        // Destination grid
        _dstGrid = DataGridViewUtil.NewDataGridView(
            autoGenerateColumns: false,
            allowSort: false,
            userColors: false,
            columnHeadersVisible: false
        );
        _dstGrid.Dock = DockStyle.Fill;
        _dstGrid.CellBorderStyle = DataGridViewCellBorderStyle.None;
        _dstGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _dstPanel.Controls.Add(_dstGrid);
        DataGridViewTextBoxColumn dstColumn = new()
        {
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            DataPropertyName = "display_name",
        };
        _dstGrid.Columns.Add(dstColumn);
        var dstView = _dataTable.AsDataView();
        dstView.RowFilter = "to_be_imported = 1";
        dstView.Sort = "display_name";
        _dstGrid.DataSource = dstView;
        _dstGrid.SelectionChanged += DstGrid_SelectionChanged;

        EnableDisableButtons();

        Ui ui = new(this, 140, 35);
        ui.Init(_selectionTable);
        ui.InitHeader(_srcLabel);
        ui.Init(_srcToolStrip);
        ui.Init(
            _addQueryButton,
            Ui.SuperimposePlusSymbol(Resources.table),
            Ui.SuperimposePlusSymbol(Resources.table32)
        );
        ui.MarginRight(_addQueryButton);
        ui.Init(_srcFilterText);
        _srcFilterText.TextBox.SetCueText("Search");
        ui.Init(_srcFilterClearButton, Resources.filter_clear, Resources.filter_clear32);
        ui.Init(_srcPanel);

        ui.InitHeader(_middleLabel);
        ui.Init(_selectionButtonsFlow);
        ui.Pad(_selectionButtonsFlow);
        ui.Init(_addButton);
        ui.Init(_removeButton);
        _addButton.AutoSize = false;
        _addButton.Size = _removeButton.Size;

        ui.InitHeader(_dstLabel);
        ui.Init(_dstToolStrip);
        ui.Init(_editTableButton, Resources.table_edit, Resources.table_edit32);
        ui.Init(_dstPanel);

        ui.InitHeader(_methodLabel);
        ui.MarginTop(_methodLabel);
        ui.Init(_methodFlow);
        ui.Pad(_methodFlow);
        ui.Init(_methodCopyRad);
        ui.Init(_methodLinkRad);

        ui.Init(_buttonFlow2);
        ui.MarginTop(_buttonFlow2);
        ui.Init(_viewSqlButton);
        ui.Init(_buttonFlow);
        ui.MarginTop(_buttonFlow);
        ui.Init(_okBtn);
        ui.Init(_cancelBtn);
    }

    private void DstGrid_SelectionChanged(object sender, EventArgs e)
    {
        _editTableButton.Enabled = _dstGrid.SelectedRows.Count == 1;
        ;
        _removeButton.Enabled = _dstGrid.SelectedRows.Count > 0;
    }

    private void SrcGrid_SelectionChanged(object sender, EventArgs e)
    {
        var any = _srcGrid.SelectedRows.Count > 0;
        _addButton.Enabled = any;
    }

    private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        EnableDisableButtons();
    }

    private void EnableDisableButtons()
    {
        _editTableButton.Enabled = _dstGrid.SelectedCells.Count == 1;
    }

    private void EditTableBtn_Click(object sender, EventArgs e)
    {
        var row = ((DataRowView)_dstGrid.SelectedRows[0].DataBoundItem).Row;
        var sourceTable = (SourceTable)row["source_table"];
        if (sourceTable.SourceIsTable)
        {
            using DatabaseImportRenameTableForm f = new(sourceTable.SourceTableName, sourceTable.TargetTableName);
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                sourceTable.TargetTableName = f.NewName;
                row["display_name"] = sourceTable.DisplayText;
            }
        }
        else if (sourceTable.SourceIsSql)
        {
            using DatabaseImportCustomQueryForm f = new(_session, sourceTable.TargetTableName, sourceTable.SourceSql);
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                sourceTable.SourceSql = f.Sql;
                sourceTable.TargetTableName = f.TargetName;
                row["display_name"] = sourceTable.DisplayText;
            }
        }
        else
        {
            Debug.Fail("No source table type.");
        }
        Focus();
    }

    private void OkBtn_Click(object sender, EventArgs e)
    {
        string sql;
        try
        {
            sql = _session.GenerateSql(GetSelectedTables(), _methodLinkRad.Checked);
        }
        catch (Exception ex)
        {
            Ui.ShowError(this, "Error", ex);
            return;
        }

        var text = $"Importing from database...";
        WaitForm.GoWithCancel(
            this,
            "Import",
            text,
            out var success,
            cancel =>
            {
                SqlUtil.WithCancellableTransaction(
                    _manager.Notebook,
                    () =>
                    {
                        _manager.ExecuteScript(sql);
                    },
                    cancel
                );
            }
        );
        _manager.Rescan();
        _manager.SetDirty();
        if (!success)
        {
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private List<SourceTable> GetSelectedTables()
    {
        List<SourceTable> tables = new();
        foreach (DataRow row in _dataTable.Rows)
        {
            if ((bool)row["to_be_imported"] == true)
            {
                var sourceTable = (SourceTable)row["source_table"];
                tables.Add(sourceTable);
            }
        }
        return tables;
    }

    private void ViewSqlButton_Click(object sender, EventArgs e)
    {
        try
        {
            var sql = _session.GenerateSql(GetSelectedTables(), _methodLinkRad.Checked);
            using DatabaseImportScriptForm f = new(sql);
            f.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Ui.ShowError(this, "Error", ex);
        }
        Focus();
    }

    private void AddQueryButton_Click(object sender, EventArgs e)
    {
        // Find a name like "query1", "query2", etc. that doesn't exist in the notebook and isn't to be imported.
        // First make a list of all the existing names that start with "query" so we can avoid them.
        var notebook = _manager.Notebook;
        List<string> existingNames = new();
        using (var sdt = notebook.Query("SELECT name FROM sqlite_master"))
        {
            foreach (var row in sdt.Rows)
            {
                var name = (string)row[0];
                if (name.StartsWith("query", StringComparison.OrdinalIgnoreCase))
                {
                    existingNames.Add((string)row[0]);
                }
            }
        }

        foreach (DataRow row in _dataTable.Rows)
        {
            var t = (SourceTable)row["source_table"];
            existingNames.Add(t.TargetTableName);
        }

        // Count up until we find a unique name.
        string targetName;
        for (var i = 1; ; i++)
        {
            var proposedTargetName = $"query{i}";
            if (!existingNames.Any(x => x.Equals(proposedTargetName, StringComparison.OrdinalIgnoreCase)))
            {
                targetName = proposedTargetName;
                break;
            }
        }

        // Ask the user for a query.
        DatabaseImportCustomQueryForm f = new(_session, targetName, "SELECT * FROM ");
        var result = f.ShowDialog(this);
        Focus();
        if (result != DialogResult.OK)
        {
            return;
        }

        // Add a SourceTable to the list for this query. Check it by default.
        var sourceTable = SourceTable.FromSql(f.Sql, f.TargetName);

        var newRow = _dataTable.NewRow();
        newRow["source_table"] = sourceTable;
        newRow["display_name"] = sourceTable.DisplayText;
        newRow["to_be_imported"] = true;
        _dataTable.Rows.Add(newRow);
    }

    private void AddButton_Click(object sender, EventArgs e)
    {
        HashSet<DataRow> rows = new();
        foreach (DataGridViewRow row in _srcGrid.SelectedRows)
        {
            var dataRow = ((DataRowView)row.DataBoundItem).Row;
            dataRow["to_be_imported"] = true;
            rows.Add(dataRow);
        }
        _srcGrid.Update();
        _dstGrid.Update();

        foreach (DataGridViewRow row in _dstGrid.Rows)
        {
            var dataRow = ((DataRowView)row.DataBoundItem).Row;
            row.Selected = rows.Contains(dataRow);
        }
    }

    private void RemoveButton_Click(object sender, EventArgs e)
    {
        HashSet<DataRow> rows = new();
        foreach (DataGridViewRow row in _dstGrid.SelectedRows)
        {
            var dataRow = ((DataRowView)row.DataBoundItem).Row;
            dataRow["to_be_imported"] = false;
            rows.Add(dataRow);
        }
        _srcGrid.Update();
        _dstGrid.Update();

        foreach (DataGridViewRow row in _srcGrid.Rows)
        {
            var dataRow = ((DataRowView)row.DataBoundItem).Row;
            row.Selected = rows.Contains(dataRow);
        }
    }

    private void SelectionTable_Paint(object sender, PaintEventArgs e)
    {
        var control = (Control)sender;
        using Pen pen = new(SystemColors.Control, this.Scaled(2));
        var x = control.Width / 2 - this.Scaled(1);
        e.Graphics.DrawLine(pen, x, 0, x, control.Height);
    }

    private void SrcFilterClearButton_Click(object sender, EventArgs e)
    {
        _srcFilterText.Clear();
    }

    private void SrcFilterText_TextChanged(object sender, EventArgs e)
    {
        _srcView.RowFilter = $"to_be_imported = 0 AND display_name LIKE '%{_srcFilterText.Text.Replace("'", "''")}%'";
    }
}
