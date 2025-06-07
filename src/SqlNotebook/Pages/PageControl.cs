using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SqlNotebook.Properties;
using SqlNotebookScript;

namespace SqlNotebook.Pages;

public sealed class PageControl : UserControl, IDocumentControl
{
    private readonly NotebookManager _manager;
    private readonly IWin32Window _mainForm;

    private readonly ToolStrip _toolStrip;
    private readonly ToolStripButton _executeAllButton;
    private readonly ToolStripButton _acceptAllButton;
    private readonly ToolStripButton _addQueryButton;
    private readonly ToolStripButton _addTextButton;
    private readonly Panel _scrollPanel;
    private readonly FlowLayoutPanel _flow;

    public PageControl(string name, NotebookManager manager, IWin32Window mainForm)
    {
        ItemName = name;
        _manager = manager;
        _mainForm = mainForm;

        Controls.Add(_scrollPanel = new() { Dock = DockStyle.Fill, AutoScroll = true });
        _scrollPanel.Controls.Add(
            _flow = new()
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = Padding.Empty,
            }
        );

        Controls.Add(_toolStrip = new() { Dock = DockStyle.Top });
        _toolStrip.Items.Add(_executeAllButton = new ToolStripButton { Text = "Execute all" });
        _executeAllButton.Click += ExecuteAllButton_Click;
        _toolStrip.Items.Add(_acceptAllButton = new ToolStripButton { Text = "Accept all", Enabled = false });
        _acceptAllButton.Click += AcceptAllButton_Click;
        var separator = new ToolStripSeparator();
        _toolStrip.Items.Add(separator);
        _toolStrip.Items.Add(_addTextButton = new ToolStripButton { Text = "Add text" });
        _addTextButton.Click += AddTextButton_Click;
        _toolStrip.Items.Add(_addQueryButton = new ToolStripButton { Text = "Add query" });
        _addQueryButton.Click += AddQueryButton_Click;
        _toolStrip.SetMenuAppearance();

        DividerBlockControl divider = new();
        divider.AddBlock += Divider_AddPart;
        InsertBlock(divider, 0);

        Ui ui = new(this, padded: false);
        ui.Init(_toolStrip);
        ui.Init(_executeAllButton, Resources.ControlPlayBlue, Resources.control_play_blue32);
        ui.Init(_acceptAllButton, Resources.accept_button, Resources.accept_button32);
        ui.Init(separator);
        var addTextIcon16 = Ui.SuperimposePlusSymbol(Resources.font);
        var addTextIcon32 = Ui.SuperimposePlusSymbol(Resources.font32);
        var addQueryIcon16 = Ui.SuperimposePlusSymbol(Resources.table);
        var addQueryIcon32 = Ui.SuperimposePlusSymbol(Resources.table32);
        ui.Init(_addTextButton, addTextIcon16, addTextIcon32);
        ui.Init(_addQueryButton, addQueryIcon16, addQueryIcon32);
        ui.Init(_scrollPanel);
        ui.Init(_flow);

        UserOptions.OnUpdateAndNow(
            this,
            () =>
            {
                var opt = UserOptions.Instance;
                var colors = opt.GetColors();
                BackColor = colors[UserOptionsColor.GRID_BACKGROUND];
            }
        );

        UserOptions.OnUpdatePost(
            this,
            () =>
            {
                OnSizeChanged(EventArgs.Empty);
            }
        );

        OnSizeChanged(EventArgs.Empty);

