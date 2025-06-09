using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SqlNotebook.Import;
using SqlNotebook.Import.Database;
using SqlNotebook.Pages;
using SqlNotebook.Properties;
using SqlNotebookScript;
using SqlNotebookScript.Core;
using SqlNotebookScript.Utils;
using WeifenLuo.WinFormsUI.Docking;

namespace SqlNotebook;

public partial class MainForm : ZForm
{
    private readonly DockPanel _dockPanel;
    private readonly NotebookManager _manager;
    private Notebook _notebook;
    private readonly UserControlDockContent _contentsDockContent;
    private readonly ConsoleControl _console;
    private readonly UserControlDockContent _consoleDockContent;
    private readonly DatabaseImporter _databaseImporter;
    private readonly ExplorerControl _explorer;
    private string FilePath => _notebook.GetFilePath();
    private bool _isNew;
    private readonly CueToolStripTextBox _searchTxt;
    private readonly Slot<bool> _isDirty = new();
    private readonly Slot<bool> _isTransactionOpen = new();
    private Stopwatch _finishedMenuFade;

    private readonly Dictionary<NotebookItem, UserControlDockContent> _openItems =
        new Dictionary<NotebookItem, UserControlDockContent>();

    public MainForm(Notebook notebook, bool isNew)
    {
        InitializeComponent();

        WaitForm.MainAppForm = this;
        Icon = Resources.SqlNotebookIcon;

        _menuStrip.SetMenuAppearance();

        _menuStrip.Items.Insert(
            0,
            _searchTxt = new CueToolStripTextBox
            {
                Alignment = ToolStripItemAlignment.Right,
                CueText = "Search Help (Ctrl+H)",
                ToolTipText = "Search the built-in documentation",
                AutoSize = false,
                Margin = new Padding(0, 0, 5, 0),
            }
        );
        _searchTxt.InnerTextBox.KeyDown += (sender, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                OpenHelp(_searchTxt.Text);
                _searchTxt.Text = "";
            }
        };

        Ui ui = new(this, 180, 50, false);
        MinimumSize = new(Size.Width / 2, Size.Height / 2);
        Padding = new(0, 0, 0, ui.SizeGripHeight);
        var newPageImage16 = Ui.SuperimposePlusSymbol(Resources.page);
        var newPageImage32 = Ui.SuperimposePlusSymbol(Resources.page32);
        var newScriptImage16 = Ui.SuperimposePlusSymbol(Resources.script);
        var newScriptImage32 = Ui.SuperimposePlusSymbol(Ui.ShiftImage(Resources.script32, 0, 1));
        ui.Init(_newMnu, Resources.PageWhite, Resources.page_white32);
        ui.Init(_openMnu, Resources.Folder, Resources.folder32);
        ui.Init(_saveMnu, Resources.Diskette, Resources.diskette32);
        ui.Init(_newScriptMnu, newScriptImage16, newScriptImage32);
        ui.Init(_fileNewPageMenu, newPageImage16, newPageImage32);
        ui.Init(_importFromFileMnu, Resources.TextImports, Resources.text_imports32);
        ui.Init(_exportCsvMnu, Resources.TextExports, Resources.text_exports32);
        ui.Init(_contentsMnu, Resources.tree_list, Resources.tree_list32);
        ui.Init(_consoleMnu, Resources.zone_select, Resources.zone_select32);
        ui.Init(_windowOptionsMenu, Resources.cog, Resources.cog32);
        ui.Init(_viewDocMnu, Resources.Help, Resources.help32);
        ui.Init(_helpReportIssueMenu, Resources.world_link, Resources.world_link32);
        ui.Init(_releaseNotesMenu, Resources.world_link, Resources.world_link32);
        ui.Init(_helpLicenseInformationMenu, Resources.world_link, Resources.world_link32);
        ui.Init(_searchTxt);
        ui.Init(_transactionCommitMenu, Resources.accept_button, Resources.accept_button32);

        var userLayout = RestoreUserLayout();
        var userLayoutScale = userLayout == null ? 1 : (double)DeviceDpi / userLayout.Dpi;

        // Setting the ForeColor of menu items in the designer doesn't seem to work; it gets overwritten. So we do
        // it here.
        _updateAvailableMenu.Paint += (sender, e) =>
        {
            e.Graphics.DrawRectangle(
                Pens.Blue,
                new(0, 0, _updateAvailableMenu.Width - 1, _updateAvailableMenu.Height - 1)
            );
        };
        _transactionMenu.Paint += (sender, e) =>
        {
            e.Graphics.DrawRectangle(Pens.Red, new(0, 0, _transactionMenu.Width - 1, _transactionMenu.Height - 1));
        };
        _updateAvailableMenu.Margin = _transactionMenu.Margin = new(ui.XWidth(1), 0, 0, 0);
        _searchTxt.Margin = new(ui.XWidth(1), 0, ui.XWidth(1), 0);

