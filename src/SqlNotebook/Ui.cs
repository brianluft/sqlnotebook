using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SqlNotebookScript.Utils;

namespace SqlNotebook;

public sealed class Ui
{
    public const string OK = "OK";
    public const string CANCEL = "Cancel";
    public const string SAVE = "&Save";
    public const string DONT_SAVE = "Do&n't save";
    public const string DELETE = "&Delete";
    public const string INSTALL = "&Install";
    public const string RELEASE_NOTES = "&View release notes";

    private readonly float _scale;
    private readonly SizeF _x;
    private readonly Padding _buttonPadding;
    private readonly Padding _defaultMargin;
    private int _tabIndex = 1;

    private static readonly Font _consolas = new("Consolas", 11f);

    private Ui(Control control, bool padded, bool autoSize)
    {
        using var graphics = control.CreateGraphics();
        _x = graphics.MeasureString("x", control.Font, PointF.Empty, StringFormat.GenericTypographic);
        _scale = (float)control.DeviceDpi / 96;
        var buttonPaddingX = (int)(_x.Width * 3);
        var buttonPaddingY = (int)(_x.Height * 0.25);
        _buttonPadding = new(buttonPaddingX, buttonPaddingY, buttonPaddingX, buttonPaddingY);

        var labelMargin = (int)(_scale * 3);
        _defaultMargin = new(labelMargin, labelMargin, labelMargin, labelMargin);

        if (padded)
        {
            var formPaddingX = (int)(_x.Width * 2);
            var formPaddingY = (int)(_x.Height * 0.75);
            control.Padding = new(formPaddingX, formPaddingY, formPaddingX, formPaddingY);
        }

        if (autoSize && control is Form form)
        {
            form.AutoSize = true;
            form.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }
    }

    public Ui(Control control, bool padded = true)
        : this(control, padded, true) { }

    public Ui(Form form, double xWidth, double xHeight, bool padded = true)
        : this(form, padded, false)
    {
        Size size = new(XWidth(xWidth), XHeight(xHeight));
        Size maxSize = new(size.Width * 10, size.Height * 10);

        form.AutoSize = false;
        form.Size = size;
        form.MinimumSize = size;
        form.MaximumSize = maxSize;
        form.SizeGripStyle = SizeGripStyle.Show;
    }

    public Size XSize(double x, double y) => new(XWidth(x), XHeight(y));

    public int XWidth(double x) => (int)(_x.Width * x);

    public int XHeight(double y) => (int)(_x.Height * y);

    public static int XWidth(double x, Control control)
    {
        using var g = control.CreateGraphics();
        var size = g.MeasureString("x", control.Font, PointF.Empty, StringFormat.GenericTypographic);
        return (int)(size.Width * x);
    }

    public static int XHeight(double x, Control control)
    {
        using var g = control.CreateGraphics();
        var size = g.MeasureString("x", control.Font, PointF.Empty, StringFormat.GenericTypographic);
        return (int)(size.Height * x);
    }

    public int SizeGripHeight
    {
        get => (int)(16 * _scale);
    }

    public void Init(Label label)
    {
        label.TabIndex = _tabIndex++;
        label.Margin = new(0, _defaultMargin.Top, _defaultMargin.Right, _defaultMargin.Bottom);
    }

    public void InitHeader(Label label)
    {
        Init(label);
        label.BackColor = Color.FromArgb(0, 122, 204);
        label.ForeColor = Color.White;
        label.Padding = new(XWidth(0.6), XHeight(0.2), XWidth(0.5), XHeight(0.2));
        label.Margin = Padding.Empty;
        label.Dock = DockStyle.Fill;
    }

    public void Init(LinkLabel label)
    {
        Init((Label)label);
    }

    public void Init(Button button, int? width = null)
    {
        button.TabIndex = _tabIndex++;
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.Margin = _defaultMargin;
        button.Padding = _buttonPadding;
        if (width.HasValue)
        {
            button.AutoSizeMode = AutoSizeMode.GrowOnly;
            button.Width = XWidth(width.Value);
        }
    }

