using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SqlNotebookScript;
using SqlNotebookScript.DataTables;
using SqlNotebookScript.Interpreter;
using SqlNotebookScript.Utils;

namespace SqlNotebook.Import.Csv;

public partial class ImportCsvForm : ZForm
{
    private readonly string _filePath;
    private readonly DatabaseSchema _databaseSchema;
    private readonly NotebookManager _manager;

    private readonly ImportCsvOptionsControl _optionsControl;
    private readonly Slot<string> _optionsError = new Slot<string>();

    private readonly LoadingContainerControl _columnsLoadControl;
    private readonly ImportColumnsControl _columnsControl;
    private readonly Slot<string> _columnsError = new Slot<string>();
    private Guid _columnsLoadId;

    private readonly LoadingContainerControl _inputPreviewLoadControl;
    private readonly ImportCsvPreviewControl _inputPreviewControl;
    private readonly Slot<string> _inputPreviewError = new Slot<string>();
    private Guid _inputPreviewLoadId;

    public ImportCsvForm(string filePath, DatabaseSchema schema, NotebookManager manager)
    {
        InitializeComponent();
        _filePath = filePath;
        _databaseSchema = schema;
        _manager = manager;

        _optionsControl = new ImportCsvOptionsControl(schema)
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        _optionsPanel.Controls.Add(_optionsControl);

        _columnsControl = new ImportColumnsControl(allowDetectTypes: true) { Dock = DockStyle.Fill };
        _columnsLoadControl = new LoadingContainerControl { ContainedControl = _columnsControl, Dock = DockStyle.Fill };
        _columnsPanel.Controls.Add(_columnsLoadControl);

        _inputPreviewControl = new ImportCsvPreviewControl { Dock = DockStyle.Fill };
        _inputPreviewLoadControl = new LoadingContainerControl
        {
            ContainedControl = _inputPreviewControl,
            Dock = DockStyle.Fill,
        };
        _originalFilePanel.Controls.Add(_inputPreviewLoadControl);

        Ui ui = new(this, 170, 45);
        ui.Init(_table);
        ui.Init(_outerSplit, 0.48);
        ui.InitHeader(_originalFileLabel);
        ui.Init(_lowerSplit, 0.52);
        ui.InitHeader(_optionsLabel);
        ui.InitHeader(_columnsLabel);
        ui.Init(_buttonFlow1);
        ui.MarginTop(_buttonFlow1);
        ui.Init(_previewButton);
        ui.Init(_buttonFlow2);
        ui.MarginTop(_buttonFlow2);
        ui.Init(_okBtn);
        ui.Init(_cancelBtn);

        Load += async (sender, e) =>
        {
            ValidateOptions();
            await UpdateControls(inputChange: true);
            _optionsControl.SelectTableCombo();
        };

        var o = _optionsControl;
        Bind.OnChange(
            new Slot[] { o.TargetTableName },
            async (sender, e) =>
            {
                ValidateOptions();
                await UpdateControls(columnsChange: true);
            }
        );
        Bind.OnChange(new Slot[] { o.FileEncoding }, async (sender, e) => await UpdateControls(inputChange: true));
        Bind.OnChange(
            new Slot[] { o.IfTableExists, o.SkipLines, o.HasColumnHeaders, o.Separator },
            async (sender, e) => await UpdateControls(columnsChange: true)
        );
        Bind.BindAny(
            new[] { _columnsLoadControl.IsOverlayVisible, _inputPreviewLoadControl.IsOverlayVisible },
            x => _okBtn.Enabled = !x
        );

        Text = $"Import {Path.GetFileName(_filePath)}";
        o.TargetTableName.Value = Path.GetFileNameWithoutExtension(_filePath);
    }