        LoadPage();
    }

    private void AddQueryButton_Click(object sender, EventArgs e)
    {
        QueryBlockControl block = new(_manager);
        AppendBlockAndDivider(block);
    }

    private void AddTextButton_Click(object sender, EventArgs e)
    {
        TextBlockControl block = new();
        AppendBlockAndDivider(block);
    }

    private void Divider_AddPart(object sender, AddBlockEventArgs e)
    {
        BlockControl block = e.Type switch
        {
            BlockType.Query => new QueryBlockControl(_manager),
            BlockType.Text => new TextBlockControl(),
            _ => throw new NotImplementedException(),
        };
        var dividerPartIndex = GetPartIndex(e.Divider);
        InsertBlockAndDivider(block, dividerPartIndex);
    }

    private void AppendBlockAndDivider(BlockControl block)
    {
        var dividerPartIndex = _flow.Controls.Count - 1;
        InsertBlockAndDivider(block, dividerPartIndex);
    }

    private void InsertBlockAndDivider(BlockControl block, int dividerPartIndex)
    {
        InsertBlock(block, dividerPartIndex + 1);

        DividerBlockControl bottomDivider = new();
        bottomDivider.AddBlock += Divider_AddPart;
        InsertBlock(bottomDivider, dividerPartIndex + 2);

        block.StartEditing();
    }

    private int GetPartIndex(BlockControl part) => _flow.Controls.GetChildIndex(part);

    public string ItemName { get; set; }

    private void LoadPage()
    {
        try
        {
            var index = 1; // Start at 1 because the top divider is already in place.

            var pageRecord = (PageNotebookItemRecord)_manager.GetItemData(ItemName);
            if (pageRecord.Blocks.Count == 0)
            {
                // Empty page. By default let's add a query.
                QueryBlockControl queryBlock = new(_manager);
                InsertBlock(queryBlock, index++);

                DividerBlockControl bottomDivider = new();
                bottomDivider.AddBlock += Divider_AddPart;
                InsertBlock(bottomDivider, index++);

                queryBlock.StartEditing();
                BeginInvoke(
                    new Action(() =>
                    {
                        queryBlock.QueryControl.TextControl.SqlFocus();
                    })
                );

                return;
            }

            foreach (var block in pageRecord.Blocks)
            {
                BlockControl blockControl;
                switch (block)
                {
                    case TextPageBlockRecord textBlock:
                        TextBlockControl textBlockControl = new();
                        textBlockControl.LoadFromRecord(textBlock);
                        blockControl = textBlockControl;
                        break;

                    case QueryPageBlockRecord queryBlock:
                        QueryBlockControl queryBlockControl = new(_manager);
                        queryBlockControl.LoadFromRecord(queryBlock);
                        blockControl = queryBlockControl;
                        break;

                    default:
                        throw new NotImplementedException();
                }

                InsertBlock(blockControl, index++);

                DividerBlockControl bottomDivider = new();
                bottomDivider.AddBlock += Divider_AddPart;
                InsertBlock(bottomDivider, index++);
            }

            OnSizeChanged(EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Ui.ShowError(TopLevelControl, "Page Error", ex);
        }
    }

    public void Save()
    {
        var blockControls = _flow
            .Controls.Cast<BlockControl>()
            .Where(x => x is TextBlockControl || x is QueryBlockControl)
            .ToList();

        PageNotebookItemRecord pageRecord = new() { Name = ItemName, Blocks = new() };
        foreach (var control in blockControls)
        {
            PageBlockRecord blockRecord = control switch
            {
                TextBlockControl textBlock => textBlock.SaveToRecord(),
                QueryBlockControl queryBlock => queryBlock.SaveToRecord(),
                _ => throw new NotImplementedException(),
            };
            pageRecord.Blocks.Add(blockRecord);
        }

        _manager.SetItemData(ItemName, pageRecord);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        _scrollPanel.HorizontalScroll.Visible = false;
        _scrollPanel.HorizontalScroll.Enabled = false;
        var w = _scrollPanel.ClientSize.Width;
        foreach (BlockControl control in _flow.Controls)
        {
            control.Width = w;
            control.Height = control.CalculateHeight();
        }
    }

    public void InsertBlock(BlockControl block, int index)
    {
        block.AutoSize = false;
        block.Margin = Padding.Empty;
        block.Size = new(_scrollPanel.ClientSize.Width, block.CalculateHeight());
        block.BlockDeleted += DeletePart;
        _flow.Controls.Add(block);
        _flow.Controls.SetChildIndex(block, index);
        OnSizeChanged(EventArgs.Empty);

        block.BlockClicked += Block_BlockClicked;
        block.BlockMoved += Block_BlockMoved;
        block.Dirty += Block_Dirty;
    }

    private void Block_Dirty(object sender, EventArgs e)
    {
        _manager.SetDirty();
    }

    private void Block_BlockClicked(object sender, EventArgs e)
    {
        OnSizeChanged(EventArgs.Empty);
        _acceptAllButton.Enabled = _flow.Controls.Cast<BlockControl>().Any(x => x.EditMode);
        TopLevelControl?.Focus();
    }

    private void Block_BlockMoved(BlockControl block, bool up)
    {
        var controlIndex = _flow.Controls.IndexOf(block);
        if (up)
        {
            // Topmost is index 1, second is index 3, etc. because of the dividers in between each one..
            if (controlIndex >= 3)
            {
                var partner = _flow.Controls[controlIndex - 2];
                _flow.Controls.SetChildIndex(block, controlIndex - 2);
                _flow.Controls.SetChildIndex(partner, controlIndex);
            }
        }
        else
        {
            var lastControlIndex = _flow.Controls.Count - 2; // skip the last divider
            if (controlIndex < lastControlIndex)
            {
                var partner = _flow.Controls[controlIndex + 2];
                _flow.Controls.SetChildIndex(partner, controlIndex);
                _flow.Controls.SetChildIndex(block, controlIndex + 2);
            }
        }
    }

    public void DeletePart(BlockControl part)
    {
        var index = _flow.Controls.IndexOf(part);
        _flow.Controls.RemoveAt(index); // the part itself
        _flow.Controls.RemoveAt(index); // the divider after the part
        OnSizeChanged(EventArgs.Empty);
    }

    private void ExecuteAllButton_Click(object sender, EventArgs e)
    {
        AcceptAll();

        _manager.CommitOpenEditors();

        List<Action> uiThreadActions = new();
        var queryBlockControls = _flow.Controls.OfType<QueryBlockControl>().ToList();
        WaitForm.Go(
            TopLevelControl,
            "Page Execution",
            "Executing all queries...",
            out _,
            () =>
            {
                foreach (var queryBlockControl in queryBlockControls)
                {
                    var output = queryBlockControl.ExecuteOnWorkerThread();
                    uiThreadActions.Add(() =>
                    {
                        queryBlockControl.Output?.Dispose();
                        queryBlockControl.Output = output;
                        queryBlockControl.Invalidate();
                    });
                }
            }
        );

        foreach (var action in uiThreadActions)
        {
            action();
        }
        OnSizeChanged(EventArgs.Empty);

        _manager.SetDirty();
        _manager.Rescan();
    }

    private void AcceptAllButton_Click(object sender, EventArgs e)
    {
        AcceptAll();
    }

    private void AcceptAll()
    {
        foreach (BlockControl control in _flow.Controls)
        {
            control.StopEditing();
        }
        _acceptAllButton.Enabled = false;
    }
}