    public void Init(Button button, Image lodpi, Image hidpi)
    {
        Init(button);
        button.Image = GetScaledIcon(button, lodpi, hidpi, _scale);
    }

    public void Init(TableLayoutPanel table)
    {
        table.TabIndex = _tabIndex++;
        table.AutoSize = true;
        table.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        table.Margin = Padding.Empty;
    }

    public void Init(FlowLayoutPanel flow)
    {
        flow.TabIndex = _tabIndex++;
        flow.AutoSize = true;
        flow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        flow.Margin = Padding.Empty;
    }

    public void Init(Panel panel, double? width = null, double? height = null, bool margin = false)
    {
        if (!margin)
        {
            panel.Margin = Padding.Empty;
        }
        panel.TabIndex = _tabIndex++;
        if (width.HasValue && height.HasValue)
        {
            panel.Size = new(XWidth(width.Value), XHeight(height.Value));
        }
    }

    public void Init(CheckBox check)
    {
        check.TabIndex = _tabIndex++;
        check.AutoSize = true;
    }

    public void Init(RadioButton radio)
    {
        radio.TabIndex = _tabIndex++;
        radio.AutoSize = true;
    }

    public void Init(TextBox textbox, int width = 25)
    {
        textbox.AutoSize = true;
        textbox.TabIndex = _tabIndex++;
        textbox.Width = XWidth(width);
        textbox.Font = _consolas;
    }

    public void Init(ToolStripTextBox textbox, int width = 25)
    {
        textbox.AutoSize = false;
        textbox.Width = XWidth(width);
        textbox.Font = _consolas;
    }

    public void Init(ListView listView)
    {
        listView.TabIndex = _tabIndex++;
    }

    public void Init(TabControl tabs)
    {
        tabs.TabIndex = _tabIndex++;
        tabs.Padding = new(XWidth(2), XHeight(0.25));
        tabs.Margin = Padding.Empty;
    }

    public void Init(TabPage tab) { }

    public void Init(PropertyGrid grid)
    {
        grid.TabIndex = _tabIndex++;
    }

    public void Init(ToolStrip strip)
    {
        strip.AutoSize = true;
        strip.BackColor = Color.FromArgb(250, 250, 250);
        strip.Stretch = true;
        strip.GripStyle = ToolStripGripStyle.Hidden;
        strip.SetMenuAppearance();
    }

    public void Init(ToolStripButton button)
    {
        button.Padding = new((int)(2 * _scale), (int)(2 * _scale), (int)(2 * _scale), (int)(4 * _scale));
        button.AutoSize = true;
    }

    public void Init(ToolStripSeparator separator) { }

    public void Init(ToolStripButton button, Image lodpi, Image hidpi)
    {
        button.Image = GetScaledIcon(button, lodpi, hidpi, _scale);
        button.ImageScaling = ToolStripItemImageScaling.None;
        button.Padding = new((int)(2 * _scale), (int)(2 * _scale), (int)(2 * _scale), (int)(4 * _scale));
        button.AutoSize = true;
    }

    public void Init(ToolStripMenuItem item, Image lodpi, Image hidpi)
    {
        item.Image = GetScaledIcon(item, lodpi, hidpi, _scale);
        item.ImageScaling = ToolStripItemImageScaling.None;
    }

    public void Init(ToolStripDropDownButton item)
    {
        item.Padding = new((int)(2 * _scale), (int)(2 * _scale), (int)(2 * _scale), (int)(4 * _scale));
    }

    public void Init(ToolStripDropDownButton item, Image lodpi, Image hidpi)
    {
        item.Image = GetScaledIcon(item, lodpi, hidpi, _scale);
        item.ImageScaling = ToolStripItemImageScaling.None;
    }

    public static Image GetScaledIcon(Control owningControl, Image lodpi, Image hidpi, bool dispose = true) =>
        GetScaledIcon(owningControl, lodpi, hidpi, (float)owningControl.DeviceDpi / 96, dispose);

