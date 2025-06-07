using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SqlNotebook.Properties;

namespace SqlNotebook.Pages;

public sealed class DividerBlockControl : BlockControl
{
    private readonly ContextMenuStrip _addMenu;
    private readonly ToolStripMenuItem _addTextMenu;
    private readonly ToolStripMenuItem _addQueryMenu;

    public event EventHandler<AddBlockEventArgs> AddBlock;

    public DividerBlockControl()
    {
        _hover.Click += Hover_Click;

        _addMenu = new();
        _addMenu.SetMenuAppearance();
        _addMenu.Items.Add(_addTextMenu = new ToolStripMenuItem("Insert text here"));
        _addTextMenu.Click += AddTextMenu_Click;
        _addMenu.Items.Add(_addQueryMenu = new ToolStripMenuItem("Insert query here"));
        _addQueryMenu.Click += AddQueryMenu_Click;

        Ui ui = new(this, padded: false);
        var addTextIcon16 = Ui.SuperimposePlusSymbol(Resources.font);
        var addTextIcon32 = Ui.SuperimposePlusSymbol(Resources.font32);
        var addQueryIcon16 = Ui.SuperimposePlusSymbol(Resources.table);
        var addQueryIcon32 = Ui.SuperimposePlusSymbol(Resources.table32);
        ui.Init(_addTextMenu, addTextIcon16, addTextIcon32);
        ui.Init(_addQueryMenu, addQueryIcon16, addQueryIcon32);
    }

    private void AddTextMenu_Click(object sender, EventArgs e)
    {
        AddBlock?.Invoke(this, new(this, BlockType.Text));
    }

    private void AddQueryMenu_Click(object sender, EventArgs e)
    {
        AddBlock?.Invoke(this, new(this, BlockType.Query));
    }

    private void Hover_Click(object sender, EventArgs e)
    {
        _addMenu.Show(MousePosition);
    }

    private readonly StringFormat _stringFormat = new(StringFormatFlags.FitBlackBox | StringFormatFlags.NoWrap);

    protected override void OnPaint(Graphics g, UserOptions opt, Color[] colors, Color backColor)
    {
        var gridLineColor100 = colors[UserOptionsColor.GRID_LINES];
        var gridLineColor30 = Color.FromArgb(77, gridLineColor100);

        // "Add" button measurement
        var addText = "➕ Add block";
        var font = UserOptions.GetDefaultDataTableFont();
        var addTextSize = g.MeasureString(addText, font, PointF.Empty, _stringFormat).ToSize();

        // Draw the dotted horizontal line
        var size = ClientSize;
        var y = size.Height / 2;
        var padding = (int)(5 * (double)DeviceDpi / 96);
        using Pen pen = new(_hover.HoverOrDown ? gridLineColor100 : gridLineColor30, this.ScaledF(1))
        {
            DashStyle = DashStyle.Dot,
        };
        if (_hover.HoverOrDown)
        {
            g.DrawLine(pen, 0, y, size.Width / 2 - addTextSize.Width / 2 - padding, y);
            g.DrawLine(pen, size.Width / 2 + addTextSize.Width / 2 + padding, y, size.Width, y);
        }
        else
        {
            g.DrawLine(pen, 0, y, size.Width, y);
        }

        // Draw the "+ Add" button when hovering
        if (_hover.HoverOrDown)
        {
            Rectangle boxRect = new(
                size.Width / 2 - addTextSize.Width / 2 - padding,
                0,
                addTextSize.Width + padding * 2,
                size.Height
            );
            var verticalOffset = (size.Height - addTextSize.Height) / 2;

            using SolidBrush brush = new(colors[UserOptionsColor.GRID_PLAIN]);
            g.DrawString(
                addText,
                font,
                brush,
                new PointF(boxRect.Left + padding, boxRect.Top + verticalOffset),
                _stringFormat
            );
        }
    }

    public override int CalculateHeight()
    {
        return this.Scaled(30);
    }
}
