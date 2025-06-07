using System;
using System.Drawing;
using System.Windows.Forms;
using SqlNotebookScript;

namespace SqlNotebook.Pages;

public sealed class TextBlockControl : BlockControl
{
    private SqlTextControl _textBox;

    public string BlockText { get; set; } = "";

    public TextBlockControl()
    {
        _hover.Click += Hover_Click;
    }

    private readonly StringFormat _stringFormat = new(StringFormatFlags.FitBlackBox);

    public override int CalculateHeight()
    {
        if (EditMode)
        {
            return this.Scaled(250);
        }

        var opt = UserOptions.Instance;
        using var g = CreateGraphics();
        var codeFont = opt.GetCodeFont();
        var textWidth = ClientSize.Width - 2 * HorizontalMargin;
        var size = g.MeasureString(
                BlockText.Replace("\r", "").Replace("\t", "    "),
                codeFont,
                textWidth,
                _stringFormat
            )
            .ToSize();
        return Math.Max(this.Scaled(40), size.Height + 2 * VerticalMargin);
    }

    protected override void OnPaint(Graphics g, UserOptions opt, Color[] colors, Color backColor)
    {
        // User's text
        var codeFont = opt.GetCodeFont();
        var textWidth = ClientSize.Width - 2 * HorizontalMargin;
        using SolidBrush brush = new(colors[UserOptionsColor.GRID_PLAIN]);
        g.DrawString(
            BlockText.Replace("\r", "").Replace("\t", "    "),
            codeFont,
            brush,
            new RectangleF(HorizontalMargin, VerticalMargin, textWidth, ClientSize.Height),
            _stringFormat
        );
    }

    private void Hover_Click(object sender, EventArgs e)
    {
        StartEditing();
    }

    public override void StartEditing()
    {
        if (EditMode)
        {
            return;
        }

        EditMode = true;
        Cursor = Cursors.Default;

        var (acceptButton, table, panel) = CreateStandardEditModeLayout();
        _textBox = new(readOnly: false, syntaxColoring: false, wrap: true)
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            BorderStyle = BorderStyle.None,
            SqlText = BlockText,
        };
        panel.Controls.Add(_textBox);
        Controls.Add(table);
        _textBox.Focus();
        _textBox.SqlTextChanged += delegate
        {
            RaiseDirty();
        };
        _textBox.F10KeyPress += delegate
        {
            StopEditing();
        };

        acceptButton.Click += delegate
        {
            StopEditing();
        };

        Height = CalculateHeight();
        Invalidate(true);
        RaiseBlockClicked();
    }

    public override void StopEditing()
    {
        if (!EditMode)
        {
            return;
        }

        UpdatePropertiesFromEditMode();
        EditMode = false;
        Cursor = Cursors.Hand;
        for (var i = Controls.Count - 1; i >= 0; i--)
        {
            Controls.RemoveAt(i);
        }
        _textBox = null;
        Height = CalculateHeight();
        Invalidate(true);
        RaiseBlockClicked();
    }

    private void UpdatePropertiesFromEditMode() => BlockText = _textBox.SqlText;

    public void LoadFromRecord(TextPageBlockRecord record)
    {
        BlockText = record.Content;
        Height = CalculateHeight();
        Invalidate(true);
    }

    public TextPageBlockRecord SaveToRecord()
    {
        if (EditMode)
        {
            UpdatePropertiesFromEditMode();
        }
        return new() { Content = BlockText };
    }
}