        _notebook = notebook;
        _isNew = isNew;
        _manager = new NotebookManager(_notebook, _isTransactionOpen);
        _databaseImporter = new DatabaseImporter(_manager, this);
        var dockLeftPortion = userLayout?.GetDockLeftPortion();
        var dockRightPortion = userLayout?.GetDockRightPortion();
        var dockTopPortion = userLayout?.GetDockTopPortion();
        var dockBottomPortion = userLayout?.GetDockBottomPortion();
        _dockPanel = new DockPanel
        {
            Dock = DockStyle.Fill,
            Theme = new VS2012LightTheme { ToolStripRenderer = new MenuRenderer() },
            DocumentStyle = DocumentStyle.DockingWindow,
            DefaultFloatWindowSize = ui.XSize(150, 50),
            ShowDocumentIcon = false,
            DockLeftPortion = dockLeftPortion.HasValue ? dockLeftPortion.Value * userLayoutScale : ui.XWidth(35),
            DockRightPortion = dockRightPortion.HasValue ? dockRightPortion.Value * userLayoutScale : ui.XWidth(35),
            DockTopPortion = dockTopPortion.HasValue ? dockTopPortion.Value * userLayoutScale : ui.XHeight(20),
            DockBottomPortion = dockBottomPortion.HasValue ? dockBottomPortion.Value * userLayoutScale : ui.XHeight(20),
        };
        _toolStripContainer.ContentPanel.Controls.Add(_dockPanel);

        var tocAutoHidePortion = userLayout?.GetTableOfContentsAutoHidePortion();
        _contentsDockContent = new(
            "Table of Contents",
            _explorer = new(_manager, this),
            DockAreas.DockLeft | DockAreas.DockRight
        )
        {
            CloseButtonVisible = false,
        };
        _explorer.NewPageButtonClicked += delegate
        {
            NewPage();
        };
        _explorer.NewScriptButtonClicked += delegate
        {
            NewScript();
        };
        _contentsDockContent.Show(_dockPanel, userLayout?.GetTableOfContentsDockState() ?? DockState.DockLeft);
        _contentsDockContent.AutoHidePortion = tocAutoHidePortion.HasValue
            ? tocAutoHidePortion.Value * userLayoutScale
            : ui.XWidth(35);

        _manager.NotebookItemOpenRequest += Manager_NotebookItemOpenRequest;
        _manager.NotebookItemCloseRequest += Manager_NotebookItemCloseRequest;
        _manager.NotebookItemsSaveRequest += Manager_NotebookItemsSaveRequest;
        _manager.NotebookDirty += (sender, e) => SetDirty();
        _manager.NotebookItemRename += Manager_NotebookItemRename;
        _manager.HandleHotkeyRequest += Manager_HandleHotkeyRequest;

        Load += delegate
        {
            Slot.Bind(
                () =>
                    BeginInvoke(new MethodInvoker(() => _importMnu.Enabled = _saveAsMnu.Enabled = !_isTransactionOpen)),
                _isTransactionOpen
            );
            Slot.Bind(
                () => BeginInvoke(new MethodInvoker(() => _saveMnu.Enabled = _isDirty && !_isTransactionOpen)),
                _isDirty,
                _isTransactionOpen
            );
            _isDirty.Change += (a, b) => SetTitle();
            Slot.Bind(
                () => BeginInvoke(new MethodInvoker(() => _transactionMenu.Visible = _isTransactionOpen)),
                _isTransactionOpen
            );
        };

        if (isNew)
        {
            // create a new script by default in a new notebook
            Load += delegate
            {
                _manager.NewScript();
                _isDirty.Value = false;
            };
        }

        _console = new(this, _manager);
        _consoleDockContent = new("Console", _console, DockAreas.DockBottom);
        _consoleDockContent.CloseButtonVisible = false;
        _consoleDockContent.Show(_dockPanel);
        _consoleDockContent.DockState = userLayout?.GetConsoleDockState() ?? DockState.DockBottomAutoHide;
        var consoleAutoHidePortion = userLayout?.GetConsoleAutoHidePortion();
        _consoleDockContent.AutoHidePortion = consoleAutoHidePortion.HasValue
            ? consoleAutoHidePortion.Value * userLayoutScale
            : ui.XHeight(20);