    private async Task UpdateControls(bool inputChange = false, bool columnsChange = false)
    {
        if (inputChange)
        {
            await UpdateInputPreview();
            columnsChange = true;
        }

        if (columnsChange)
        {
            await UpdateColumns();
        }
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_optionsControl.TargetTableName))
        {
            _optionsError.Value = "You must enter a target table name.";
        }
        else
        {
            _optionsError.Value = null;
        }
    }

    private async Task UpdateInputPreview()
    {
        var loadId = Guid.NewGuid();
        _inputPreviewLoadId = loadId;
        _inputPreviewLoadControl.PushLoad();
        try
        {
            var tempTableName = Guid.NewGuid().ToString();
            var fileEncoding = _optionsControl.FileEncoding.Value;
            var text = await Task.Run(() =>
            {
                try
                {
                    var importSql =
                        @"IMPORT TXT @filePath INTO @tableName (number, line)
                        OPTIONS (TAKE_LINES: 1000, TEMPORARY_TABLE: 1, FILE_ENCODING: @encoding);";
                    _manager.ExecuteScriptNoOutput(
                        importSql,
                        new Dictionary<string, object>
                        {
                            ["@filePath"] = _filePath,
                            ["@tableName"] = tempTableName,
                            ["@encoding"] = fileEncoding,
                        }
                    );

                    using var dt = _manager
                        .ExecuteScript($"SELECT line FROM {tempTableName.DoubleQuote()} ORDER BY number")
                        .DataTables[0];

                    return string.Join(Environment.NewLine, dt.Rows.Select(x => x[0].ToString()));
                }
                finally
                {
                    _manager.ExecuteScriptNoOutput($"DROP TABLE IF EXISTS {tempTableName.DoubleQuote()}");
                }
            });

            if (_inputPreviewLoadId == loadId)
            {
                _inputPreviewError.Value = null;
                _inputPreviewControl.PreviewText = text;
            }
        }
        catch (UncaughtErrorScriptException ex)
        {
            if (_inputPreviewLoadId == loadId)
            {
                _inputPreviewError.Value = $"Error loading the input file:\r\n{ex.ErrorMessage}";
                _inputPreviewLoadControl.SetError(_inputPreviewError.Value);
            }
        }
        finally
        {
            _inputPreviewLoadControl.PopLoad();
        }
    }

    private async Task UpdateColumns()
    {
        var loadId = Guid.NewGuid();
        _columnsLoadId = loadId;
        _columnsLoadControl.PushLoad();

        try
        {
            var (sourceColumns, detectedTypes) = await GetSourceColumns();
            if (_columnsLoadId == loadId)
            {
                _columnsControl.SetSourceColumns(sourceColumns, detectedTypes);
                UpdateTargetColumns();
                _columnsLoadControl.ClearError();
                _columnsError.Value = null;
            }
        }
        catch (UncaughtErrorScriptException ex)
        {
            if (_columnsLoadId == loadId)
            {
                _columnsError.Value = $"Error importing the CSV file:\r\n{ex.ErrorMessage}";
                _columnsLoadControl.SetError(_columnsError.Value);
            }
        }
        catch (Exception ex)
        {
            if (_columnsLoadId == loadId)
            {
                _columnsError.Value = $"Error importing the CSV file:\r\n{ex.GetExceptionMessage()}";
                _columnsLoadControl.SetError(_columnsError.Value);
            }
        }
        finally
        {
            _columnsLoadControl.PopLoad();
        }
    }

    private async Task<(IReadOnlyList<string> Names, IReadOnlyList<string> DetectedTypes)> GetSourceColumns()
    {
        var tempTableName = Guid.NewGuid().ToString();
        try
        {
            var headerRow = _optionsControl.HasColumnHeaders.Value;
            var fileEncoding = _optionsControl.FileEncoding.Value;
            var skipLines = _optionsControl.SkipLines.Value;
            var separator = _optionsControl.Separator.Value;
            return await Task.Run(() =>
            {
                var importSql =
                    @$"IMPORT CSV @filePath INTO @tableName
                    OPTIONS (SKIP_LINES: @skipLines, TAKE_LINES: 1000, HEADER_ROW: @headerRow, TEMPORARY_TABLE: 1, 
                        FILE_ENCODING: @encoding, SEPARATOR: @sep);
                    SELECT * FROM {SqlUtil.DoubleQuote(tempTableName)};";
                var output = _manager.ExecuteScript(
                    importSql,
                    new Dictionary<string, object>
                    {
                        ["@filePath"] = _filePath,
                        ["@tableName"] = tempTableName,
                        ["@sep"] = separator,
                        ["@headerRow"] = headerRow ? 1 : 0,
                        ["@encoding"] = fileEncoding,
                        ["@skipLines"] = skipLines,
                    }
                );

                IReadOnlyList<string> detectedTypes = null;
                try
                {
                    detectedTypes = TypeDetection.DetectTypes(output.DataTables[0]);
                }
                catch
                {
                    // Don't let this blow up the import.
                    detectedTypes = Array.Empty<string>();
                }

                using var dt = _manager.ExecuteScript($"PRAGMA TABLE_INFO ({tempTableName.DoubleQuote()})").DataTables[
                    0
                ];
                var nameCol = dt.GetIndex("name");
                return (
                    dt.Rows.Select(x => x[nameCol].ToString()).Where(x => !string.IsNullOrEmpty(x)).ToList(),
                    detectedTypes
                );
            });
        }
        finally
        {
            await Task.Run(() =>
            {
                _manager.ExecuteScriptNoOutput($"DROP TABLE IF EXISTS {tempTableName.DoubleQuote()}");
            });
        }
    }

    private void UpdateTargetColumns()
    {
        var targetTable = _optionsControl.TargetTableName.Value;

        if (_databaseSchema.NonTables.Contains(targetTable.ToLower()))
        {
            throw new Exception($"\"{targetTable}\" already exists, but is not a table.");
        }

        if (_optionsControl.IfTableExists.Value == ImportTableExistsOption.DropTable)
        {
            _columnsControl.SetTargetToNewTable();
        }
        else if (_databaseSchema.Tables.TryGetValue(targetTable.ToLower(), out var schema))
        {
            _columnsControl.SetTargetToExistingTable(schema);
        }
        else
        {
            _columnsControl.SetTargetToNewTable();
        }
    }

    private string GetImportSql(int takeRows = -1, string temporaryTableName = null)
    {
        var truncate = _optionsControl.IfTableExists.Value != ImportTableExistsOption.AppendNewRows;
        var drop = _optionsControl.IfTableExists.Value == ImportTableExistsOption.DropTable;
        var tableName = temporaryTableName ?? _optionsControl.TargetTableName.Value;
        var separator = _optionsControl.Separator.Value;

        StringBuilder sb = new();
        if (drop)
        {
            sb.AppendLine($"DROP TABLE IF EXISTS {_optionsControl.TargetTableName.Value.DoubleQuote()};");
            sb.AppendLine();
        }
        sb.AppendLine($"IMPORT CSV {_filePath.SingleQuote()}");
        sb.AppendLine($"INTO {tableName.DoubleQuote()} (");
        sb.AppendLine(_columnsControl.SqlColumnList);
        List<string> options = new();
        if (_optionsControl.SkipLines.Value != 0)
        {
            options.Add($"    SKIP_LINES: {_optionsControl.SkipLines.Value}");
        }
        if (takeRows >= 0)
        {
            options.Add($"    TAKE_LINES: {takeRows}");
        }
        if (temporaryTableName != null)
        {
            options.Add($"    TEMPORARY_TABLE: 1");
        }
        if (!_optionsControl.HasColumnHeaders.Value)
        {
            options.Add($"    HEADER_ROW: 0");
        }
        if (separator != ",")
        {
            options.Add($"    SEPARATOR: {separator.SingleQuote()}");
        }
        if (truncate)
        {
            options.Add($"    TRUNCATE_EXISTING_TABLE: 1");
        }
        if (_optionsControl.FileEncoding.Value != 0)
        {
            options.Add($"    FILE_ENCODING: {_optionsControl.FileEncoding.Value}");
        }
        if (_optionsControl.IfConversionFails.Value != ImportConversionFailOption.ImportAsText)
        {
            options.Add($"    IF_CONVERSION_FAILS: {(int)_optionsControl.IfConversionFails.Value}");
        }
        if (_optionsControl.BlankValues.Value != BlankValuesOption.Null)
        {
            options.Add($"    BLANK_VALUES: {(int)_optionsControl.BlankValues.Value}");
        }
        if (options.Count > 0)
        {
            sb.AppendLine(") OPTIONS (");
            foreach (var x in options.Take(options.Count - 1))
            {
                sb.Append(x);
                sb.Append(',');
                sb.AppendLine();
            }
            sb.AppendLine(options[^1]);
        }
        sb.AppendLine(");");

        return sb.ToString();
    }

    private string GetErrorMessage()
    { // or null
        if (_columnsError.Value != null)
        {
            // this error is shown in the columns pane
            return "Please correct the error in the \"Columns\" pane.";
        }
        else if (_optionsError.Value != null)
        {
            // this error is not shown
            return _optionsError.Value;
        }
        else if (_inputPreviewError.Value != null)
        {
            // this error is shown in the original file pane
            return "Please correct the error in the \"Original File\" pane.";
        }
        else
        {
            return null;
        }
    }

    private void OkBtn_Click(object sender, EventArgs e)
    {
        var errorMessage = GetErrorMessage();
        if (errorMessage == null)
        {
            var sql = GetImportSql();
            WaitForm.GoWithCancel(
                this,
                "Import",
                "Importing CSV file...",
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
            Close();
        }
        else
        {
            Ui.ShowError(this, "Import Error", errorMessage);
        }
    }

    private void PreviewButton_Click(object sender, EventArgs e)
    {
        var errorMessage = GetErrorMessage();
        if (errorMessage != null)
        {
            Ui.ShowError(this, "Error", errorMessage);
            return;
        }

        string script = null;
        SimpleDataTable table = null;
        WaitForm.GoWithCancel(
            this,
            "Import",
            "Generating preview...",
            out var success,
            cancel =>
            {
                var tableName = "preview_" + Guid.NewGuid().ToString();
                script = GetImportSql();
                SqlUtil.WithCancellableTransaction(
                    _manager.Notebook,
                    () =>
                    {
                        var sql = GetImportSql(100, tableName);
                        _manager.ExecuteScriptNoOutput(sql);
                        table = _manager.ExecuteScript($"SELECT * FROM {tableName.DoubleQuote()}").DataTables[0];
                    },
                    rollback: true,
                    cancel: cancel
                );
            }
        );
        if (!success)
        {
            return;
        }

        using (table)
        {
            using ImportScriptPreviewForm previewForm = new(script, table);
            previewForm.ShowDialog(this);
        }
    }
}
