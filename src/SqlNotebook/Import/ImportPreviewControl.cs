﻿using System.Data;
using System.Windows.Forms;

namespace SqlNotebook.Import;

public partial class ImportPreviewControl : UserControl {
    private readonly DataGridView _grid;

    public ImportPreviewControl(DataTable table) {
        InitializeComponent();

        Controls.Add(_grid = DataGridViewUtil.NewDataGridView());
        _grid.Dock = DockStyle.Fill;
        _grid.DataSource = table;

        Disposed += delegate {
            table.Dispose();
        };
    }
}
