using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SqlNotebookScript;
using SqlNotebookScript.Interpreter;
using SqlNotebookScript.Utils;

namespace SqlNotebook.Pages;

public sealed class QueryBlockControl : BlockControl
{
    private readonly NotebookManager _manager;

    public QueryEmbeddedControl QueryControl { get; private set; }
    public string SqlText { get; set; } = "";
    public ScriptOutput Output { get; set; } = null;
    public int MaxDisplayRows { get; set; } = 10;
    public bool ShowSql { get; set; } = true;
    public bool ShowResults { get; set; } = true;

    public QueryBlockControl(NotebookManager manager)
    {
        _manager = manager;
        _hover.Click += Hover_Click;
        ResizeRedraw = true;
        Cursor = Cursors.Hand;
        this.EnableDoubleBuffering();

        UserOptions.OnUpdate(this, Invalidate);

        Disposed += delegate
        {
            Output?.Dispose();
        };
    }

    private struct MeasuredLayout
    {
        public string SqlText;
        public Rectangle SqlBounds;
        public string PrintedText;
        public Rectangle TextBounds;
        public string ScalarResultText;
        public Rectangle ScalarResultBounds;
        public int GridRowHeight;
        public int GridCellTextYOffset;
        public List<(int NumRows, Rectangle Bounds, Rectangle RowCountBounds)> Grids;
        public int TotalHeight;
    }

    private readonly StringFormat _sqlStringFormat = new(StringFormatFlags.FitBlackBox);
    private readonly StringFormat _scalarResultStringFormat = new(StringFormatFlags.FitBlackBox);
    private readonly StringFormat _gridStringFormat = new(StringFormatFlags.FitBlackBox | StringFormatFlags.NoWrap)
    {
        Trimming = StringTrimming.None,
    };

    private MeasuredLayout Measure(Graphics g)
    {
        var opt = UserOptions.Instance;
        var codeFont = opt.GetCodeFont();
        var gridFont = opt.GetDataTableFont();
        var maxContentWidth = Width - 2 * HorizontalMargin;
        MeasuredLayout layout = new() { Grids = new() };
        var lastBounds = Rectangle.Empty;

        var spacingBetweenBlocks = this.Scaled(10);

        if (ShowSql)
        {
            // SQL code block
            layout.SqlText = (SqlText ?? "").Replace("\r", "").Replace("\t", "    ");
            var codeSize = g.MeasureString(layout.SqlText, codeFont, maxContentWidth, _sqlStringFormat).ToSize();
            layout.SqlBounds = new(HorizontalMargin, VerticalMargin, maxContentWidth, codeSize.Height);
            lastBounds = layout.SqlBounds;
        }

        if (ShowResults)
        {
            // Scalar result, if any
            if (Output?.ScalarResult != null)
            {
                layout.ScalarResultText = FormatCellValue(Output.ScalarResult);
                var textSize = g.MeasureString(
                        layout.ScalarResultText,
                        gridFont,
                        maxContentWidth,
                        _scalarResultStringFormat
                    )
                    .ToSize();
                layout.ScalarResultBounds = new(
                    HorizontalMargin,
                    lastBounds.Bottom + spacingBetweenBlocks,
                    maxContentWidth,
                    textSize.Height
                );
                lastBounds = layout.ScalarResultBounds;
            }

            // Result text, if any
            if (Output?.TextOutput?.Any() ?? false)
            {
                layout.PrintedText = string.Join(Environment.NewLine, Output.TextOutput);
                var textSize = g.MeasureString(layout.PrintedText, gridFont, maxContentWidth, _scalarResultStringFormat)
                    .ToSize();
                layout.TextBounds = new(
                    HorizontalMargin,
                    lastBounds.Bottom + spacingBetweenBlocks,
                    maxContentWidth,
                    textSize.Height
                );
                lastBounds = layout.TextBounds;
            }

            // Result grids, if any
            var textHeight = g.MeasureString("X", gridFont).ToSize().Height;
            layout.GridRowHeight = (int)(textHeight * 1.4);
            layout.GridCellTextYOffset = (layout.GridRowHeight - textHeight) / 2;
            if (Output != null)
            {
                foreach (var table in Output.DataTables)
                {
                    var numRows = Math.Min(MaxDisplayRows, table.Rows.Count);
                    Rectangle countRect = new(
                        HorizontalMargin,
                        lastBounds.Bottom + spacingBetweenBlocks,
                        maxContentWidth,
                        textHeight
                    );
                    Rectangle gridRect = new(
                        HorizontalMargin,
                        countRect.Bottom,
                        maxContentWidth,
                        layout.GridRowHeight * (numRows + 1)
                    );
                    layout.Grids.Add((numRows, gridRect, countRect));
                    lastBounds = gridRect;
                }
            }
        }

        layout.TotalHeight = lastBounds.Bottom + VerticalMargin;

        return layout;
    }

    public override int CalculateHeight()
    {
        if (EditMode)
        {
            return this.Scaled(500);
        }

        using var g = CreateGraphics();
        return Measure(g).TotalHeight;
    }

