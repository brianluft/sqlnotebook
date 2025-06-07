using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SqlNotebook.Properties;
using SqlNotebookScript.Utils;

namespace SqlNotebook;

public partial class ExportForm : ZForm
{
    private readonly NotebookManager _manager;

    public ExportForm(NotebookManager manager)
    {
        InitializeComponent();
        _manager = manager;

        using var g = CreateGraphics();

        var scripts = _manager.Items.Where(x => x.Type == NotebookItemType.Script).Select(x => x.Name);
        var tables = _manager.Items.Where(x => x.Type == NotebookItemType.Table).Select(x => x.Name);
        var views = _manager.Items.Where(x => x.Type == NotebookItemType.View).Select(x => x.Name);

        Ui ui = new(this, 75, 25);
        ui.Init(_table);
        ui.Init(_helpLabel);
        ui.Init(_list);
        ui.MarginTop(_list);
        ui.Init(_buttonFlow);
        ui.MarginTop(_buttonFlow);
        ui.Init(_exportButton);
        ui.Init(_cancelButton);

        ImageList imageList = new()
        {
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new(this.Scaled(16), this.Scaled(16)),
        };
        imageList.Images.Add(
            Ui.GetScaledIcon(this, Resources.script, Ui.ShiftImage(Resources.script32, 0, 1), dispose: false)
        );
        imageList.Images.Add(Ui.GetScaledIcon(this, Resources.table, Resources.table32, dispose: false));
        imageList.Images.Add(Ui.GetScaledIcon(this, Resources.filter, Resources.filter32, dispose: false));
        _list.SmallImageList = imageList;

        foreach (var name in scripts)
        {
            var lvi = _list.Items.Add(name);
            lvi.ImageIndex = 0;
            lvi.Group = _list.Groups[0];
        }
        foreach (var name in tables)
        {
            var lvi = _list.Items.Add(name);
            lvi.ImageIndex = 1;
            lvi.Group = _list.Groups[1];
        }
        foreach (var name in views)
        {
            var lvi = _list.Items.Add(name);
            lvi.ImageIndex = 2;
            lvi.Group = _list.Groups[2];
        }
        _list.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
    }

    private void SaveBtn_Click(object sender, EventArgs e)
    {
        if (_list.SelectedIndices.Count != 1)
        {
            return;
        }
        var lvi = _list.SelectedItems[0];
        var type = (NotebookItemType)Enum.Parse(typeof(NotebookItemType), lvi.Group.Name);
        NotebookItem item = new(type, lvi.Text);

        using SaveFileDialog saveFileDialog = new()
        {
            AddExtension = true,
            AutoUpgradeEnabled = true,
            CheckPathExists = true,
            DefaultExt = ".csv",
            Filter = "CSV files|*.csv",
            OverwritePrompt = true,
            SupportMultiDottedExtensions = true,
            Title = "Save CSV As",
            ValidateNames = true,
        };
        if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }
        var filePath = saveFileDialog.FileName;

        var typeKeyword = type switch
        {
            NotebookItemType.Script => "SCRIPT",
            NotebookItemType.Table => "TABLE",
            NotebookItemType.View => "TABLE",
            _ => throw new InvalidOperationException("Unrecognzied notebook item type."),
        };
        var sql =
            $"EXPORT CSV {filePath.SingleQuote()} "
            + $"FROM {typeKeyword} {item.Name.DoubleQuote()} "
            + $"OPTIONS (TRUNCATE_EXISTING_FILE: 1);";

        WaitForm.GoWithCancel(
            this,
            "Export",
            "Exporting to file...",
            out var success,
            cancel =>
            {
                SqlUtil.WithCancellableTransaction(
                    _manager.Notebook,
                    () =>
                    {
                        _manager.ExecuteScriptNoOutput(sql);
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

        Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/e, /select, \"{filePath}\"" });
        Close();
    }

    private void List_SelectedIndexChanged(object sender, EventArgs e)
    {
        _exportButton.Enabled = _list.SelectedIndices.Count == 1;
    }
}
