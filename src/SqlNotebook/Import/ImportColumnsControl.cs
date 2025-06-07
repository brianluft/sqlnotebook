using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SqlNotebook.Properties;
using SqlNotebookScript.Utils;

namespace SqlNotebook.Import;

public partial class ImportColumnsControl : UserControl
{
    private static class GridColumn
    {
        public static readonly string Import = "import";
        public static readonly string SourceName = "source_name";
        public static readonly string TargetName = "target_name";
        public static readonly string Conversion = "conversion";
    }

    private readonly DataTable _table;
    private TableSchema _targetTable; // null if the target is a new table

    public NotifySlot Change = new NotifySlot();
    public Slot<bool> Error = new Slot<bool>();
    private IReadOnlyList<string> _detectedTypes = Array.Empty<string>();

    public string SqlColumnList =>
        string.Join(
            ",\r\n",
            from col in GetImportColumns()
            let renamed = col.SourceName != col.TargetName && col.TargetName != null
            select $"    {col.SourceName.DoubleQuote()}{(renamed ? " AS " + col.TargetName.DoubleQuote() : "")} {col.Conversion}"
        );

    public ImportColumnsControl(bool allowDetectTypes = false)
    {
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
        ui.Init(_toolStrip);
        ui.Init(_detectTypesButton, Resources.magic_wand16, Resources.magic_wand32);
        ui.Init(_toolStripSeparator);
        _detectTypesButton.Visible = allowDetectTypes;
        _toolStripSeparator.Visible = allowDetectTypes;
        ui.Init(_setSelectedTypesMenu);
        ui.Init(_importColumn, 10);
        ui.Init(_conversionColumn, 25);

        Bind.OnChange(new Slot[] { Change }, (sender, e) => ValidateGridInput());
    }

