using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SqlNotebook.Properties;
using SqlNotebookScript;
using SqlNotebookScript.Interpreter;
using SqlNotebookScript.Utils;

namespace SqlNotebook;

public partial class ConsoleControl : UserControl
{
    private const int MAX_GRID_ROWS = 10;
    private const int MAX_HISTORY = 50;

    private readonly IWin32Window _mainForm;
    private readonly NotebookManager _manager;
    private readonly SqlTextControl _inputText;
    private readonly FlowLayoutPanel _outputFlow;
    private readonly Padding _outputSqlMargin;
    private readonly Padding _outputTableMargin;
    private readonly Padding _outputCountMargin;
    private readonly Size _spacerSize;

    public ConsoleControl(IWin32Window mainForm, NotebookManager manager)
    {
        InitializeComponent();
        _mainForm = mainForm;
        _manager = manager;

        _inputText = new(false) { Dock = DockStyle.Fill };
        _inputText.SetVerticalScrollbarVisibility(SqlTextControl.ScrollbarVisibility.Auto);
        _inputText.F5KeyPress += InputText_F5KeyPress;
        _inputPanel.Controls.Add(_inputText);

        _outputFlow = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
        };
        _outputPanel.Controls.Add(_outputFlow);

        Ui ui = new(this, false);
        ui.Init(_table);
        ui.Init(_executeButton, Resources.ControlPlayBlue, Resources.control_play_blue32);
        _executeButton.Margin = Padding.Empty;
        _table.RowStyles[1].Height = ui.XHeight(4);
        _inputBorderPanel.Margin = new(0, ui.XHeight(0.2), 0, 0);

        _outputSqlMargin = new(ui.XWidth(1), ui.XHeight(0.5), ui.XWidth(1), ui.XHeight(0.75));
        _outputTableMargin = new(ui.XWidth(8), 0, ui.XWidth(1), ui.XHeight(0.75));
        _outputCountMargin = new(ui.XWidth(8), 0, 0, 0);
        _spacerSize = new(0, ui.XHeight(1));

        _contextMenuStrip.SetMenuAppearance();
        ui.Init(_clearHistoryMenu, Resources.Delete, Resources.delete32);
        _outputFlow.ContextMenuStrip = _contextMenuStrip;
        _outputPanel.ContextMenuStrip = _contextMenuStrip;

