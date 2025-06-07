using System.Windows.Forms;

namespace SqlNotebook.Import.Database;

public sealed class DatabaseImporter
{
    private readonly NotebookManager _manager;
    private readonly IWin32Window _owner;

    public DatabaseImporter(NotebookManager manager, IWin32Window owner)
    {
        _manager = manager;
        _owner = owner;
    }

    public void DoDatabaseImport<T>()
        where T : IImportSession, new()
    {
        T session = new();
        if (!session.FromConnectForm(_owner))
        {
            return;
        }

        using DatabaseImportTablesForm f = new(_manager, session);
        f.ShowDialog(_owner);
    }
}
