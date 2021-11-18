﻿using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using SqlNotebook.Properties;
using SqlNotebookScript.Utils;

namespace SqlNotebook;

public partial class TableDocumentControl : UserControl, IDocumentControl, IDocumentControlOpenNotification {
    private readonly NotebookManager _manager;
    private readonly string _tableName;
    private readonly DataGridView _grid;
    private string _query;

    public TableDocumentControl(NotebookManager manager, string tableName) {
        InitializeComponent();
        _manager = manager;
        _tableName = tableName;
        _toolStrip.SetMenuAppearance();

        _grid = DataGridViewUtil.NewDataGridView();
        _grid.Dock = DockStyle.Fill;
        _tablePanel.Controls.Add(_grid);

        Ui ui = new(this, false);
        ui.Init(_scriptBtn, Resources.script_go, Resources.script_go32);
    }

    public void OnOpen() {
        var simpleDataTable = WaitForm.GoWithCancel(TopLevelControl, "Table", "Reading table...\r\n" + _tableName, out var success, cancel => {
            cancel.Register(() => _manager.Notebook.BeginUserCancel());
            try {
                return _manager.Notebook.Query($"SELECT * FROM {_tableName.DoubleQuote()} LIMIT 1000", 1000);
            } finally {
                _manager.Notebook.EndUserCancel();
            }
        });
        if (!success) {
            throw new OperationCanceledException();
        }

        var dt = new DataTable();
        foreach (var col in simpleDataTable.Columns) {
            dt.Columns.Add(col);
        }
        foreach (var row in simpleDataTable.Rows) {
            var dtRow = dt.NewRow();
            dtRow.ItemArray = row;
            dt.Rows.Add(dtRow);
        }
        _grid.DataSource = dt;
        _grid.AutoSizeColumns(this.Scaled(500));

        _query = $"SELECT\r\n{string.Join(",\r\n", simpleDataTable.Columns.Select(x => "    " + x.DoubleQuote()))}\r\nFROM {_tableName.DoubleQuote()}\r\nLIMIT 1000;\r\n";
    }

    // IDocumentControl
    string IDocumentControl.ItemName { get; set; }
    public void Save() { }

    private void ScriptBtn_Click(object sender, EventArgs e) {
        var name = _manager.NewScript();
        _manager.SetItemData(name, _query);
        _manager.OpenItem(new NotebookItem(NotebookItemType.Script, name));
    }
}
