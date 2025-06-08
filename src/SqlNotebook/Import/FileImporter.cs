using System;
using System.IO;
using System.Windows.Forms;
using SqlNotebook.Import.Csv;
using SqlNotebook.Import.Database;
using SqlNotebook.Import.Xls;
using SqlNotebookScript.Utils;

namespace SqlNotebook.Import;

public static class FileImporter
{
    public static string Filter =>
        string.Join(
            "|",
            "All data files|*.csv;*.txt;*.tsv;*.xls;*.xlsx;*.duckdb",
            "Comma-separated values|*.csv;*.tsv;*.txt",
            "Excel workbooks|*.xls;*.xlsx",
            "DuckDB databases|*.duckdb"
        );

    public static void Start(IWin32Window owner, string filePath, NotebookManager manager)
    {
        var schema = ReadDatabaseSchema(owner, manager);
        if (schema == null)
            return;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        switch (extension)
        {
            case ".csv":
            case ".tsv":
            case ".txt":
                ImportCsv(owner, filePath, manager, schema);
                break;

            case ".xls":
            case ".xlsx":
            case ".xlsm":
            case ".xlsb":
                ImportXls(owner, filePath, manager, schema);
                break;

            case ".duckdb":
                ImportDuckDb(owner, filePath, manager);
                break;

            default:
                throw new InvalidOperationException($"The file type \"{extension}\" is not supported.");
        }
    }

    private static void ImportCsv(IWin32Window owner, string filePath, NotebookManager manager, DatabaseSchema schema)
    {
        using ImportCsvForm f = new(filePath, schema, manager);
        f.ShowDialog(owner);
    }

    private static void ImportXls(IWin32Window owner, string filePath, NotebookManager manager, DatabaseSchema schema)
    {
        var input = WaitForm.Go(owner, "Import", "Reading workbook...", out var success, () => XlsInput.Load(filePath));
        if (!success)
        {
            return;
        }

        using ImportXlsForm f = new(input, manager, schema);
        f.ShowDialog(owner);
    }

    private static void ImportDuckDb(IWin32Window owner, string filePath, NotebookManager manager)
    {
        var session = new DuckDBImportSession(filePath);
        if (!session.FromConnectForm(owner))
        {
            return;
        }

        using DatabaseImportTablesForm f = new(manager, session);
        f.ShowDialog(owner);
    }

    private static DatabaseSchema ReadDatabaseSchema(IWin32Window owner, NotebookManager manager)
    {
        var schema = WaitForm.Go(
            owner,
            "Import",
            "Reading notebook...",
            out var success,
            () => DatabaseSchema.FromNotebook(manager.Notebook)
        );
        if (!success)
        {
            return null;
        }
        return schema;
    }
}
