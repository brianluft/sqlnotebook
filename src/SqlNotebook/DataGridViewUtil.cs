using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SqlNotebook;

public static class DataGridViewUtil
{
    public static DataGridView NewDataGridView(
        bool columnHeadersVisible = true,
        bool rowHeadersVisible = false,
        bool autoGenerateColumns = true,
        bool allowColumnResize = true,
        bool allowSort = true,
        bool userColors = true,
        bool contextMenu = false
    )
    {
        DoubleBufferedDataGridView grid = new()
        {
            AutoSize = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            AutoGenerateColumns = autoGenerateColumns,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToOrderColumns = false,
            AllowUserToResizeColumns = allowColumnResize,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackgroundColor = Color.White,
            RowHeadersVisible = rowHeadersVisible,
            ColumnHeadersVisible = columnHeadersVisible,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing,
            ShowCellToolTips = false,
        };

        if (userColors)
        {
            grid.EnableHeadersVisualStyles = false;
            AttachFontColorEventHandler(grid);
        }

        if (allowSort)
        {
            grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
            grid.ColumnAdded += (sender, e) => e.Column.SortMode = DataGridViewColumnSortMode.Programmatic;
        }
        else
        {
            grid.ColumnAdded += (sender, e) => e.Column.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        if (contextMenu)
        {
            ContextMenuStrip contextMenuStrip = new();
            contextMenuStrip.SetMenuAppearance();

            ToolStripMenuItem viewFullTextMenuItem = new("View full text");
            contextMenuStrip.Items.Add(viewFullTextMenuItem);

            viewFullTextMenuItem.Click += (sender, e) =>
            {
                var text = grid.SelectedCells[0].Value.ToString();
                using ViewFullTextForm f = new(text);
                f.ShowDialog(grid.TopLevelControl);
                grid.TopLevelControl.Focus();
            };

            grid.CellContextMenuStripNeeded += (sender, e) =>
            {
                e.ContextMenuStrip = grid.SelectedCells.Count == 1 ? contextMenuStrip : null;
            };

            grid.Disposed += delegate
            {
                contextMenuStrip.Dispose();
            };
        }

        return grid;
    }

    private static void Grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
    {
        var grid = (DataGridView)sender;
        if (grid.DataSource is not DataTable table)
        {
            return; // ???
        }

        var desc = grid.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection == SortOrder.Ascending;

        List<object[]> rows = new(table.Rows.Count);
        foreach (DataRow row in table.Rows)
        {
            rows.Add(row.ItemArray);
        }
        rows.Sort(desc ? CompareDesc : Compare);

        var clone = table.Clone();
        clone.BeginLoadData();
        foreach (var row in rows)
        {
            clone.LoadDataRow(row, true);
        }
        clone.EndLoadData();

        grid.DataSource = clone;

        for (var i = 0; i < grid.Columns.Count; i++)
        {
            var header = grid.Columns[i].HeaderCell;
            if (i == e.ColumnIndex)
            {
                header.SortGlyphDirection = desc ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                header.SortGlyphDirection = SortOrder.None;
            }
        }

        int CompareDesc(object[] xRow, object[] yRow)
        {
            return -Compare(xRow, yRow);
        }

        int Compare(object[] xRow, object[] yRow)
        {
            var compare = CompareColumn(xRow, yRow, e.ColumnIndex);
            // Tie-breaker: every column starting from the left.
            for (var j = 0; compare == 0 && j < xRow.Length; j++)
            {
                compare = CompareColumn(xRow, yRow, j);
            }
            return compare;
        }

        static int CompareColumn(object[] xRow, object[] yRow, int columnIndex)
        {
            var xObj = xRow[columnIndex];
            var yObj = yRow[columnIndex];
            if (xObj is double xDbl && yObj is double yDbl)
            {
                return xDbl.CompareTo(yDbl);
            }
            else if (xObj is int xInt && yObj is int yInt)
            {
                return xInt.CompareTo(yInt);
            }
            else if (xObj is long xLong && yObj is long yLong)
            {
                return xLong.CompareTo(yLong);
            }
            var xStr = xObj?.ToString() ?? "";
            var yStr = yObj?.ToString() ?? "";
            if (double.TryParse(xStr, out var lhsNum) && double.TryParse(yStr, out var rhsNum))
            {
                return lhsNum.CompareTo(rhsNum);
            }
            else
            {
                return xStr.CompareTo(yStr);
            }
        }
    }

    public static void AttachFontColorEventHandler(DataGridView grid)
    {
        void OptionsUpdated()
        {
            using var g = grid.CreateGraphics();
            var oldX = g.MeasureString("x", grid.Font, PointF.Empty, StringFormat.GenericTypographic);
            var opt = UserOptions.Instance;
            grid.Font =
                grid.DefaultCellStyle.Font =
                grid.ColumnHeadersDefaultCellStyle.Font =
                grid.RowHeadersDefaultCellStyle.Font =
                grid.RowsDefaultCellStyle.Font =
                grid.DefaultCellStyle.Font =
                    opt.GetDataTableFont();
            var newX = g.MeasureString("x", grid.Font, PointF.Empty, StringFormat.GenericTypographic);
            var widthRatio = newX.Width / oldX.Width;
            var rowHeight = (int)(newX.Height * 1.5);
            grid.RowTemplate.Height = rowHeight;
            grid.ColumnHeadersHeight = rowHeight;

            var colors = opt.GetColors();
            grid.BackgroundColor = colors[UserOptionsColor.GRID_BACKGROUND];
            grid.ForeColor = colors[UserOptionsColor.GRID_PLAIN];
            grid.GridColor = colors[UserOptionsColor.GRID_LINES];
            grid.ColumnHeadersDefaultCellStyle.BackColor = colors[UserOptionsColor.GRID_HEADER];
            grid.ColumnHeadersDefaultCellStyle.ForeColor = colors[UserOptionsColor.GRID_PLAIN];
            grid.RowHeadersDefaultCellStyle.BackColor = colors[UserOptionsColor.GRID_HEADER];
            grid.RowHeadersDefaultCellStyle.ForeColor = colors[UserOptionsColor.GRID_PLAIN];
            grid.RowTemplate.DefaultCellStyle.BackColor = colors[UserOptionsColor.GRID_BACKGROUND];
            grid.DefaultCellStyle.ForeColor = colors[UserOptionsColor.GRID_PLAIN];
            grid.DefaultCellStyle.BackColor = colors[UserOptionsColor.GRID_BACKGROUND];

            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.Width = (int)(column.Width * widthRatio);
            }

            DataGridViewCellStyle cellStyle = null;
            foreach (DataGridViewRow row in grid.Rows)
            {
                row.Height = rowHeight;
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cellStyle == null)
                    {
                        cellStyle = cell.Style;
                        cellStyle.ForeColor = colors[UserOptionsColor.GRID_PLAIN];
                        cellStyle.BackColor = colors[UserOptionsColor.GRID_BACKGROUND];
                    }
                    cell.Style = cellStyle;
                }
            }