    public void SetSourceColumns(IReadOnlyList<string> columnNames, IReadOnlyList<string> detectedTypes = null)
    {
        _detectedTypes = detectedTypes;
        _table.BeginLoadData();
        _table.Clear();
        foreach (var columnName in columnNames)
        {
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

    public void SetTargetToNewTable()
    {
        _targetTable = null;
        ApplyTargetToTable();
        Change.Notify();
    }

    public void SetTargetToExistingTable(TableSchema tableSchema)
    {
        _targetTable = tableSchema;
        ApplyTargetToTable();
        Change.Notify();
    }

    // reset target column names based on the target (new vs. existing table).
    private void ApplyTargetToTable()
    {
        var isNewTable = _targetTable == null;
        _table.BeginLoadData();
        foreach (DataRow row in _table.Rows)
        {
            var sourceName = row.Field<string>(GridColumn.SourceName);
            if (isNewTable)
            {
                // for new tables, import every column with the name as-is
                row.SetField(GridColumn.TargetName, sourceName);
            }
            else
            {
                // for existing tables, try to match a column with the same name, otherwise don't import this
                // column by default
                if (_targetTable.Columns.Any(x => x.Name == sourceName))
                {
                    row.SetField(GridColumn.TargetName, sourceName);
                }
                else
                {
                    row.SetField(GridColumn.TargetName, "");
                }
            }
        }
        _table.EndLoadData();
    }

    private IEnumerable<ImportColumn> GetImportColumns()
    {
        var list = (
            from x in _table.Rows.Cast<DataRow>()
            let c = new ImportColumn
            {
                Import = x.Field<bool>(GridColumn.Import),
                SourceName = x.Field<string>(GridColumn.SourceName),
                TargetName = TargetNameOrNull(x.Field<string>(GridColumn.TargetName)),
                Conversion = x.Field<string>(GridColumn.Conversion),
            }
            where c.Import
            select c
        ).ToList();

        if (!list.Any())
        {
            throw new Exception("At least one column must be selected for import.");
        }

        // check for blank column names
        var missingTargetName = (
            from x in list
            where string.IsNullOrWhiteSpace(x.TargetName)
            select x.SourceName
        ).FirstOrDefault();
        if (missingTargetName != null)
        {
            throw new Exception($"The target column name for source column \"{missingTargetName}\" must be provided.");
        }

        // check for duplicate column names
        var duplicateName = (
            from x in list
            group x by x.TargetName.ToUpper().Trim() into grp
            where grp.Count() > 1
            select grp.Key
        ).FirstOrDefault();
        if (duplicateName != null)
        {
            throw new Exception($"The target column name \"{duplicateName}\" is included more than once.");
        }

        return list;
    }

    private static string TargetNameOrNull(string name) => string.IsNullOrWhiteSpace(name) ? null : name;

    private void ValidateGridInput()
    { // true = passed validation
        var seenLowercaseTargetColumnNames = new HashSet<string>();
        var error = false;
        foreach (DataRow row in _table.Rows)
        {
            var sourceName = row.Field<string>(GridColumn.SourceName);
            var targetName = row.Field<string>(GridColumn.TargetName);
            var lcTargetName = targetName?.ToLower();
            if (string.IsNullOrWhiteSpace(targetName))
            {
                if (!row.Field<bool>(GridColumn.Import))
                {
                    // this is okay, the column will not be imported
                    row.SetColumnError(GridColumn.TargetName, null);
                }
                else
                {
                    error = true;
                    row.SetColumnError(
                        GridColumn.TargetName,
                        $"No column name is specified for the source column named \"{sourceName}\"."
                    );
                }
            }
            else if (seenLowercaseTargetColumnNames.Contains(lcTargetName))
            {
                error = true;
                row.SetColumnError(GridColumn.TargetName, $"The column name \"{targetName}\" is already in use.");
            }
            else if (_targetTable != null && !_targetTable.Columns.Any(x => x.Name.ToLower() == lcTargetName))
            {
                error = true;
                row.SetColumnError(
                    GridColumn.TargetName,
                    $"The column name \"{targetName}\" does not exist in the target table \"{_targetTable.Name}\"."
                );
            }
            else
            {
                row.SetColumnError(GridColumn.TargetName, null);
                seenLowercaseTargetColumnNames.Add(lcTargetName);
            }
        }
        Error.Value = error;
    }

    private void SetTypeMenu_Click(object sender, EventArgs e)
    {
        string type;
        if (ReferenceEquals(sender, _setTypeTextMenu))
        {
            type = "TEXT";
        }
        else if (ReferenceEquals(sender, _setTypeIntegerMenu))
        {
            type = "INTEGER";
        }
        else if (ReferenceEquals(sender, _setTypeRealMenu))
        {
            type = "REAL";
        }
        else if (ReferenceEquals(sender, _setTypeDateMenu))
        {
            type = "DATE";
        }
        else if (ReferenceEquals(sender, _setTypeDateTimeMenu))
        {
            type = "DATETIME";
        }
        else
        {
            Debug.Assert(false);
            return;
        }

        foreach (
            var dataRow in _grid
                .SelectedCells.Cast<DataGridViewCell>()
                .Select(x => ((DataRowView)x.OwningRow.DataBoundItem).Row)
                .Distinct()
        )
        {
            dataRow[GridColumn.Conversion] = type;
        }
    }

    private void DetectTypesButton_Click(object sender, EventArgs e)
    {
        var rowIndex = 0;
        foreach (DataGridViewRow gridRow in _grid.Rows)
        {
            if (rowIndex >= 0 && rowIndex < _detectedTypes.Count)
            {
                var dataRow = ((DataRowView)gridRow.DataBoundItem).Row;
                dataRow[GridColumn.Conversion] = _detectedTypes[rowIndex];
            }
            rowIndex++;
        }
    }
}

public sealed class ImportColumn
{
    public bool Import { get; set; }
    public string SourceName { get; set; }
    public string TargetName { get; set; }
    public string Conversion { get; set; }
}
