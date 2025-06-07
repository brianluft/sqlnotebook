using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SqlNotebook.Pages;

public abstract class BlockControl : UserControl
{
    private const int HORIZONTAL_MARGIN_UNSCALED = 10;
    private const int VERTICAL_MARGIN_UNSCALED = 5;

    protected readonly ToolTip _tooltip = new();
    protected readonly MouseHover _hover;
    public bool EditMode { get; protected set; }

    protected int HorizontalMargin => this.Scaled(HORIZONTAL_MARGIN_UNSCALED);
    protected int VerticalMargin => this.Scaled(VERTICAL_MARGIN_UNSCALED);

    public BlockControl()
    {
        _hover = new(this);
        Cursor = Cursors.Hand;
        ResizeRedraw = true;
        this.EnableDoubleBuffering();
        UserOptions.OnUpdate(this, Invalidate);
        Disposed += delegate
        {
            _tooltip.Dispose();
        };
    }

    public abstract int CalculateHeight();

    public delegate void BlockDeletedEventHandler(BlockControl block);
    public event BlockDeletedEventHandler BlockDeleted;

    protected void RaiseBlockDeleted()
    {
        BlockDeleted?.Invoke(this);
    }

    public delegate void BlockMovedEventHandler(BlockControl block, bool up);
    public event BlockMovedEventHandler BlockMoved;

    protected void RaiseBlockMoved(bool up)
    {
        BlockMoved?.Invoke(this, up);
    }

    public event EventHandler BlockClicked;

    protected void RaiseBlockClicked()
    {
        BlockClicked?.Invoke(this, EventArgs.Empty);
    }

    public virtual void StartEditing() { }

    public virtual void StopEditing() { }

    public event EventHandler Dirty;

    protected void RaiseDirty()
    {
        Dirty?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
        var opt = UserOptions.Instance;
        var colors = opt.GetColors();

        if (EditMode)
        {
            using SolidBrush editModeBg = new(colors[UserOptionsColor.GRID_BACKGROUND]);
            e.Graphics.FillRectangle(editModeBg, 0, 0, Width, Height);
            return;
        }

        var gridLineColor100 = colors[UserOptionsColor.GRID_LINES];
        var gridLineColor30 = Color.FromArgb(77, gridLineColor100);
        var gridLineColor5 = Color.FromArgb(13, gridLineColor100);

        // Background
        var backgroundColor = _hover.State switch
        {
            MouseHoverState.Down => gridLineColor30,
            MouseHoverState.Hover => gridLineColor5,
            _ => colors[UserOptionsColor.GRID_BACKGROUND],
        };
        using SolidBrush backgroundBrush = new(backgroundColor);
        e.Graphics.FillRectangle(backgroundBrush, e.ClipRectangle);

        OnPaint(e.Graphics, opt, colors, backgroundColor);
    }

    protected abstract void OnPaint(Graphics g, UserOptions opt, Color[] colors, Color backColor);

    protected (Button Accept, TableLayoutPanel table, Panel panel) CreateStandardEditModeLayout()
    {
        Button upButton = new()
        {
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Margin = new(this.Scaled(2), 0, 0, this.Scaled(2)),
            Text = "▲",
            Padding = Padding.Empty,
            BackColor = Color.WhiteSmoke,
            Cursor = Cursors.Hand,
        };
        upButton.FlatAppearance.BorderSize = 0;
        upButton.Click += delegate
        {
            RaiseBlockMoved(true);
        };

        Button downButton = new()
        {
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Margin = new(this.Scaled(2), 0, 0, this.Scaled(2)),
            Text = "▼",
            Padding = Padding.Empty,
            BackColor = Color.WhiteSmoke,
            Cursor = Cursors.Hand,
        };
        downButton.FlatAppearance.BorderSize = 0;
        downButton.Click += delegate
        {
            RaiseBlockMoved(false);
        };

        Button acceptButton = new()
        {
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Margin = new(this.Scaled(2), 0, 0, this.Scaled(2)),
            Text = "✔️",
            BackColor = Color.AliceBlue,
            Cursor = Cursors.Hand,
        };
        acceptButton.FlatAppearance.BorderSize = 0;

        Button deleteButton = new()
        {
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Margin = new(this.Scaled(2), 0, 0, 0),
            Text = "❌",
            BackColor = Color.WhiteSmoke,
            Cursor = Cursors.Hand,
        };
        deleteButton.FlatAppearance.BorderSize = 0;
        deleteButton.Click += delegate
        {
            var choice = Ui.ShowTaskDialog(
                TopLevelControl,
                "Do you want to delete this block?",
                "Page",
                new[] { Ui.DELETE, Ui.CANCEL },
                TaskDialogIcon.Warning,
                defaultIsFirst: false
            );
            if (choice == Ui.DELETE)
            {
                RaiseBlockDeleted();
            }
        };

        TableLayoutPanel table = new() { Dock = DockStyle.Fill };

        table.RowStyles.Add(new(SizeType.Absolute, this.Scaled(25)));
        table.RowStyles.Add(new(SizeType.Absolute, this.Scaled(25)));
        table.RowStyles.Add(new(SizeType.Percent, 100));
        table.RowStyles.Add(new(SizeType.Absolute, this.Scaled(25)));
        table.ColumnStyles.Add(new(SizeType.Absolute, this.Scaled(40)));
        table.ColumnStyles.Add(new(SizeType.Percent, 100));

        table.Controls.Add(upButton);
        table.SetRow(upButton, 0);
        table.SetColumn(upButton, 0);

        table.Controls.Add(downButton);
        table.SetRow(downButton, 1);
        table.SetColumn(downButton, 0);

        table.Controls.Add(acceptButton);
        table.SetRow(acceptButton, 2);
        table.SetColumn(acceptButton, 0);

        table.Controls.Add(deleteButton);
        table.SetRow(deleteButton, 3);
        table.SetColumn(deleteButton, 0);

        Panel panel = new()
        {
            Dock = DockStyle.Fill,
            Margin = new(this.Scaled(2), 0, this.Scaled(5), 0),
            Padding = Padding.Empty,
        };
        table.Controls.Add(panel);
        table.SetRow(panel, 0);
        table.SetColumn(panel, 1);
        table.SetRowSpan(panel, 4);

        _tooltip.SetToolTip(acceptButton, "Accept changes to this block (F10)");
        _tooltip.SetToolTip(upButton, "Move this block up");
        _tooltip.SetToolTip(downButton, "Move this block down");
        _tooltip.SetToolTip(deleteButton, "Remove this block from the page");

        return (acceptButton, table, panel);
    }
}