            // This seems to be necessary, otherwise a black bar sometimes appears between the column header and
            // the rows. Refresh() and Invalidate() don't seem to work.
            grid.Width++;
            grid.Width--;
        }
        ;
        OptionsUpdated();
        UserOptions.OnUpdate(grid, OptionsUpdated);
    }

    // This will hide the arrow on the row header of the current row.
    public static void ApplyCustomRowHeaderPaint(DataGridView grid)
    {
        SolidBrush brush = new(grid.ForeColor);
        grid.CellPainting += (_, e) =>
        {
            if (e.ColumnIndex < 0 && e.RowIndex >= 0)
            {
                e.PaintBackground(e.CellBounds, true);
                StringFormat stringFormat = new();
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;
                e.Graphics.DrawString($"{e.Value}", e.CellStyle.Font, brush, e.CellBounds, stringFormat);
                e.Handled = true;
            }
        };
    }

    private sealed class DoubleBufferedDataGridView : DataGridView
    {
        private bool _drawVerticalResizeLine;
        private int _verticalResizeLineX;

        public DoubleBufferedDataGridView()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_drawVerticalResizeLine)
            {
                var colors = UserOptions.Instance.GetColors();
                using Pen pen = new(colors[UserOptionsColor.GRID_PLAIN]) { DashStyle = DashStyle.Dot };
                e.Graphics.DrawLine(
                    pen,
                    _verticalResizeLineX,
                    ColumnHeadersHeight,
                    _verticalResizeLineX,
                    ClientSize.Height
                );
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.Left && Cursor == Cursors.SizeWE)
            {
                _drawVerticalResizeLine = true;
                _verticalResizeLineX = e.X;
                Invalidate();
            }
            else
            {
                _drawVerticalResizeLine = false;
                Invalidate();
            }
        }
    }
}