        void OptionsUpdated()
        {
            var opt = UserOptions.Instance;
            _outputPanel.BackColor = opt.GetColors()[UserOptionsColor.GRID_BACKGROUND];
        }
        ;
        OptionsUpdated();
        UserOptions.OnUpdate(this, OptionsUpdated);
    }

    private void Log(string sql, ScriptOutput output)
    {
        var maxColWidth = Ui.XWidth(50, this);
        _outputFlow.SuspendLayout();
        while (_outputFlow.Controls.Count > MAX_HISTORY)
        {
            _outputFlow.Controls.RemoveAt(0);
        }

        if (!string.IsNullOrWhiteSpace(sql))
        {
            Label label = new()
            {
                AutoSize = true,
                Text = sql,
                Margin = _outputSqlMargin,
                Cursor = Cursors.Hand,
            };

            void OptionsUpdated()
            {
                var opt = UserOptions.Instance;
                label.Font = opt.GetCodeFont();
                var colors = opt.GetColors();
                label.BackColor = colors[UserOptionsColor.GRID_BACKGROUND];
                label.ForeColor = colors[UserOptionsColor.GRID_PLAIN];
            }
            OptionsUpdated();
            UserOptions.OnUpdate(label, OptionsUpdated);

            label.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _inputText.SqlText = sql;
                    TakeFocus();
                }
            };
            _outputFlow.Controls.Add(label);
        }

        if ((output.TextOutput?.Count ?? 0) > 0)
        {
            var text = string.Join(Environment.NewLine, output.TextOutput);
            Label label = new()
            {
                AutoSize = true,
                Text = text,
                Margin = _outputTableMargin,
                ContextMenuStrip = _contextMenuStrip,
            };

            void OptionsUpdated()
            {
                var opt = UserOptions.Instance;
                label.Font = opt.GetDataTableFont();
                var colors = opt.GetColors();
                label.BackColor = colors[UserOptionsColor.GRID_BACKGROUND];
                label.ForeColor = colors[UserOptionsColor.GRID_PLAIN];
            }
            OptionsUpdated();
            UserOptions.OnUpdate(label, OptionsUpdated);

            _outputFlow.Controls.Add(label);
        }

        if (output.ScalarResult != null)
        {
            Label label = new()
            {
                AutoSize = true,
                Text = output.ScalarResult.ToString(),
                Margin = _outputTableMargin,
            };

            void OptionsUpdated()
            {
                var opt = UserOptions.Instance;
                label.Font = opt.GetDataTableFont();
                var colors = opt.GetColors();
                label.BackColor = colors[UserOptionsColor.GRID_BACKGROUND];
                label.ForeColor = colors[UserOptionsColor.GRID_PLAIN];
            }
            ;
            OptionsUpdated();
            UserOptions.OnUpdate(label, OptionsUpdated);

            _outputFlow.Controls.Add(label);
        }

        foreach (var simpleDataTable in output.DataTables)
        {
            Label label = new()
            {
                AutoSize = true,
                Text =
                    simpleDataTable.FullCount > MAX_GRID_ROWS
                        ? $"{simpleDataTable.FullCount:#,##0} row{(simpleDataTable.FullCount == 1 ? "" : "s")} ({MAX_GRID_ROWS:#,##0} shown)"
                        : $"{simpleDataTable.FullCount:#,##0} row{(simpleDataTable.FullCount == 1 ? "" : "s")}",
                Margin = _outputCountMargin,
            };

            void OptionsUpdated()
            {
                var opt = UserOptions.Instance;
                label.Font = opt.GetDataTableFont();
                var colors = opt.GetColors();
                label.BackColor = colors[UserOptionsColor.GRID_BACKGROUND];
                label.ForeColor = colors[UserOptionsColor.GRID_PLAIN];
            }
            ;
            OptionsUpdated();
            UserOptions.OnUpdate(label, OptionsUpdated);

            _outputFlow.Controls.Add(label);

            var grid = DataGridViewUtil.NewDataGridView(allowColumnResize: false, allowSort: false);
            grid.Margin = _outputTableMargin;
            grid.ContextMenuStrip = _contextMenuStrip;
            grid.ScrollBars = ScrollBars.None;
            _outputFlow.Controls.Add(grid);
            grid.DataSource = simpleDataTable.ToDataTable(MAX_GRID_ROWS);
            grid.ClearSelection();

            // Why do I need BeginInvoke() for autosize to work properly?
            BeginInvoke(
                new Action(() =>
                {
                    grid.AutoSizeColumns(maxColWidth);
                    grid.Size = new(
                        grid.Columns.OfType<DataGridViewColumn>().Sum(x => x.Width),
                        grid.ColumnHeadersHeight + grid.Rows.OfType<DataGridViewRow>().Sum(x => x.Height)
                    );
                })
            );
        }

        _outputFlow.Controls.Add(new Panel { Size = _spacerSize, AutoSize = false });

        _outputFlow.ResumeLayout(true);

        if (_outputPanel.AutoScrollPosition.X != 0)
        {
            _outputPanel.AutoScrollPosition = new(0, _outputPanel.AutoScrollPosition.Y);
        }
        _outputPanel.ScrollControlIntoView(_outputFlow.Controls[_outputFlow.Controls.Count - 1]);
    }

    public void TakeFocus()
    {
        _inputText.SqlFocus();
        _inputText.SqlSelectAll();
    }

    private void ExecuteButton_Click(object sender, EventArgs e)
    {
        Execute();
    }

    private void Execute()
    {
        var sql = _inputText.SqlText.Trim();

        if (string.IsNullOrWhiteSpace(sql))
        {
            return;
        }

        using var output = WaitForm.GoWithCancel(
            TopLevelControl,
            "Console",
            "Executing...",
            out var success,
            cancel =>
            {
                return SqlUtil.WithCancellation(
                    _manager.Notebook,
                    () =>
                    {
                        using var status = WaitStatus.StartRows("Script output");
                        return _manager.ExecuteScript(sql, onRow: status.IncrementRows);
                    },
                    cancel
                );
            }
        );
        _manager.SetDirty();
        _manager.Rescan();
        if (!success)
        {
            return;
        }

        _inputText.SqlText = "";
        Log(sql, output);
        TakeFocus();
    }

    private void ClearHistoryMenu_Click(object sender, EventArgs e)
    {
        _outputFlow.Controls.Clear();
    }

    private void InputText_F5KeyPress(object sender, EventArgs e)
    {
        Execute();
    }
}
