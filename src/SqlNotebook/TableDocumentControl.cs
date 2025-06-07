using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using SqlNotebook.Properties;
using SqlNotebookScript;
using SqlNotebookScript.Utils;

namespace SqlNotebook;

public partial class TableDocumentControl : UserControl, IDocumentControl, IDocumentControlOpenNotification
{
    private readonly NotebookManager _manager;
    private readonly string _tableName;
    private readonly DataGridView _grid;
    private string _query;

    public TableDocumentControl(NotebookManager manager, string tableName)
    {
        InitializeComponent();
        _manager = manager;
        _tableName = tableName;
        _toolStrip.SetMenuAppearance();

        _grid = DataGridViewUtil.NewDataGridView(contextMenu: true);
        _grid.Dock = DockStyle.Fill;
        _tablePanel.Controls.Add(_grid);

        Ui ui = new(this, false);
        ui.Init(_scriptBtn, Resources.script_go, Resources.script_go32);
    }

    public void OnOpen()
    {
        using var simpleDataTable = WaitForm.GoWithCancel(
            TopLevelControl,
            "Table",
            "Reading table...",
            out var success,
            cancel =>
            {
                using var status = WaitStatus.StartStatic(_tableName);
                cancel.Register(() => _manager.Notebook.BeginUserCancel());
                try
                {
                    return _manager.Notebook.Query($"SELECT * FROM {_tableName.DoubleQuote()} LIMIT 1000");
                }
                finally
                {
                    _manager.Notebook.EndUserCancel();
                }
            }
        );
        if (!success)
        {
            throw new OperationCanceledException();
        }

        _grid.DataSource = simpleDataTable.ToDataTable();
        _grid.AutoSizeColumns(this.Scaled(500));

        _query =
            $"SELECT\r\n{string.Join(",\r\n", simpleDataTable.Columns.Select(x => "    " + x.DoubleQuote()))}\r\nFROM {_tableName.DoubleQuote()}\r\nLIMIT 1000;\r\n";
    }

    // IDocumentControl
    string IDocumentControl.ItemName { get; set; }

    public void Save() { }

    private void ScriptBtn_Click(object sender, EventArgs e)
    {
        var name = _manager.NewScript();
        _manager.SetItemData(name, new ScriptNotebookItemRecord { Name = name, Sql = _query });
        _manager.OpenItem(new NotebookItem(NotebookItemType.Script, name));
    }
}