    protected override void OnPaint(Graphics g, UserOptions opt, Color[] colors, Color backColor)
    {
        var layout = Measure(g);
        var gridFont = opt.GetDataTableFont();
        using SolidBrush gridTextBrush = new(colors[UserOptionsColor.GRID_PLAIN]);

        if (ShowSql && !string.IsNullOrWhiteSpace(layout.SqlText))
        {
            g.DrawString(layout.SqlText, opt.GetCodeFont(), gridTextBrush, layout.SqlBounds, _sqlStringFormat);
        }

        if (Output == null || !ShowResults)
        {
            return;
        }

        if (layout.ScalarResultText != null)
        {
            g.DrawString(
                layout.ScalarResultText,
                gridFont,
                gridTextBrush,
                layout.ScalarResultBounds,
                _scalarResultStringFormat
            );
        }

        if (layout.PrintedText != null)
        {
            g.DrawString(layout.PrintedText, gridFont, gridTextBrush, layout.TextBounds, _scalarResultStringFormat);
        }

        SizeF rowHeightSize = new(float.MaxValue, layout.GridRowHeight);

        for (var tableIndex = 0; tableIndex < (Output?.DataTables?.Count ?? 0); tableIndex++)
        {
            var table = Output.DataTables[tableIndex];
            var (numRows, gridBounds, countBounds) = layout.Grids[tableIndex];

            var countText =
                table.FullCount != table.Rows.Count
                    ? $"{table.FullCount:#,##0} row{(table.FullCount == 1 ? "" : "s")} ({numRows} shown)"
                    : $"{table.FullCount:#,##0} row{(table.FullCount == 1 ? "" : "s")}";
            var countTextHeight = g.MeasureString(countText, gridFont).Height;
            g.DrawString(countText, gridFont, gridTextBrush, new PointF(countBounds.Left, countBounds.Top));

            // Measure the widths of the values in each column, among rows we intend to show.
            var columnWidths = new int[table.Columns.Count];

            for (var j = 0; j < table.Columns.Count; j++)
            {
                var columnName = table.Columns[j];
                var size = g.MeasureString(columnName, gridFont, rowHeightSize, _gridStringFormat).ToSize();
                columnWidths[j] = Math.Max(columnWidths[j], size.Width);
            }

            for (var i = 0; i < numRows; i++)
            {
                var row = table.Rows[i];
                for (var j = 0; j < table.Columns.Count; j++)
                {
                    var formattedCellValue = FormatCellValue(row[j]);
                    var size = g.MeasureString(formattedCellValue, gridFont, rowHeightSize, _gridStringFormat).ToSize();
                    columnWidths[j] = Math.Max(columnWidths[j], size.Width);
                }
            }

            var cellLeftPadding = this.Scaled(3);
            var cellRightPadding = this.Scaled(16);

            for (var j = 0; j < table.Columns.Count; j++)
            {
                // Add padding to each column
                columnWidths[j] += cellLeftPadding + cellRightPadding;
                // Cap each width at a reasonable max limit
                columnWidths[j] = Math.Min(columnWidths[j], this.Scaled(500));
            }

            // Squish the table horizontally to fit into the space we have.
            int GetOverage() => columnWidths.Sum() - gridBounds.Width;
            int overage;

            void SquishColumnsOverWidth(int n)
            {
                // Try squishing columns over N down to N without touching other columns.
                var scaledN = this.Scaled(n);
                var widthOverN = columnWidths.Where(x => x > scaledN).Sum(x => x - scaledN);
                if (widthOverN > 0 && widthOverN < overage)
                {
                    // Even if we drop these all to N it's still not enough, so just drop them straight to N.
                    for (var i = 0; i < columnWidths.Length; i++)
                    {
                        if (columnWidths[i] > scaledN)
                        {
                            columnWidths[i] = scaledN;
                        }
                    }
                }
                else if (widthOverN > 0 && widthOverN > overage)
                {
                    // We can make the difference here. Shrink just enough to cover the overage.
                    var percentOfWidthOverNToKeep = 1 - (double)overage / widthOverN;
                    for (var i = 0; i < columnWidths.Length; i++)
                    {
                        var w = columnWidths[i];
                        if (w > scaledN)
                        {
                            w -= scaledN;
                            w = (int)(w * percentOfWidthOverNToKeep);
                            w += scaledN;
                            columnWidths[i] = w;
                        }
                    }
                }
            }
            if ((overage = GetOverage()) > 0)
            {
                SquishColumnsOverWidth(200);
            }
            if ((overage = GetOverage()) > 0)
            {
                SquishColumnsOverWidth(75);
            }
            if ((overage = GetOverage()) > 0)
            {
                // Shrinking large columns only didn't do the trick, so now squish all columns proportionally.
                var squishFactor = gridBounds.Width / (double)columnWidths.Sum();
                for (var j = 0; j < table.Columns.Count; j++)
                {
                    columnWidths[j] = (int)(columnWidths[j] * squishFactor);
                }
            }

            // Draw the header background
            using SolidBrush headerBrush = new(colors[UserOptionsColor.GRID_HEADER]);
            g.FillRectangle(headerBrush, gridBounds.Left, gridBounds.Top, columnWidths.Sum(), layout.GridRowHeight);

            // Draw the header text and cell text
            var headerX = gridBounds.Left;
            var textHeight = g.MeasureString("X", gridFont).ToSize().Height;
            var maxCellTextHeight = layout.GridRowHeight - layout.GridCellTextYOffset;
            for (var j = 0; j < table.Columns.Count; j++)
            {
                var columnWidth = columnWidths[j];
                var maxTextWidth = columnWidth - cellLeftPadding;

                // Header text
                g.DrawString(
                    table.Columns[j],
                    gridFont,
                    gridTextBrush,
                    new Rectangle(
                        headerX + cellLeftPadding,
                        gridBounds.Top + layout.GridCellTextYOffset,
                        maxTextWidth,
                        maxCellTextHeight
                    ),
                    _gridStringFormat
                );

                // Cells text
                for (var i = 0; i < numRows; i++)
                {
                    var cellY = gridBounds.Top + (i + 1) * layout.GridRowHeight;
                    var cellText = FormatCellValue(table.Rows[i][j]);
                    g.DrawString(
                        cellText,
                        gridFont,
                        gridTextBrush,
                        new Rectangle(
                            headerX + cellLeftPadding,
                            cellY + layout.GridCellTextYOffset,
                            maxTextWidth,
                            maxCellTextHeight
                        ),
                        _gridStringFormat
                    );
                }

                headerX += columnWidths[j];
            }

            // Draw the vertical grid lines
            var verticalLineX = gridBounds.Left;
            using Pen gridPen = new(colors[UserOptionsColor.GRID_LINES], this.Scaled(1));
            for (var j = 0; j < table.Columns.Count; j++)
            {
                g.DrawLine(gridPen, verticalLineX, gridBounds.Top, verticalLineX, gridBounds.Bottom);
                verticalLineX += columnWidths[j];
            }
            g.DrawLine(gridPen, verticalLineX, gridBounds.Top, verticalLineX, gridBounds.Bottom);

            // Draw the horizontal grid lines
            var horizontalLineY = gridBounds.Top;
            for (var i = 0; i < numRows + 1; i++)
            {
                g.DrawLine(gridPen, gridBounds.Left, horizontalLineY, verticalLineX, horizontalLineY);
                horizontalLineY += layout.GridRowHeight;
            }
            g.DrawLine(gridPen, gridBounds.Left, horizontalLineY, verticalLineX, horizontalLineY);
        }
    }

