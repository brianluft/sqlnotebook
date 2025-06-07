using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SqlNotebook.Properties;
using SqlNotebookScript.Utils;

namespace SqlNotebook;

public partial class ExplorerControl : UserControl
{
    private readonly NotebookManager _manager;
    private readonly IWin32Window _mainForm;
    private readonly TreeView _tree;

    private const int ICON_PAGE = 0;
    private const int ICON_SCRIPT = 1;
    private const int ICON_TABLE = 2;
    private const int ICON_VIEW = 3;
    private const int ICON_LINKED_TABLE = 4;
    private const int ICON_KEY = 5;
    private const int ICON_COLUMN = 6;

    public event EventHandler NewPageButtonClicked;
    public event EventHandler NewScriptButtonClicked;

    public ExplorerControl(NotebookManager manager, IWin32Window mainForm)
    {
        InitializeComponent();
        _mainForm = mainForm;
        _manager = manager;
        _manager.NotebookChange += (sender, e) => HandleNotebookChange(e);
        _contextMenuStrip.SetMenuAppearance();
        _toolStrip.SetMenuAppearance();
        _toolStrip.Padding = new(this.Scaled(1), 0, 0, 0);
        _toolStrip.BackColor = SystemColors.Window;

        ImageList imageList = new()
        {
            ColorDepth = ColorDepth.Depth32Bit,
            ImageSize = new(this.Scaled(16), this.Scaled(16)),
        };

        // Same order as the ICON_* constants.
        // ICON_PAGE
        imageList.Images.Add(Ui.GetScaledIcon(this, Resources.page, Resources.page32, dispose: false));
        // ICON_SCRIPT
        imageList.Images.Add(
            Ui.GetScaledIcon(this, Resources.script, Ui.ShiftImage(Resources.script32, 0, 1), dispose: false)
        );
        // ICON_TABLE
        imageList.Images.Add(Ui.GetScaledIcon(this, Resources.table, Resources.table32, dispose: false));
        // ICON_VIEW
        imageList.Images.Add(Ui.GetScaledIcon(this, Resources.filter, Resources.filter32, dispose: false));
        // ICON_LINKED_TABLE
        imageList.Images.Add(Ui.GetScaledIcon(this, Resources.link, Resources.link32, dispose: false));
        // ICON_KEY
        imageList.Images.Add(Ui.GetScaledIcon(this, Resources.key, Resources.key32, dispose: false));
        // ICON_COLUMN
        imageList.Images.Add(Ui.GetScaledIcon(this, Resources.bullet_white, Resources.bullet_white32, dispose: false));

        _tree = new ExplorerTreeView
        {
            BorderStyle = BorderStyle.None,
            ContextMenuStrip = _contextMenuStrip,
            Dock = DockStyle.Fill,
            FullRowSelect = true,
            LabelEdit = true,
            ShowRootLines = true,
        };
        _tree.ImageList = imageList;
        _tree.TreeViewNodeSorter = new ExplorerNodeComparer();
        _tree.AfterLabelEdit += Tree_AfterLabelEdit;
        _tree.BeforeExpand += Tree_BeforeExpand;
        _tree.BeforeLabelEdit += Tree_BeforeLabelEdit;
        _tree.KeyDown += Tree_KeyDown;
        _tree.NodeMouseClick += Tree_NodeMouseClick;
        _tree.NodeMouseDoubleClick += Tree_NodeMouseDoubleClick;
        _tree.EnableDoubleBuffering();
        _toolStripContainer.ContentPanel.Controls.Add(_tree);

        Ui ui = new(this, false);
        Padding buttonPadding = new(this.Scaled(2));
        var newPageImage16 = Ui.SuperimposePlusSymbol(Resources.page);
        var newPageImage32 = Ui.SuperimposePlusSymbol(Resources.page32);
        ui.Init(_toolbarNewPageButton, newPageImage16, newPageImage32);
        var newScriptImage16 = Ui.SuperimposePlusSymbol(Resources.script);
        var newScriptImage32 = Ui.SuperimposePlusSymbol(Ui.ShiftImage(Resources.script32, 0, 1));
        ui.Init(_toolbarNewScriptButton, newScriptImage16, newScriptImage32);

        _tree.GotFocus += (sender, e) =>
        {
            _manager.CommitOpenEditors();
            _manager.Rescan(notebookItemsOnly: true);
        };
    }

