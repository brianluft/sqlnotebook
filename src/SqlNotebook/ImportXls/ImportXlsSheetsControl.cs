﻿// SQL Notebook
// Copyright (C) 2018 Brian Luft
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
using System.Windows.Forms;

namespace SqlNotebook.ImportXls {
    public partial class ImportXlsSheetsControl : UserControl {
        private List<XlsSheetMeta> _list;

        public event EventHandler ValueChanged;

        public ImportXlsSheetsControl() {
            InitializeComponent();
            _grid.AutoGenerateColumns = false;
            _grid.ApplyOneClickComboBoxFix();
            _grid.EnableDoubleBuffering();
            _importTableExistsColumn.Items.AddRange(
                default(ImportTableExistsOption).GetDescriptions().Cast<object>().ToArray());
            _onErrorColumn.Items.AddRange(
                default(ImportConversionFailOption).GetDescriptions().Cast<object>().ToArray());

            Ui ui = new(this, false);
            ui.Init(_toBeImportedColumn, 10);
            ui.Init(_importTableExistsColumn, 35);
            ui.Init(_onErrorColumn, 35);
        }

        public void SetWorksheetInfos(IEnumerable<XlsSheetMeta> list) {
            _list = list.ToList();
            _grid.DataSource = _list;
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e) =>
            ValueChanged?.Invoke(this, EventArgs.Empty);
    }
}
