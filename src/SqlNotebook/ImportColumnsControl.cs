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
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SqlNotebookScript.Utils;

namespace SqlNotebook {
    public partial class ImportColumnsControl : UserControl {
        private static class GridColumn {
            public static readonly string Import = "import";
            public static readonly string SourceName = "source_name";
            public static readonly string TargetName = "target_name";
            public static readonly string Conversion = "conversion";
        }

        private readonly DataTable _table;
        private TableSchema _targetTable; // null if the target is a new table

        public NotifySlot Change = new NotifySlot();
        public Slot<bool> Error = new Slot<bool>();

        public string SqlColumnList =>
            string.Join(",\r\n",
                from col in GetImportColumns()
                let renamed = col.SourceName != col.TargetName && col.TargetName != null
                select $"    {col.SourceName.DoubleQuote()}{(renamed ? " AS " + col.TargetName.DoubleQuote() : "")} {col.Conversion}"
            );

        public ImportColumnsControl() {
            InitializeComponent();

            _table = new DataTable();
            _table.Columns.Add(GridColumn.Import, typeof(bool));
            _table.Columns.Add(GridColumn.SourceName, typeof(string));
            _table.Columns.Add(GridColumn.TargetName, typeof(string));
            _table.Columns.Add(GridColumn.Conversion, typeof(string));
            _grid.AutoGenerateColumns = false;
            _grid.DataSource = _table;
            _grid.CellValueChanged += (sender, e) => Change.Notify();
            _grid.ApplyOneClickComboBoxFix();
            _grid.EnableDoubleBuffering();

            Ui ui = new(this, false);
            ui.Init(_importColumn, 10);
            ui.Init(_conversionColumn, 25);

            Bind.OnChange(new Slot[] { Change },
                (sender, e) => ValidateGridInput());
        }

        public void SetFixedColumnWidths(int? conversionWidth = null) {
            using var g = CreateGraphics();
            var x = g.MeasureString("x", Font, PointF.Empty, StringFormat.GenericTypographic);
            _conversionColumn.Width = (int)((conversionWidth ?? 25) * x.Width);
        }

        public void SetSourceColumns(IReadOnlyList<string> columnNames) {
            _table.BeginLoadData();
            _table.Clear();
            foreach (var columnName in columnNames) {
                var row = _table.NewRow();
                row.SetField(GridColumn.Import, true);
                row.SetField(GridColumn.SourceName, columnName);
                // target_name will be set by ApplyTargetToTable() below
                row.SetField(GridColumn.Conversion, "TEXT");
                _table.Rows.Add(row);
            }
            _table.EndLoadData();

            ApplyTargetToTable();
            Change.Notify();
        }

        public void SetTargetToNewTable() {
            _targetTable = null;
            ApplyTargetToTable();
            Change.Notify();
        }

        public void SetTargetToExistingTable(TableSchema tableSchema) {
            _targetTable = tableSchema;
            ApplyTargetToTable();
            Change.Notify();
        }

        // reset target column names based on the target (new vs. existing table).
        private void ApplyTargetToTable() {
            var isNewTable = _targetTable == null;
            _table.BeginLoadData();
            foreach (DataRow row in _table.Rows) {
                var sourceName = row.Field<string>(GridColumn.SourceName);
                if (isNewTable) {
                    // for new tables, import every column with the name as-is
                    row.SetField(GridColumn.TargetName, sourceName);
                } else {
                    // for existing tables, try to match a column with the same name, otherwise don't import this
                    // column by default
                    if (_targetTable.Columns.Any(x => x.Name == sourceName)) {
                        row.SetField(GridColumn.TargetName, sourceName);
                    } else {
                        row.SetField(GridColumn.TargetName, "");
                    }
                }
            }
            _table.EndLoadData();
        }

        private IEnumerable<ImportColumn> GetImportColumns() {
            var list =
                (from x in _table.Rows.Cast<DataRow>()
                let c = new ImportColumn {
                    Import = x.Field<bool>(GridColumn.Import),
                    SourceName = x.Field<string>(GridColumn.SourceName),
                    TargetName = TargetNameOrNull(x.Field<string>(GridColumn.TargetName)),
                    Conversion = x.Field<string>(GridColumn.Conversion)
                }
                where c.Import
                select c
                ).ToList();

            if (!list.Any()) {
                throw new Exception("At least one column must be selected for import.");
            }

            // check for blank column names
            var missingTargetName =
                (from x in list
                where string.IsNullOrWhiteSpace(x.TargetName)
                select x.SourceName
                ).FirstOrDefault();
            if (missingTargetName != null) {
                throw new Exception($"The target column name for source column \"{missingTargetName}\" must be provided.");
            }

            // check for duplicate column names
            var duplicateName =
                (from x in list
                group x by x.TargetName.ToUpper().Trim() into grp
                where grp.Count() > 1
                select grp.Key
                ).FirstOrDefault();
            if (duplicateName != null) {
                throw new Exception($"The target column name \"{duplicateName}\" is included more than once.");
            }

            return list;
        }

        private static string TargetNameOrNull(string name) => string.IsNullOrWhiteSpace(name) ? null : name;

        private void ValidateGridInput() { // true = passed validation
            var seenLowercaseTargetColumnNames = new HashSet<string>();
            var error = false;
            foreach (DataRow row in _table.Rows) {
                var sourceName = row.Field<string>(GridColumn.SourceName);
                var targetName = row.Field<string>(GridColumn.TargetName);
                var lcTargetName = targetName?.ToLower();
                if (string.IsNullOrWhiteSpace(targetName)) {
                    if (!row.Field<bool>(GridColumn.Import)) {
                        // this is okay, the column will not be imported
                        row.SetColumnError(GridColumn.TargetName, null);
                    } else {
                        error = true;
                        row.SetColumnError(GridColumn.TargetName, $"No column name is specified for the source column named \"{sourceName}\".");
                    }
                } else if (seenLowercaseTargetColumnNames.Contains(lcTargetName)) {
                    error = true;
                    row.SetColumnError(GridColumn.TargetName, $"The column name \"{targetName}\" is already in use.");
                } else if (_targetTable != null && !_targetTable.Columns.Any(x => x.Name.ToLower() == lcTargetName)) {
                    error = true;
                    row.SetColumnError(GridColumn.TargetName, $"The column name \"{targetName}\" does not exist in the target table \"{_targetTable.Name}\".");
                } else {
                    row.SetColumnError(GridColumn.TargetName, null);
                    seenLowercaseTargetColumnNames.Add(lcTargetName);
                }
            }
            Error.Value = error;
        }
    }

    public sealed class ImportColumn {
        public bool Import { get; set; }
        public string SourceName { get; set; }
        public string TargetName { get; set; }
        public string Conversion { get; set; }
    }
}