        Load += async delegate
        {
            _manager.Rescan();

            // open first item named "Page*", "Script*" or "Main"
            var items =
                from x in _manager.Items
                where
                    x.Name.StartsWith("Page", StringComparison.OrdinalIgnoreCase)
                    || x.Name.StartsWith("Script", StringComparison.OrdinalIgnoreCase)
                    || x.Name.Equals("Main", StringComparison.OrdinalIgnoreCase)
                orderby x.Name.ToUpperInvariant()
                select x;
            if (items.Any())
            {
                OpenItem(items.First());
            }

            var updateAvailable = await IsUpdateAvailable();
            if (updateAvailable)
            {
                _updateAvailableMenu.Visible = true;
            }
        };
        SetTitle();

        // No rush on this one; clean up any update MSIs we might have around
        Task.Run(DeleteUpdateDir);
    }

    private void Manager_HandleHotkeyRequest(object sender, HotkeyEventArgs e)
    {
        switch (e.KeyData)
        {
            case (Keys.Control | Keys.N):
                _newMnu.PerformClick();
                break;
            case (Keys.Control | Keys.O):
                _openMnu.PerformClick();
                break;
            case (Keys.Control | Keys.S):
                _saveMnu.PerformClick();
                break;
            case (Keys.Alt | Keys.F4):
                _exitMnu.PerformClick();
                break;
            case (Keys.Control | Keys.Shift | Keys.S):
                _newScriptMnu.PerformClick();
                break;
            case Keys.F1:
                _viewDocMnu.PerformClick();
                break;
            case (Keys.Control | Keys.H):
                _searchDocMnu.PerformClick();
                break;
        }
    }

    private void Manager_NotebookItemRename(object sender, NotebookItemRenameEventArgs e)
    {
        UserControlDockContent ucdc;
        if (_openItems.TryGetValue(e.Item, out ucdc))
        {
            ucdc.Text = e.NewName;
            ucdc.Content.ItemName = e.NewName;
            _openItems.Remove(e.Item);
            var newItem = new NotebookItem(e.Item.Type, e.NewName);
            _openItems.Add(newItem, ucdc);
        }
        var isTableOrView = e.Item.Type == NotebookItemType.Table || e.Item.Type == NotebookItemType.View;
        _manager.Rescan(notebookItemsOnly: !isTableOrView);
    }

    private void Manager_NotebookItemsSaveRequest(object sender, EventArgs e)
    {
        Invoke(new MethodInvoker(SaveOpenItems));
    }

    private void Manager_NotebookItemCloseRequest(object sender, NotebookItemRequestEventArgs e)
    {
        UserControlDockContent ucdc;
        if (_openItems.TryGetValue(e.Item, out ucdc))
        {
            ucdc.Close();
        }
    }

    private void SetTitle()
    {
        string prefix;
        if (_isNew)
        {
            prefix = "Untitled";
        }
        else
        {
            prefix = Path.GetFileNameWithoutExtension(FilePath);
        }
        var star = _isDirty ? "*" : "";
        BeginInvoke(
            new MethodInvoker(() =>
            {
                Text = $"{prefix}{star} - SQL Notebook";
            })
        );
    }

    private void SetDirty()
    {
        _isDirty.Value = true;
    }

    private void Manager_NotebookItemOpenRequest(object sender, NotebookItemRequestEventArgs e)
    {
        OpenItem(e.Item);
    }

    private void ApplySaveOnClose(UserControlDockContent f, IDocumentControl doc)
    {
        f.FormClosing += (sender, e) => doc.Save();
    }

    private void OpenItem(NotebookItem item)
    {
        if (_openItems.TryGetValue(item, out var userControlDockContent))
        {
            userControlDockContent.Activate();
            userControlDockContent.Focus();
            return;
        }

        UserControlDockContent f = null;
        Func<string> getName = null;
        var dockAreas = DockAreas.Document | DockAreas.Float;
        UserControl control = null;
        IDocumentControl doc = null;
        if (item.Type == NotebookItemType.Script)
        {
            var queryControl = new QueryDocumentControl(item.Name, _manager, this);
            doc = queryControl;
            control = queryControl;
            f = new UserControlDockContent(item.Name, control, dockAreas) { Icon = Resources.script32Ico };
            ApplySaveOnClose(f, doc);
            getName = () => doc.ItemName;
        }
        else if (item.Type == NotebookItemType.Table || item.Type == NotebookItemType.View)
        {
            var tableControl = new TableDocumentControl(_manager, item.Name);
            doc = tableControl;
            control = tableControl;
            f = new UserControlDockContent(item.Name, control, dockAreas) { Icon = Resources.table32Ico };
        }
        else if (item.Type == NotebookItemType.Page)
        {
            var pageControl = new PageControl(item.Name, _manager, this);
            doc = pageControl;
            control = pageControl;
            f = new UserControlDockContent(item.Name, control, dockAreas) { Icon = Resources.script32Ico };
            ApplySaveOnClose(f, doc);
            getName = () => doc.ItemName;
        }

        if (getName != null)
        {
            f.FormClosed += (sender2, e2) =>
            {
                // the item may have been renamed since we opened it, so make sure to use getName() for the current
                // name instead of closing over item.Name.
                _openItems.Remove(new NotebookItem(item.Type, getName()));
            };
            _openItems[item] = f;
        }

        f.Show(_dockPanel);

        if (doc is IDocumentControlOpenNotification openNotification)
        {
            try
            {
                openNotification.OnOpen();
            }
            catch (OperationCanceledException)
            {
                f.Close();
            }
            catch (Exception ex)
            {
                Ui.ShowError(this, "Error", ex);
                f.Close();
            }
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        _notebook.Dispose();
        _notebook = null;
    }

    private void AboutMnu_Click(object sender, EventArgs e)
    {
        var siteButton = "Visit sqlnotebook.com";
        var choice = Ui.ShowTaskDialog(
            this,
            "SQL Notebook",
            "About SQL Notebook",
            new[] { siteButton, Ui.OK },
            TaskDialogIcon.Information,
            defaultIsFirst: false,
            details: $"Version {Application.ProductVersion}\r\n© 2016-2025 Brian Luft"
        );
        if (choice == siteButton)
        {
            Process.Start(new ProcessStartInfo("https://sqlnotebook.com/") { UseShellExecute = true });
        }
    }

    private void ImportFileMnu_Click(object sender, EventArgs e)
    {
        using OpenFileDialog openFrm = new()
        {
            AutoUpgradeEnabled = true,
            CheckFileExists = true,
            CheckPathExists = true,
            DereferenceLinks = true,
            Filter = FileImporter.Filter,
            SupportMultiDottedExtensions = true,
            Title = "Import from File",
        };
        if (openFrm.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }
        var filePath = openFrm.FileName;

        try
        {
            FileImporter.Start(this, filePath, _manager);
        }
        catch (Exception ex)
        {
            Ui.ShowError(this, "Import Error", ex);
        }
        Focus();
    }

    private void ImportPostgresMnu_Click(object sender, EventArgs e)
    {
        try
        {
            _databaseImporter.DoDatabaseImport<PostgreSqlImportSession>();
        }
        catch (Exception ex)
        {
            ErrorBox("Import Error", ex.GetExceptionMessage());
        }
        Focus();
    }

    private void ImportMssqlMnu_Click(object sender, EventArgs e)
    {
        try
        {
            _databaseImporter.DoDatabaseImport<SqlServerImportSession>();
        }
        catch (Exception ex)
        {
            ErrorBox("Import Error", ex.GetExceptionMessage());
        }
        Focus();
    }

    private void ImportMysqlMnu_Click(object sender, EventArgs e)
    {
        try
        {
            _databaseImporter.DoDatabaseImport<MySqlImportSession>();
        }
        catch (Exception ex)
        {
            ErrorBox("Import Error", ex.GetExceptionMessage());
        }
        Focus();
    }

    private void ErrorBox(string title, string message, string details = null)
    {
        Ui.ShowError(this, title, message, details);
    }

    private void NewScriptBtn_Click(object sender, EventArgs e)
    {
        NewScript();
    }

    private void NewScript()
    {
        try
        {
            OpenItem(new NotebookItem(NotebookItemType.Script, _manager.NewScript()));
        }
        catch (Exception ex)
        {
            ErrorBox("Notebook Error", "There was a problem creating the script.", ex.GetExceptionMessage());
        }
    }

    private void NewPage()
    {
        try
        {
            OpenItem(new NotebookItem(NotebookItemType.Page, _manager.NewPage()));
        }
        catch (Exception ex)
        {
            ErrorBox("Notebook Error", "There was a problem creating the page.", ex.GetExceptionMessage());
        }
    }

    private void FileNewPageMenu_Click(object sender, EventArgs e)
    {
        NewPage();
    }

    private void NewPageButton_Click(object sender, EventArgs e)
    {
        NewPage();
    }

    private void ExitMnu_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void NewMnu_Click(object sender, EventArgs e)
    {
        SaveUserLayout(offset: true);
        Process.Start(Application.ExecutablePath);
    }

    private void OpenMnu_Click(object sender, EventArgs e)
    {
        using OpenFileDialog f = new()
        {
            AutoUpgradeEnabled = true,
            CheckFileExists = true,
            CheckPathExists = true,
            DefaultExt = ".sqlnb",
            DereferenceLinks = true,
            Filter = "SQL Notebook files|*.sqlnb;*.db;*.sqlite;*.sqlite3",
            Multiselect = false,
            SupportMultiDottedExtensions = true,
            Title = "Open Notebook",
            ValidateNames = true,
        };
        if (f.ShowDialog(this) == DialogResult.OK)
        {
            SaveUserLayout(offset: true);
            Process.Start(Application.ExecutablePath, $"\"{f.FileName}");
        }
    }

    private void SaveMnu_Click(object sender, EventArgs e)
    {
        SaveOrSaveAs();
    }

    private bool SaveOrSaveAs(bool saveAs = false)
    {
        SaveOpenItems();

        if (_isNew || saveAs)
        {
            using SaveFileDialog f = new()
            {
                AddExtension = true,
                AutoUpgradeEnabled = true,
                CheckPathExists = true,
                DefaultExt = ".sqlnb",
                Filter = "SQL Notebook files|*.sqlnb",
                OverwritePrompt = true,
                SupportMultiDottedExtensions = true,
                Title = "Save Notebook As",
                ValidateNames = true,
            };
            if (f.ShowDialog(this) != DialogResult.OK)
            {
                Focus();
                return false;
            }

            WaitForm.GoWithCancel(
                this,
                "Save",
                "Saving notebook...",
                out var success,
                cancel =>
                {
                    _manager.SaveAs(f.FileName, cancel);
                }
            );
            if (!success)
            {
                Focus();
                return false;
            }
            _isNew = false;
        }
        else
        {
            WaitForm.GoWithCancel(
                this,
                "Save",
                "Saving notebook...",
                out _,
                cancel =>
                {
                    _manager.Save(cancel);
                }
            );
        }

        // set this after doing the isNew stuff above so that if you click Save and then cancel the Save As dialog,
        // the file remains dirty. since untitled files are in stored in temporary files it doesn't matter that we
        // saved to it despite the cancelation.
        _isDirty.Value = false;

        _manager.Rescan();
        SetTitle();
        Focus();
        return true;
    }

    private void SaveOpenItems()
    {
        foreach (var x in _openItems.Values)
        {
            x.Content.Save();
        }
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        var success = PrepareToCloseFile();
        if (!success)
        {
            e.Cancel = true;
            return;
        }

        SaveUserLayout();
    }

    private bool PrepareToCloseFile()
    {
        var isTransactionActive = _notebook.IsTransactionActive();
        if (isTransactionActive)
        {
            ErrorBox(
                "SQL Notebook",
                "A transaction is active.",
                "Execute either \"COMMIT\" or \"ROLLBACK\" to end the transaction before exiting."
            );
            return false;
        }

        if (_isDirty)
        {
            var shortFilename = _isNew ? "Untitled" : Path.GetFileNameWithoutExtension(FilePath);

            var result = Ui.ShowTaskDialog(
                this,
                $"Do you want to save changes to {shortFilename}?",
                "SQL Notebook",
                new[] { Ui.SAVE, Ui.DONT_SAVE, Ui.CANCEL }
            );

            if (result == Ui.SAVE)
            {
                try
                {
                    if (!SaveOrSaveAs())
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Ui.ShowError(this, "SQL Notebook", ex);
                    return false;
                }
            }
            else if (result == Ui.CANCEL)
            {
                return false;
            }
        }
        return true;
    }

    private void SaveAsMnu_Click(object sender, EventArgs e)
    {
        SaveOrSaveAs(saveAs: true);
    }

    private void ExportMnu_Click(object sender, EventArgs e)
    {
        SaveOpenItems();

        using ExportForm exportForm = new(_manager);
        exportForm.ShowDialog(this);
        Focus();
    }

    private void ReportIssueMnu_Click(object sender, EventArgs e)
    {
        Process.Start(
            new ProcessStartInfo("https://github.com/brianluft/sqlnotebook/issues/new") { UseShellExecute = true }
        );
    }

    private void OpenHelp(string keywords)
    {
        var results = WaitForm.Go(
            this,
            "Help",
            "Searching help...",
            out var success,
            () => HelpSearcher.Search(keywords)
        );
        if (!success)
        {
            return;
        }
        using HelpSearchResultsForm resultsForm = new(keywords, results);
        resultsForm.ShowDialog(this);
        Focus();
    }

    private void ViewDocMnu_Click(object sender, EventArgs e)
    {
        var htmlFilePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "doc", "doc.html");
        Process.Start(new ProcessStartInfo(htmlFilePath) { UseShellExecute = true });
    }

    private void SearchDocMnu_Click(object sender, EventArgs e)
    {
        _searchTxt.Select();
        _searchTxt.Focus();
    }

    public static async Task<bool> IsUpdateAvailable()
    {
        try
        {
            var appversion = await SharedHttp.Client.GetStringAsync(
                $"https://sqlnotebook.com/appversion.txt?{Guid.NewGuid()}"
            );
            var data = appversion.Split('\n').Select(x => x.Trim()).ToList();
            var version = data[0];
            if (Application.ProductVersion != version)
            {
                return true;
            }
        }
        catch
        {
            // bummer
        }

        return false;
    }

    private void ContentsMnu_Click(object sender, EventArgs e)
    {
        switch (_contentsDockContent.DockState)
        {
            case DockState.DockLeftAutoHide:
            case DockState.DockRightAutoHide:
                _dockPanel.ActiveAutoHideContent = _contentsDockContent;
                break;
        }

        _explorer.TakeFocus();
    }

    private void ConsoleMnu_Click(object sender, EventArgs e)
    {
        if (_consoleDockContent.DockState == DockState.DockBottomAutoHide)
        {
            _dockPanel.ActiveAutoHideContent = _consoleDockContent;
        }

        _console.TakeFocus();
    }

    private void WindowMnu_DropDownOpening(object sender, EventArgs e)
    {
        while (_windowMnu.DropDownItems.Count > 3)
        {
            _windowMnu.DropDownItems.RemoveAt(3);
        }
        if (_dockPanel.Documents.Any())
        {
            _windowMnu.DropDownItems.Add(new ToolStripSeparator());
        }
        foreach (var x in _dockPanel.Documents)
        {
            var text = x.DockHandler.TabText;
            ToolStripMenuItem item = new(text);
            item.Click += delegate
            {
                x.DockHandler.Activate();
            };
            _windowMnu.DropDownItems.Add(item);
        }
    }

    private void TransactionCommitMenu_Click(object sender, EventArgs e)
    {
        WaitForm.Go(
            this,
            "Active Transaction",
            "Committing transaction...",
            out var success,
            () =>
            {
                _manager.ExecuteScriptNoOutput("COMMIT");
            }
        );
        _manager.Rescan();
    }

    private void TransactionRollbackMenu_Click(object sender, EventArgs e)
    {
        WaitForm.Go(
            this,
            "Active Transaction",
            "Rolling back transaction...",
            out var success,
            () =>
            {
                _manager.ExecuteScriptNoOutput("ROLLBACK");
            }
        );
        _manager.Rescan();
    }

    private void UpdateAvailableMenu_Click(object sender, EventArgs e)
    {
        var (version, msiUrl) = WaitForm.Go(
            this,
            "Software Update",
            "Checking for updates...",
            out var success,
            () =>
            {
                var appversion = Program
                    .HttpClient.GetStringAsync("https://sqlnotebook.com/appversion.txt")
                    .GetAwaiter()
                    .GetResult();
                var data = appversion.Split('\n').Select(x => x.Trim()).ToList();
                if (data.Count < 2)
                {
                    throw new Exception("The appversion.txt data is malformed.");
                }
                return (data[0], data[1]);
            }
        );

        var arch = RuntimeInformation.ProcessArchitecture;
        if (arch == Architecture.Arm64)
            msiUrl = msiUrl.Replace("-64bit-", "-arm64-");

        var confirmation = Ui.ShowTaskDialog(
            this,
            $"Version {version} is available. Install now?",
            "SQL Notebook",
            new[] { Ui.INSTALL, Ui.RELEASE_NOTES, Ui.CANCEL }
        );
        if (confirmation == Ui.RELEASE_NOTES)
        {
            ShowReleaseNotes();
            return;
        }
        else if (confirmation != Ui.INSTALL)
        {
            return;
        }

        var msiFilePath = WaitForm.Go(
            this,
            "Software Update",
            "Downloading update...",
            out success,
            () =>
            {
                var dir = GetUpdateDir();
                var msiFilePath = Path.Combine(dir, $"SQLNotebook_{version}_{DateTime.Now.Ticks}.msi");
                using var downloadStream = Program.HttpClient.GetStreamAsync(msiUrl).GetAwaiter().GetResult();
                using var fileStream = File.Create(msiFilePath);
                downloadStream.CopyTo(fileStream);
                return msiFilePath;
            }
        );
        if (!success)
        {
            return;
        }

        Process.Start(new ProcessStartInfo { FileName = msiFilePath, UseShellExecute = true });
        Close();
    }

    private static void DeleteUpdateDir()
    {
        try
        {
            var dir = Path.Combine(Path.GetTempPath(), "SqlNotebookUpdate");
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
        catch { }
    }

    private static string GetUpdateDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "SqlNotebookUpdate");
        Directory.CreateDirectory(dir);
        foreach (var filePath in Directory.GetFiles(dir))
        {
            try
            {
                File.Delete(filePath);
            }
            catch { }
        }
        return dir;
    }

    private void ReleaseNotesMenu_Click(object sender, EventArgs e)
    {
        ShowReleaseNotes();
    }

    private static void ShowReleaseNotes() =>
        Process.Start(
            new ProcessStartInfo
            {
                FileName = "https://github.com/brianluft/sqlnotebook/releases",
                UseShellExecute = true,
            }
        );

    public sealed class UserLayout
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Dpi { get; set; }
        public int WindowState { get; set; }

        public int TableOfContentsDockState { get; set; }

        public DockState? GetTableOfContentsDockState()
        {
            return (DockState)TableOfContentsDockState switch
            {
                DockState.DockLeft
                or DockState.DockLeftAutoHide
                or DockState.DockRight
                or DockState.DockRightAutoHide => (DockState)TableOfContentsDockState,
                _ => null,
            };
        }

        public double TableOfContentsAutoHidePortion { get; set; }

        public double? GetTableOfContentsAutoHidePortion() =>
            TableOfContentsAutoHidePortion > 0 ? TableOfContentsAutoHidePortion : null;

        public double DockLeftPortion { get; set; }

        public double? GetDockLeftPortion() => DockLeftPortion > 0 ? DockLeftPortion : null;

        public double DockRightPortion { get; set; }

        public double? GetDockRightPortion() => DockRightPortion > 0 ? DockRightPortion : null;

        public double DockTopPortion { get; set; }

        public double? GetDockTopPortion() => DockTopPortion > 0 ? DockTopPortion : null;

        public double DockBottomPortion { get; set; }

        public double? GetDockBottomPortion() => DockBottomPortion > 0 ? DockBottomPortion : null;

        public int ConsoleDockState { get; set; }

        public DockState? GetConsoleDockState()
        {
            return (DockState)ConsoleDockState switch
            {
                DockState.DockBottom or DockState.DockBottomAutoHide => (DockState)ConsoleDockState,
                _ => null,
            };
        }

        public double ConsoleAutoHidePortion { get; set; }

        public double? GetConsoleAutoHidePortion() => ConsoleAutoHidePortion > 0 ? ConsoleAutoHidePortion : null;
    }

    private void SaveUserLayout(bool offset = false)
    {
        try
        {
            var offsetAmount = (int)(32 * (double)DeviceDpi / 96);
            var bounds = WindowState != FormWindowState.Normal ? RestoreBounds : Bounds;

            UserLayout layout = new()
            {
                Left = bounds.Left + (offset ? offsetAmount : 0),
                Top = bounds.Top + (offset ? offsetAmount : 0),
                Width = bounds.Width,
                Height = bounds.Height,
                Dpi = DeviceDpi,
                WindowState = (int)WindowState,
                TableOfContentsDockState = (int)_contentsDockContent.DockState,
                TableOfContentsAutoHidePortion = _contentsDockContent.AutoHidePortion,
                ConsoleDockState = (int)_consoleDockContent.DockState,
                ConsoleAutoHidePortion = _consoleDockContent.AutoHidePortion,
                DockLeftPortion = _dockPanel.DockLeftPortion,
                DockRightPortion = _dockPanel.DockRightPortion,
                DockTopPortion = _dockPanel.DockTopPortion,
                DockBottomPortion = _dockPanel.DockBottomPortion,
            };

            Settings.Default.MainFormLayout = JsonSerializer.Serialize(layout);
            Settings.Default.Save();
        }
        catch { }
    }

    private UserLayout RestoreUserLayout()
    {
        try
        {
            var x = JsonSerializer.Deserialize<UserLayout>(Settings.Default.MainFormLayout);
            var dpiRatio = (double)DeviceDpi / x.Dpi;
            var left = (int)(dpiRatio * x.Left);
            var top = (int)(dpiRatio * x.Top);
            var width = (int)(dpiRatio * x.Width);
            var height = (int)(dpiRatio * x.Height);

            var screen = Screen.PrimaryScreen;
            foreach (var potentialScreen in Screen.AllScreens)
            {
                if (potentialScreen.WorkingArea.Contains(new Point(left, top)))
                {
                    screen = potentialScreen;
                    break;
                }
            }

            var screenSize = screen.WorkingArea.Size;
            // Don't be bigger than whole screen
            width = Math.Min(width, screenSize.Width);
            height = Math.Min(height, screenSize.Height);

            // Don't go off the right or bottom of the screen
            left = Math.Min(left, screenSize.Width - width);
            top = Math.Min(top, screenSize.Height - height);

            // Don't go off the left or top of the screen
            left = Math.Max(left, screen.WorkingArea.Left);
            top = Math.Max(top, screen.WorkingArea.Top);

            Location = new(left, top);
            Size = new(width, height);
            var state = (FormWindowState)x.WindowState;
            if (state != FormWindowState.Minimized)
            {
                WindowState = state;
            }

            return x;
        }
        catch
        {
            return null;
        }
    }

    private void WindowOptionsMenu_Click(object sender, EventArgs e)
    {
        using OptionsForm f = new();
        f.ShowDialog(this);
        Focus();
    }

    private void HelpLicenseInformationMenu_Click(object sender, EventArgs e)
    {
        var filePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "ThirdPartyLicenses.html");
        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
    }

    private void FinishedTimer_Tick(object sender, EventArgs e)
    {
        var fadeTime = TimeSpan.FromSeconds(2);

        if (_finishedMenuFade.Elapsed >= fadeTime)
        {
            _finishedMenu.Visible = false;
            _finishedTimer.Stop();
        }

        var percent = _finishedMenuFade.Elapsed.TotalSeconds / fadeTime.TotalSeconds;

        // Instead of a linear fade, fade slowly at first and then rapidly at the end.
        percent = Math.Pow(percent, 3);

        var c = Color.Blue;
        int Scale(int x) => Math.Min(255, Math.Max(0, (int)((250 - x) * percent + x)));
        _finishedMenu.ForeColor = Color.FromArgb(Scale(c.R), Scale(c.G), Scale(c.B));
    }

    public void SetFinished(TimeSpan duration)
    {
        BeginInvoke(
            new(() =>
            {
                if (duration < TimeSpan.FromSeconds(1))
                {
                    _finishedMenu.Text = $"Finished in {duration.TotalMilliseconds:#,##0} ms";
                }
                else
                {
                    _finishedMenu.Text = $"Finished in {duration.TotalSeconds:#,##0.0} sec";
                }
                _finishedMenuFade = Stopwatch.StartNew();
                _finishedMenu.ForeColor = Color.Blue;
                _finishedMenu.Visible = true;
                _finishedTimer.Stop();
                _finishedTimer.Start();
            })
        );
    }

    private void FinishedMenu_Paint(object sender, PaintEventArgs e)
    {
        using SolidBrush bg = new(Color.FromArgb(250, 250, 250));
        e.Graphics.FillRectangle(bg, e.ClipRectangle);
        var size = e.Graphics.MeasureString(_finishedMenu.Text, _finishedMenu.Font);
        using SolidBrush fg = new(_finishedMenu.ForeColor);
        e.Graphics.DrawString(
            _finishedMenu.Text,
            _finishedMenu.Font,
            fg,
            new PointF(_finishedMenu.Width / 2 - size.Width / 2, _finishedMenu.Height / 2 - size.Height / 2)
        );
    }

    private void MainForm_DragEnter(object sender, DragEventArgs e)
    {
        e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void MainForm_DragDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            foreach (var filePath in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                if (File.Exists(filePath))
                {
                    try
                    {
                        FileImporter.Start(this, filePath, _manager);
                    }
                    catch (Exception ex)
                    {
                        Ui.ShowError(this, "Import Error", ex);
                    }
                    Focus();
                    return;
                }
            }
        }
    }
}
