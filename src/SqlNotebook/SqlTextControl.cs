using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScintillaNET;
using SqlNotebookScript;
using SqlNotebookScript.Core;

namespace SqlNotebook;

public partial class SqlTextControl : UserControl
{
    private static readonly UTF8Encoding _utf8Encoding = new(false);

    private static readonly IReadOnlySet<TokenType> _operatorTokens = new HashSet<TokenType>(
        new[]
        {
            TokenType.Add,
            TokenType.Asterisk,
            TokenType.Bitand,
            TokenType.Bitnot,
            TokenType.Bitor,
            TokenType.Comma,
            TokenType.Dot,
            TokenType.Eq,
            TokenType.Ge,
            TokenType.Gt,
            TokenType.Le,
            TokenType.Lp,
            TokenType.Lshift,
            TokenType.Lt,
            TokenType.Ne,
            TokenType.Plus,
            TokenType.Rp,
            TokenType.Rshift,
            TokenType.Semi,
            TokenType.Star,
            TokenType.Uminus,
            TokenType.Uplus,
        }
    );

    private static readonly IReadOnlySet<string> _sqlnbKeywords = new HashSet<string>(
        new[]
        {
            "declare",
            "parameter",
            "while",
            "break",
            "continue",
            "print",
            "execute",
            "exec",
            "return",
            "throw",
            "try",
            "catch",
            "import",
            "csv",
            "options",
            "text",
            "integer",
            "real",
            "date",
            "datetime",
            "table",
            "txt",
        }
    );

    private static readonly Regex _whitespaceRegex = new(@"^\s*", RegexOptions.Compiled);
    private readonly Scintilla _scintilla;
    private readonly Margin _lineNumberMargin;
    private readonly bool _syntaxColoring;
    private float _digitWidth;

    public event EventHandler F5KeyPress;
    public event EventHandler F10KeyPress;

    public enum ScrollbarVisibility
    {
        Auto,
        Hide,
        Show,
    }

    public string SqlText
    {
        get { return _scintilla.Text; }
        set
        {
            // scintilla won't let us update the text programmatically if it is set to read-only
            var oldReadOnly = _scintilla.ReadOnly;
            _scintilla.ReadOnly = false;
            _scintilla.Text = value;
            _scintilla.ReadOnly = oldReadOnly;
        }
    }

    public void SetFont(Font font)
    {
        using var g = CreateGraphics();
        _digitWidth = g.MeasureString("9", font, PointF.Empty, StringFormat.GenericTypographic).Width;

        foreach (var style in _scintilla.Styles)
        {
            style.Font = font.FontFamily.Name;
            style.Size = (int)font.SizeInPoints;
        }
    }

    public event EventHandler SqlTextChanged;

    public void SqlFocus()
    {
        _scintilla.Focus();
    }

    public void SqlSelectAll()
    {
        _scintilla.SelectAll();
    }

    public SqlTextControl(bool readOnly, bool syntaxColoring = true, bool wrap = true)
    {
        InitializeComponent();
        _syntaxColoring = syntaxColoring;

        _scintilla = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = readOnly,
            BorderStyle = BorderStyle.None,
            WrapMode = wrap ? WrapMode.Word : WrapMode.None,
            Lexer = Lexer.Container,
            FontQuality = FontQuality.LcdOptimized,
            IndentWidth = 4,
            UseTabs = false,
            BufferedDraw = true,
            TabWidth = 4,
            ScrollWidthTracking = true,
            ScrollWidth = 1,
            VScrollBar = true,
            HScrollBar = true,
        };
        var lineNumberMarginWidth = 50;
        _lineNumberMargin = new Margin(_scintilla, 0) { Type = MarginType.Number, Width = lineNumberMarginWidth };
        _ = new Margin(_scintilla, 1) { Type = MarginType.BackColor, Width = this.Scaled(12) };
        void SetLineNumberMarginWidth()
        {
            var numDigits = (int)Math.Log10(_scintilla.Lines.Count) + 1;
            var width = (int)((numDigits + 2) * _digitWidth);
            if (width != lineNumberMarginWidth)
            {
                lineNumberMarginWidth = width;
                _lineNumberMargin.Width = width;
            }
        }
        _scintilla.InsertCheck += Scintilla_InsertCheck;
        _scintilla.StyleNeeded += Scintilla_StyleNeeded;
        _scintilla.TextChanged += (sender, e) =>
        {
            SqlTextChanged?.Invoke(this, EventArgs.Empty);
            SetLineNumberMarginWidth();
        };
        _scintilla.KeyDown += (sender, e) =>
        {
            if (e.KeyCode == Keys.F5 && e.Modifiers == Keys.None)
            {
                F5KeyPress?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F10 && e.Modifiers == Keys.None)
            {
                F10KeyPress?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z && e.Modifiers.HasFlag(Keys.Control))
            {
                // Avoid insertion of nonprintable control characters, which Scintilla supports but is not desired.
                // This can happen if the user types Ctrl + any letter key that isn't an enabled menu accelerator.
                // But we do need Scintilla to handle a few text editing hotkeys.
                switch (e.KeyCode)
                {
                    case Keys.A: // Select All
                    case Keys.C: // Copy
                    case Keys.X: // Cut
                    case Keys.V: // Paste
                    case Keys.Z: // Undo
                    case Keys.Y: // Redo
                        break;

                    default:
                        e.SuppressKeyPress = true;
                        e.Handled = true;
                        break;
                }
            }
        };