    private static Image GetScaledIcon(
        Component owningComponent,
        Image lodpi,
        Image hidpi,
        float scale,
        bool dispose = true
    )
    {
        Image image;
        if (scale <= 1)
        {
            image = lodpi;
        }
        else
        {
            var scaledWidth = 16 * scale;
            Bitmap bmp = new((int)Math.Ceiling(scaledWidth), (int)Math.Ceiling(scaledWidth));
            if (dispose)
            {
                owningComponent.Disposed += delegate
                {
                    bmp.Dispose();
                };
            }
            using (var bmpG = Graphics.FromImage(bmp))
            {
                bmpG.DrawImage(hidpi, new RectangleF(0, 0, scaledWidth, scaledWidth));
            }
            image = bmp;
        }

        return image;
    }

    public void Init(CueToolStripTextBox textbox, int width = 25)
    {
        textbox.Width = XWidth(width);
    }

    public void Init(StatusStrip strip)
    {
        strip.TabIndex = _tabIndex++;
    }

    public void Init(ToolStripProgressBar bar)
    {
        bar.AutoSize = false;
        bar.Width = XWidth(15);
    }

    public void Init(ProgressBar bar)
    {
        bar.AutoSize = false;
        bar.Width = XWidth(15);
        bar.TabIndex = _tabIndex++;
    }

    public void Init(NumericUpDown upd, int width = 10)
    {
        upd.TabIndex = _tabIndex++;
        upd.Font = _consolas;
        upd.Width = XWidth(width);
    }

    public void Init(ComboBox cmb, int width = 50)
    {
        cmb.Width = XWidth(width);
        cmb.TabIndex = _tabIndex++;
        if (cmb.DropDownStyle != ComboBoxStyle.DropDownList)
        {
            cmb.Font = _consolas;
        }
    }

    public void Init(DataGridViewColumn col, int width)
    {
        col.Width = XWidth(width);
    }

    public void Init(SplitContainer split, double dividerFraction)
    {
        split.Cursor = Cursors.Default;
        split.SplitterWidth = XWidth(1);
        if (split.Orientation == Orientation.Horizontal)
        {
            split.SplitterDistance = (int)(split.Height * dividerFraction);
        }
        else
        {
            split.SplitterDistance = (int)(split.Width * dividerFraction);
        }
        split.TabIndex = _tabIndex++;
    }

    public void Init(ListBox list, double width, double height)
    {
        Init(list);
        list.Size = new(XWidth(width), XHeight(height));
    }

    public void Init(ListBox list)
    {
        list.IntegralHeight = false;
    }

    public void Init(ToolStripLabel label) { }

    public void MarginTop(Control control, double height = 0.75)
    {
        var m = control.Margin;
        m.Top = XHeight(height);
        control.Margin = m;
    }

    public void MarginBottom(Control control, double height = 0.75)
    {
        var m = control.Margin;
        m.Bottom = XHeight(height);
        control.Margin = m;
    }

    public void MarginLeft(Control control, double width = 2)
    {
        var m = control.Margin;
        m.Left = XWidth(width);
        control.Margin = m;
    }

    public void MarginRight(Control control, double width = 2)
    {
        var m = control.Margin;
        m.Right = XWidth(width);
        control.Margin = m;
    }

    public void MarginRight(ToolStripItem item, double width = 2)
    {
        var m = item.Margin;
        m.Right = XWidth(width);
        item.Margin = m;
    }

    public void PadLeft(Control control, double width = 2)
    {
        var m = control.Padding;
        m.Left = XWidth(width);
        control.Padding = m;
    }

    public void PadRight(Control control, double width = 2)
    {
        var m = control.Padding;
        m.Right = XWidth(width);
        control.Padding = m;
    }

    public void Pad(Control control)
    {
        var x = XWidth(1);
        var y = XHeight(0.375);
        control.Padding = new(x, y, x, y);
    }

    public void PadBig(Control control)
    {
        // same as form padding
        var x = XWidth(2);
        var y = XHeight(0.75);
        control.Padding = new(x, y, x, y);
    }