    private sealed class ExplorerNodeComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            var xNode = (TreeNode)x;
            var yNode = (TreeNode)y;
            if (xNode.Parent is null && yNode.Parent is null)
            {
                var xType = (NotebookItemType)xNode.Tag;
                var yType = (NotebookItemType)yNode.Tag;
                var result = GetOrder(xType).CompareTo(GetOrder(yType));
                if (result != 0)
                {
                    return result;
                }
            }
            return xNode.Text.CompareTo(yNode.Text);
        }

        private static int GetOrder(NotebookItemType t) =>
            t switch
            {
                NotebookItemType.Page => 0,
                NotebookItemType.Script => 1,
                NotebookItemType.Table => 2,
                NotebookItemType.View => 2,
                _ => throw new NotImplementedException(),
            };
    }

    public void TakeFocus()
    {
        _tree.Select();
    }

    private void HandleNotebookChange(NotebookChangeEventArgs e)
    {
        if (e.RemovedItems.Any() || e.AddedItems.Any())
        {
            BeginInvoke(
                new Action(() =>
                {
                    _tree.BeginUpdate();
                    try
                    {
                        foreach (var item in e.RemovedItems)
                        {
                            foreach (TreeNode node in _tree.Nodes)
                            {
                                if (node.Text == item.Name && (NotebookItemType)node.Tag == item.Type)
                                {
                                    node.Remove();
                                    break;
                                }
                            }
                        }
                        foreach (var item in e.AddedItems)
                        {
                            TreeNode child = new(item.Name);
                            child.Tag = item.Type;
                            _tree.Nodes.Add(child);
                            if (item.IsVirtualTable)
                            {
                                child.ImageIndex = ICON_LINKED_TABLE;
                            }
                            else
                            {
                                child.ImageIndex = item.Type switch
                                {
                                    NotebookItemType.Page => ICON_PAGE,
                                    NotebookItemType.Script => ICON_SCRIPT,
                                    NotebookItemType.Table => ICON_TABLE,
                                    NotebookItemType.View => ICON_VIEW,
                                    _ => throw new NotImplementedException(),
                                };
                            }
                            child.SelectedImageIndex = child.ImageIndex;

                            // For tables and views, we will show the columns as sub-nodes on-demand. For scripts, we will show
                            // the parameters. We have to create a child node here so that the [+] button shows up. We will
                            // replace it with the real items when the user expands the node.
                            if (item.Type is NotebookItemType.Script or NotebookItemType.Table or NotebookItemType.View)
                            {
                                child.Nodes.Add("Loading...");
                            }
                        }
                        _tree.Sort();
                    }
                    finally
                    {
                        _tree.EndUpdate();
                    }
                })
            );
        }
    }

    private void DeleteMnu_Click(object sender, EventArgs e)
    {
        DoDelete();
    }

    private void DoDelete()
    {
        if (_tree.SelectedNode == null || _tree.SelectedNode.Parent != null)
        {
            return;
        }
        var node = _tree.SelectedNode;
        var name = node.Text;
        var type = (NotebookItemType)node.Tag;

        var choice = Ui.ShowTaskDialog(
            _mainForm,
            $"Do you want to delete \"{name}\"?",
            "Delete Item",
            new[] { Ui.DELETE, Ui.CANCEL },
            TaskDialogIcon.Warning,
            defaultIsFirst: false
        );
        if (choice != Ui.DELETE)
        {
            return;
        }

        var item = new NotebookItem(type, name);
        _manager.CloseItem(item);

        WaitForm.Go(
            TopLevelControl,
            "Delete",
            "Deleting the selected item...",
            out _,
            () =>
            {
                _manager.DeleteItem(item);
            }
        );

        var isTableOrView = type == NotebookItemType.Table || type == NotebookItemType.View;
        _manager.Rescan(notebookItemsOnly: !isTableOrView);
    }

    private void ContextMenuStrip_Opening(object sender, CancelEventArgs e)
    {
        var isSelection = _tree.SelectedNode != null && _tree.SelectedNode.Parent == null;
        _openMnu.Enabled = _deleteMnu.Enabled = _renameMnu.Enabled = isSelection;
    }

    public sealed class Row
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    private void RenameMnu_Click(object sender, EventArgs e)
    {
        _tree.SelectedNode?.BeginEdit();
    }

    private void ToolbarNewPageButton_Click(object sender, EventArgs e)
    {
        NewPageButtonClicked?.Invoke(this, EventArgs.Empty);
    }

    private void ToolbarNewScriptButton_Click(object sender, EventArgs e)
    {
        NewScriptButtonClicked?.Invoke(this, EventArgs.Empty);
    }

    private void Tree_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
    {
        DoOpen(e.Node);
    }

    private void DoOpen(TreeNode node)
    {
        node ??= _tree.SelectedNode;
        if (node == null || node.Parent != null)
        {
            return;
        }
        var type = (NotebookItemType)node.Tag;
        _manager.OpenItem(new NotebookItem(type, node.Text));
    }

    private void Tree_KeyDown(object sender, KeyEventArgs e)
    {
        if (_tree.SelectedNode != null)
        {
            if (e.KeyData == Keys.F2)
            {
                _tree.SelectedNode.BeginEdit();
            }
            else if (e.KeyCode == Keys.Delete)
            {
                DoDelete();
            }
            else if (e.KeyData == Keys.Enter)
            {
                DoOpen(null);
            }
        }
    }

    private void Tree_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
    {
        e.CancelEdit = true; // if this is a successful edit, we will update the label ourselves
        var node = e.Node;
        var type = (NotebookItemType)node.Tag;

        if (e.Label == null)
        {
            return; // user did not change the name
        }

        try
        {
            _manager.RenameItem(new NotebookItem(type, node.Text), e.Label);
        }
        catch (Exception ex)
        {
            Ui.ShowError(TopLevelControl, "Rename Error", ex);
        }
    }

    private void Tree_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
    {
        // Don't allow renaming the child nodes.
        if (e.Node.Parent != null)
        {
            e.CancelEdit = true;
        }
    }

    private void Tree_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
    {
        // By default, right-clicking on a node doesn't select it.
        _tree.SelectedNode = e.Node;
    }

    private void OpenMnu_Click(object sender, EventArgs e)
    {
        DoOpen(null);
    }

    private void PopulateScriptParameters(TreeNode node)
    {
        node.Nodes.Clear();
        var paramNames = _manager.Notebook.UserData.GetScriptParameters(node.Text);
        if (paramNames != null)
        {
            foreach (var paramName in paramNames)
            {
                var child = node.Nodes.Add(paramName);
                child.ImageIndex = ICON_COLUMN;
                child.SelectedImageIndex = ICON_COLUMN;
            }
        }
        if (node.Nodes.Count == 0)
        {
            var child = node.Nodes.Add("(No parameters)");
            child.ImageIndex = ICON_COLUMN;
            child.SelectedImageIndex = ICON_COLUMN;
        }
    }

    private void PopulateTableColumns(TreeNode node)
    {
        node.Nodes.Clear();
        using var dt = _manager.Notebook.Query($"PRAGMA table_info ({node.Text.DoubleQuote()})", Array.Empty<object>());
        for (int i = 0; i < dt.Rows.Count; i++)
        {
            var name = (string)dt.Get(i, "name");
            var child = node.Nodes.Add(name);
            if (Convert.ToInt32(dt.Get(i, "pk")) != 0)
            {
                child.ImageIndex = ICON_KEY;
            }
            else
            {
                child.ImageIndex = ICON_COLUMN;
            }
            child.SelectedImageIndex = child.ImageIndex;
        }
    }

    private void Tree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
    {
        var type = (NotebookItemType)e.Node.Tag;

        switch (type)
        {
            case NotebookItemType.Script:
                PopulateScriptParameters(e.Node);
                break;

            case NotebookItemType.Table:
            case NotebookItemType.View:
                PopulateTableColumns(e.Node);
                break;
        }
    }

    // Don't expand or collapse tree nodes on double-click. It interferes with double-click to open.
    private sealed class ExplorerTreeView : TreeView
    {
        private bool _suppressExpandCollapse;
        private DateTime _lastClick;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _suppressExpandCollapse = (DateTime.Now - _lastClick).TotalMilliseconds < SystemInformation.DoubleClickTime;
            _lastClick = DateTime.Now;
            base.OnMouseDown(e);
        }

        protected override void OnBeforeCollapse(TreeViewCancelEventArgs e)
        {
            e.Cancel = _suppressExpandCollapse;
            base.OnBeforeCollapse(e);
        }

        protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
        {
            e.Cancel = _suppressExpandCollapse;
            base.OnBeforeExpand(e);
        }
    }
}