        Controls.Add(_scintilla);

        UserOptions.OnUpdateAndNow(
            this,
            () =>
            {
                SetFont(UserOptions.Instance.GetCodeFont());
                SetupSyntaxColoring();
                SetLineNumberMarginWidth();
            }
        );
    }

    // Auto-indent
    private void Scintilla_InsertCheck(object sender, InsertCheckEventArgs e)
    {
        switch (e.Text[^1])
        {
            case '\r':
            case '\n':
                // Get the current line of text
                int lineIndex = _scintilla.LineFromPosition(_scintilla.CurrentPosition);
                var lineStartPos = _scintilla.Lines[lineIndex].Position;
                var lineText = _scintilla.GetTextRange(lineStartPos, e.Position - lineStartPos);

                // Grab the whitespace from the beginning of the line and add it to the text about to be inserted, after
                // the newline.
                e.Text += _whitespaceRegex.Match(lineText).Value;
                break;
        }
    }

    public void SetVerticalScrollbarVisibility(ScrollbarVisibility visibility)
    {
        // Scintilla doesn't have an "always show" mode, so treat Show as Auto.
        _scintilla.VScrollBar = visibility != ScrollbarVisibility.Hide;
    }

    public void SetHorizontalScrollbarVisibility(ScrollbarVisibility visibility)
    {
        // Scintilla doesn't have an "always show" mode, so treat Show as Auto.
        _scintilla.HScrollBar = visibility != ScrollbarVisibility.Hide;
    }

    private void Scintilla_StyleNeeded(object sender, StyleNeededEventArgs e)
    {
        if (!_syntaxColoring)
        {
            return;
        }

        var text = _scintilla.Text;
        Task.Run(() =>
        {
            var tokens = Notebook.Tokenize(text);
            ulong i = 0;
            var utf8 = _utf8Encoding.GetBytes(text);
            List<Tuple<int, int>> list = new(); // length, type; for successive calls to SetStyling()

            foreach (var token in tokens)
            {
                // treat characters between i and token.Utf8Start as a comment
                var numCommentBytes = token.Utf8Start - i;
                var strComment = Encoding.UTF8.GetString(utf8, (int)i, (int)numCommentBytes);
                list.Add(Tuple.Create(strComment.Length, Style.Sql.Comment));
                i += numCommentBytes;

                var strSpace = Encoding.UTF8.GetString(utf8, (int)i, (int)token.Utf8Length);
                int type = 0;
                if (_operatorTokens.Contains(token.Type))
                {
                    type = Style.Sql.Operator;
                }
                else
                {
                    switch (token.Type)
                    {
                        case TokenType.Space:
                        case TokenType.Span:
                        case TokenType.Illegal:
                        case TokenType.Column:
                        case TokenType.Table:
                            type = Style.Sql.Default;
                            break;

                        case TokenType.String:
                            type = Style.Sql.String;
                            break;

                        case TokenType.Id:
                            if (_sqlnbKeywords.Contains(token.Text.ToLower()))
                            {
                                type = Style.Sql.Word;
                            }
                            else
                            {
                                type = Style.Sql.Identifier;
                            }
                            break;

                        case TokenType.Variable:
                            type = Style.Sql.User1;
                            break;

                        case TokenType.Integer:
                        case TokenType.Float:
                            type = Style.Sql.Number;
                            break;

                        default:
                            type = Style.Sql.Word;
                            break;
                    }
                }

                list.Add(Tuple.Create(strSpace.Length, type));
                i += token.Utf8Length;
            }

            // everything from the last token to the end of the string is a comment
            var numEndCommentBytes = utf8.Length - (int)i;
            if (numEndCommentBytes > 0)
            {
                var strEndComment = Encoding.UTF8.GetString(utf8, (int)i, numEndCommentBytes);
                list.Add(Tuple.Create(strEndComment.Length, Style.Sql.Comment));
            }

            BeginInvoke(
                new MethodInvoker(() =>
                {
                    if (_scintilla.Text != text)
                    {
                        // the text changed while we were working, so discard these results because
                        // another tokenization will soon deliver more up-to-date results.
                        return;
                    }

                    _scintilla.StartStyling(0);
                    foreach (var item in list)
                    {
                        _scintilla.SetStyling(item.Item1, item.Item2);
                    }
                })
            );
        });
    }

    private void SetupSyntaxColoring()
    {
        var opt = UserOptions.Instance;
        var colors = opt.GetColors();

        _scintilla.SetWhitespaceBackColor(true, colors[UserOptionsColor.CODE_BACKGROUND]);
        _scintilla.CaretForeColor = colors[UserOptionsColor.CODE_PLAIN];
        foreach (var style in _scintilla.Styles)
        {
            style.ForeColor = colors[UserOptionsColor.CODE_PLAIN];
            style.BackColor = colors[UserOptionsColor.CODE_BACKGROUND];
        }

        // In the future we may want to support: Number, Operator, User1 (variables)
        if (_syntaxColoring)
        {
            _scintilla.Styles[Style.Sql.String].ForeColor = colors[UserOptionsColor.CODE_STRING];
            _scintilla.Styles[Style.Sql.Comment].ForeColor = colors[UserOptionsColor.CODE_COMMENT];
            _scintilla.Styles[Style.Sql.Word].ForeColor = colors[UserOptionsColor.CODE_KEYWORD];
        }

        _scintilla.Styles[Style.LineNumber].ForeColor = colors[UserOptionsColor.CODE_LINENUMS];
    }
}