    private static string FormatCellValue(object obj) =>
        obj switch
        {
            double x => $"{x:0.####}",
            byte[] x => BlobUtil.ToString(x),
            _ => obj.ToString(),
        };

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

        QueryControl = new(_manager, isPageContext: true, initialText: SqlText)
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Margin = Padding.Empty,
            Output = Output?.TakeRef(),
            MaxRows = MaxDisplayRows,
            ShowSql = ShowSql,
            ShowResults = ShowResults,
        };
        panel.Controls.Add(QueryControl);
        Controls.Add(table);
        QueryControl.TextControl.Focus();

        QueryControl.Dirty += delegate
        {
            RaiseDirty();
        };
        QueryControl.TextControl.F10KeyPress += delegate
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
            var control = Controls[i];
            Controls.RemoveAt(i);
            control.Dispose();
        }
        QueryControl = null;
        Height = CalculateHeight();
        Invalidate(true);
        RaiseBlockClicked();
    }

    private void UpdatePropertiesFromEditMode()
    {
        SqlText = QueryControl.SqlText;
        if (!ReferenceEquals(Output, QueryControl.Output))
        {
            Output?.Dispose();
            Output = QueryControl.Output?.TakeRef();
        }
        MaxDisplayRows = QueryControl.MaxRows;
        ShowSql = QueryControl.ShowSql;
        ShowResults = QueryControl.ShowResults;
    }

    public void LoadFromRecord(QueryPageBlockRecord record)
    {
        SqlText = record.Sql;
        if (!ReferenceEquals(Output, record.Output))
        {
            Output?.Dispose();
            Output = record.Output?.TakeRef();
        }
        ShowSql = record.Options.ShowSql;
        ShowResults = record.Options.ShowResults;
        MaxDisplayRows = record.Options.MaxDisplayRows;
        Height = CalculateHeight();
        Invalidate(true);
    }

    public QueryPageBlockRecord SaveToRecord()
    {
        if (EditMode)
        {
            UpdatePropertiesFromEditMode();
        }
        return new()
        {
            Sql = SqlText,
            Output = Output?.TakeRef(),
            Options = new()
            {
                ShowSql = ShowSql,
                ShowResults = ShowResults,
                MaxDisplayRows = MaxDisplayRows,
            },
        };
    }

    public ScriptOutput ExecuteOnWorkerThread()
    {
        return SqlUtil.WithTransaction(
            _manager.Notebook,
            () =>
            {
                return _manager.ExecuteScript(SqlText);
            }
        );
    }
}