    public static string ShowTaskDialog(
        IWin32Window owner,
        string heading,
        string caption,
        string[] buttons,
        TaskDialogIcon icon = null,
        bool defaultIsFirst = true,
        string details = null
    )
    {
        TaskDialogPage taskDialogPage = new()
        {
            Heading = heading,
            Caption = caption,
            Icon = icon,
        };
        if (details != null)
        {
            taskDialogPage.Text = details;
        }
        foreach (var button in buttons)
        {
            taskDialogPage.Buttons.Add(button);
        }
        taskDialogPage.DefaultButton = taskDialogPage.Buttons[defaultIsFirst ? 0 : buttons.Length - 1];
        TaskDialogButton result;
        if (owner == null)
        {
            result = TaskDialog.ShowDialog(taskDialogPage);
        }
        else
        {
            result = TaskDialog.ShowDialog(owner, taskDialogPage);
        }
        return result.Text;
    }

    public static void ShowError(IWin32Window owner, string title, string message, string details = null)
    {
        TaskDialogPage taskDialogPage = new()
        {
            Heading = message,
            Caption = title,
            Icon = TaskDialogIcon.Error,
        };
        if (details != null)
        {
            taskDialogPage.Text = details;
        }
        taskDialogPage.Buttons.Add("OK");
        taskDialogPage.DefaultButton = taskDialogPage.Buttons[0];
        if (owner == null)
        {
            TaskDialog.ShowDialog(taskDialogPage);
        }
        else
        {
            TaskDialog.ShowDialog(owner, taskDialogPage);
        }
    }

    public static void ShowError(IWin32Window owner, string title, Exception exception)
    {
        var extended = exception as ExceptionEx;
        var message = exception.GetExceptionMessage();
        string details = null;
        if (message.Contains("\r\n\r\n"))
        {
            var parts = message.Split("\r\n\r\n", 2);
            message = parts[0];
            details = parts[1];
        }
        if (extended != null)
        {
            details = extended.Details;
        }

        TaskDialogPage taskDialogPage = new()
        {
            Heading = extended?.Heading ?? message,
            Caption = title,
            Icon = TaskDialogIcon.Error,
        };
        if (details != null)
        {
            taskDialogPage.Text = details;
        }

        taskDialogPage.Buttons.Add("OK");
        taskDialogPage.DefaultButton = taskDialogPage.Buttons[0];
        if (owner == null)
        {
            TaskDialog.ShowDialog(taskDialogPage);
        }
        else
        {
            TaskDialog.ShowDialog(owner, taskDialogPage);
        }
    }

    public static Bitmap ShiftImage(Bitmap source, int xOffset, int yOffset)
    {
        Bitmap shifted = new(source.Width, source.Height);
        using var g = Graphics.FromImage(shifted);
        g.DrawImageUnscaled(source, xOffset, yOffset);
        return shifted;
    }

    public static Bitmap SuperimposePlusSymbol(Bitmap source)
    {
        Debug.Assert(source.Width == 16 || source.Width == 32);
        Bitmap composite = new(source);
        using var g = Graphics.FromImage(composite);

        var w = source.Width;
        var offset = (int)(0.3 * w);
        var thickness = (int)(0.1 * w);
        if (thickness < 1 || (thickness % 2) != 0)
        {
            thickness++;
        }
        var barLength = w - 2 * offset;
        var centeringOffset = w / 2 - thickness / 2;
        var padding = thickness / 2;
        var diagonalAlignmentOffset = 0;

        Rectangle verticalRect = new(
            x: diagonalAlignmentOffset + centeringOffset,
            y: diagonalAlignmentOffset + offset,
            width: thickness,
            height: barLength
        );

        Rectangle horizontalRect = new(
            x: diagonalAlignmentOffset + offset,
            y: diagonalAlignmentOffset + centeringOffset,
            width: barLength,
            height: thickness
        );

        var verticalRectExpanded = verticalRect;
        var horizontalRectExpanded = horizontalRect;
        verticalRectExpanded.Inflate(padding, padding);
        horizontalRectExpanded.Inflate(padding, padding);
        g.FillRectangle(Brushes.WhiteSmoke, verticalRectExpanded);
        g.FillRectangle(Brushes.WhiteSmoke, horizontalRectExpanded);

        g.FillRectangle(Brushes.Green, verticalRect);
        g.FillRectangle(Brushes.Green, horizontalRect);

        return composite;
    }
}
