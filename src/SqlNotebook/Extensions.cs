using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SqlNotebookScript.DataTables;
using SqlNotebookScript.Utils;

namespace SqlNotebook;

public static class Extensions
{
    private const int WM_SETREDRAW = 0x0B;

    public static void BeginUpdate(this RichTextBox self)
    {
        NativeMethods.SendMessage(self.Handle, WM_SETREDRAW, (IntPtr)0, IntPtr.Zero);
    }

    public static void EndUpdate(this RichTextBox self)
    {
        NativeMethods.SendMessage(self.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
        self.Invalidate();
    }

    public static void SetInnerMargins(this TextBoxBase textBox, int left, int top, int right, int bottom)
    {
        var rect = textBox.GetFormattingRect();

        var newRect = new Rectangle(left, top, rect.Width - left - right, rect.Height - top - bottom);
        textBox.SetFormattingRect(newRect);
    }

    private static void SetFormattingRect(this TextBoxBase textbox, Rectangle rect)
    {
        NativeMethods.RECT rc = new(rect);
        NativeMethods.SendMessageRefRect(textbox.Handle, NativeMethods.EM_SETRECT, 0, ref rc);
    }

    private static Rectangle GetFormattingRect(this TextBoxBase textbox)
    {
        var rect = new Rectangle();
        NativeMethods.SendMessage(textbox.Handle, NativeMethods.EM_GETRECT, (IntPtr)0, ref rect);
        return rect;
    }

    public static void EnableDoubleBuffer(this ListView self)
    {
        typeof(ListView)
            .GetMethod("SetStyle", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(self, new object[] { ControlStyles.OptimizedDoubleBuffer, true });
    }

    public static void InstallCopyPasteHandling(this RichTextBox richTextBox, bool allowRtfPaste)
    {
        richTextBox.AllowDrop = false;
        richTextBox.EnableAutoDragDrop = false;
        richTextBox.ShortcutsEnabled = false;
        richTextBox.PreviewKeyDown += (sender, e) =>
        {
            var isCtrlC = e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.C;
            var isCtrlIns = e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.Insert;
            var isCtrlV = e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.V;
            var isShiftIns = !e.Control && !e.Alt && e.Shift && e.KeyCode == Keys.Insert;
            if (isCtrlC || isCtrlIns)
            {
                richTextBox.Copy();
            }
            else if (isCtrlV || isShiftIns)
            {
                if (richTextBox.SelectionProtected)
                {
                    System.Media.SystemSounds.Exclamation.Play();
                }
                else if (allowRtfPaste && Clipboard.ContainsText(TextDataFormat.Rtf))
                {
                    string unprotectedRtf;
                    using (var offscreen = new RichTextBox { Rtf = Clipboard.GetText(TextDataFormat.Rtf) })
                    {
                        offscreen.SelectAll();
                        offscreen.SelectionProtected = false;
                        unprotectedRtf = offscreen.Rtf;
                    }
                    richTextBox.SelectedRtf = unprotectedRtf;
                }
                else if (Clipboard.ContainsText())
                {
                    richTextBox.SelectedText = Clipboard.GetText();
                }
            }
        };
    }

    public static ImageList PadListViewIcons(this ImageList imageList, Graphics graphics)
    {
        var scale = graphics.DpiX / 96;
        ImageList paddedImageList = new()
        {
            ImageSize = new Size((int)(18 * scale), (int)(17 * scale)),
            ColorDepth = ColorDepth.Depth32Bit,
        };
        foreach (Image image in imageList.Images)
        {
            Bitmap newImage = new((int)(18 * scale), (int)(16 * scale), image.PixelFormat);
            using (var g = Graphics.FromImage(newImage))
            {
                g.DrawImage(image, 0, 1 * scale, 16 * scale, 16 * scale);
            }
            paddedImageList.Images.Add(newImage);
        }

        return paddedImageList;
    }

    public static void RemoveWhere<T>(this List<T> self, Func<T, bool> selector)
    {
        for (int i = self.Count - 1; i >= 0; i--)
        {
            if (selector(self[i]))
            {
                self.RemoveAt(i);
            }
        }
    }

    public static void MakeDivider(this Label self, int leftMargin = 5, int rightMargin = 5)
    {
        var y = self.Top + self.Height / 2;
        var leftLine = new Panel
        {
            Left = leftMargin,
            Top = y,
            Width = self.Left - 2 - (leftMargin),
            Height = 1,
            BackColor = SystemColors.ControlDark,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
        };
        var rightLine = new Panel
        {
            Left = self.Right + 2,
            Top = y,
            Width = self.Parent.Width - rightMargin - (self.Right + 2),
            Height = 1,
            BackColor = SystemColors.ControlDark,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
        };
        self.Parent.Controls.Add(leftLine);
        self.Parent.Controls.Add(rightLine);
    }

    public static DialogResult ShowDialogAndDispose(this Form form, IWin32Window owner)
    {
        using (form)
        {
            return form.ShowDialog(owner);
        }
    }

    // make combo boxes less awkward in data grids
    public static void ApplyOneClickComboBoxFix(this DataGridView self)
    {
        // show the dropdown box on the first cell click
        self.CellEnter += (sender, e) =>
        {
            var isCombo =
                e.RowIndex != -1 && e.ColumnIndex != -1 && self.Columns[e.ColumnIndex] is DataGridViewComboBoxColumn;
            if (isCombo)
            {
                self.BeginEdit(true);
                self.BeginInvoke(
                    new MethodInvoker(() =>
                    {
                        var editingControl = self.EditingControl as ComboBox;
                        if (editingControl != null)
                        {
                            editingControl.DroppedDown = true;
                        }
                    })
                );
            }
        };

        // commit the selected dropdown value as soon as it is clicked
        self.CurrentCellDirtyStateChanged += (sender, e) =>
        {
            var colIndex = self.CurrentCell.ColumnIndex;
            if (self.Columns[colIndex] is DataGridViewComboBoxColumn && self.IsCurrentCellDirty)
            {
                self.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
    }

    public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> self, TKey key)
    {
        TValue value;
        if (self.TryGetValue(key, out value))
        {
            return value;
        }
        else
        {
            return default(TValue);
        }
    }

    public static void EnableDoubleBuffering(this Control self)
    {
        typeof(Control).InvokeMember(
            name: "DoubleBuffered",
            invokeAttr: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
            binder: null,
            target: self,
            args: new object[] { true }
        );
    }

    public static DataTable ToDataTable(this SimpleDataTable self, int maxRows = int.MaxValue)
    {
        var dt = new DataTable();
        foreach (var col in self.Columns)
        {
            dt.Columns.Add(col);
        }
        dt.BeginLoadData();
        foreach (var row in self.Rows)
        {
            var objs = new object[row.Length];
            for (int i = 0; i < row.Length; i++)
            {
                objs[i] = row[i] switch
                {
                    double n => $"{n:0.####}",
                    byte[] bytes => BlobUtil.ToString(bytes),
                    _ => row[i],
                };
            }
            dt.LoadDataRow(objs, true);

            if (dt.Rows.Count >= maxRows)
            {
                break;
            }
        }
        dt.EndLoadData();
        return dt;
    }

    public static void Drain<T>(this BlockingCollection<T> self)
    {
        T item;
        while (self.TryTake(out item)) { }
    }

    private static void FixToolStripMargins(ToolStrip self)
    {
        foreach (var menu in self.Items.OfType<ToolStripMenuItem>())
        {
            FixMenuMargins(menu);
        }
    }

    private static void FixContextMenuMargins(ContextMenuStrip self)
    {
        self.Opened += (sender, e) =>
        {
            var items = self.Items.OfType<ToolStripMenuItem>().Where(x => x.Visible);
            var first = items.First();
            var last = items.Last();
            foreach (var item in items)
            {
                if (item == first && item == last)
                {
                    item.Margin = new Padding(0, 1, 0, 1);
                }
                else if (item == first)
                {
                    item.Margin = new Padding(0, 1, 0, 0);
                }
                else if (item == last)
                {
                    item.Margin = new Padding(0, 0, 0, 1);
                }
                else
                {
                    item.Margin = Padding.Empty;
                }
            }
        };
    }

    private static void FixMenuMargins(ToolStripMenuItem self)
    {
        self.DropDownOpened += (sender, e) =>
        {
            var items = self.DropDownItems.OfType<ToolStripMenuItem>().Where(x => x.Visible);
            var first = items.First();
            var last = items.Last();
            foreach (var item in items)
            {
                if (item == first && item == last)
                {
                    item.Margin = new Padding(0, 1, 0, 1);
                }
                else if (item == first)
                {
                    item.Margin = new Padding(0, 1, 0, 0);
                }
                else if (item == last)
                {
                    item.Margin = new Padding(0, 0, 0, 1);
                }
                else
                {
                    item.Margin = Padding.Empty;
                }
            }
        };
    }

    public static void SetMenuAppearance(this ToolStrip self)
    {
        self.Renderer = new MenuRenderer();
        FixToolStripMargins(self);
    }

    public static void SetMenuAppearance(this ContextMenuStrip self)
    {
        self.Renderer = new MenuRenderer();
        FixContextMenuMargins(self);
    }

    public static void AutoSizeColumns(this DataGridView self, int maxWidth = 250)
    {
        foreach (DataGridViewColumn col in self.Columns)
        {
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            var width = col.Width;
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            col.Width = Math.Min(maxWidth, width);
        }
    }

    public static int Scaled(this Control self, double x) => (int)(x * self.DeviceDpi / 96);

    public static float ScaledF(this Control self, double x) => (float)(x * self.DeviceDpi / 96);

    public static Size ToSize(this SizeF self) => new((int)self.Width, (int)self.Height);

    #region Enum Descriptions
    // https://www.codementor.io/cerkit/giving-an-enum-a-string-value-using-the-description-attribute-6b4fwdle0
    public static string GetDescription<T>(this T e)
        where T : IConvertible
    {
        string description = null;

        if (e is Enum)
        {
            var type = e.GetType();
            var values = Enum.GetValues(type).Cast<int>();

            foreach (var val in values)
            {
                if (val == e.ToInt32(CultureInfo.InvariantCulture))
                {
                    var memInfo = type.GetMember(type.GetEnumName(val));
                    var descriptionAttributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                    if (descriptionAttributes.Length > 0)
                    {
                        // we're only getting the first description we find
                        // others will be ignored
                        description = ((DescriptionAttribute)descriptionAttributes[0]).Description;
                    }

                    break;
                }
            }
        }

        return description;
    }

    public static T GetValueFromDescription<T>(this T e, string description)
        where T : IConvertible
    {
        if (e is Enum)
        {
            var type = e.GetType();
            var values = Enum.GetValues(type);

            foreach (var val in values)
            {
                var memInfo = type.GetMember(type.GetEnumName(val));
                var descriptionAttributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (descriptionAttributes.Length > 0)
                {
                    // we're only getting the first description we find
                    // others will be ignored
                    if (description == ((DescriptionAttribute)descriptionAttributes[0]).Description)
                    {
                        return (T)val;
                    }
                }
            }
        }

        return default(T);
    }

    public static IEnumerable<string> GetDescriptions<T>(this T e)
        where T : IConvertible
    {
        if (e is Enum)
        {
            var type = e.GetType();
            var values = Enum.GetValues(type);

            foreach (var val in values)
            {
                var memInfo = type.GetMember(type.GetEnumName(val));
                var descriptionAttributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (descriptionAttributes.Length > 0)
                {
                    // we're only getting the first description we find
                    // others will be ignored
                    yield return ((DescriptionAttribute)descriptionAttributes[0]).Description;
                }
            }
        }
    }
    #endregion // Enum Descriptions

    public static void SetCueText(this TextBox self, string text)
    {
        if (self.IsHandleCreated)
        {
            NativeMethods.SendMessage(self.Handle, NativeMethods.EM_SETCUEBANNER, (IntPtr)1, text);
        }
        else
        {
            self.HandleCreated += delegate
            {
                self.SetCueText(text);
            };
        }
    }

    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public readonly int Left;
            public readonly int Top;
            public readonly int Right;
            public readonly int Bottom;

            private RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public RECT(Rectangle r)
                : this(r.Left, r.Top, r.Right, r.Bottom) { }
        }

        public const int EM_GETRECT = 0xB2;
        public const int EM_SETRECT = 0xB3;

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        [DllImport("user32.dll", EntryPoint = @"SendMessage", CharSet = CharSet.Auto)]
        public static extern int SendMessageRefRect(IntPtr hWnd, uint msg, int wParam, ref RECT rect);

        [DllImport("user32.dll", EntryPoint = @"SendMessage", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, ref Rectangle lParam);

        public const uint ECM_FIRST = 0x1500;
        public const uint EM_SETCUEBANNER = ECM_FIRST + 1;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wp, string lp);
    }
}
