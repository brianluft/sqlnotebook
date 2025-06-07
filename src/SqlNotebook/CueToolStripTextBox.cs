using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace SqlNotebook;

[ToolStripItemDesignerAvailability(ToolStripItemDesignerAvailability.ToolStrip)]
public sealed class CueToolStripTextBox : ToolStripControlHost
{
    public CueToolStripTextBox()
        : base(new StyledTextBox())
    {
        InnerTextBox.Width = 150;
        Font = new Font("Segoe UI", 9);
    }

    public TextBox InnerTextBox
    {
        get { return (TextBox)Control; }
    }

    public override string Text
    {
        get { return InnerTextBox.Text; }
        set { InnerTextBox.Text = value; }
    }

    public string CueText
    {
        set { SetCueText(InnerTextBox, value); }
    }

    public void SelectAll()
    {
        InnerTextBox.SelectAll();
    }

    public static void SetCueText(TextBox self, string text)
    {
        if (self.IsHandleCreated)
        {
            NativeMethods.SendMessage(self.Handle, NativeMethods.EM_SETCUEBANNER, (IntPtr)1, text ?? "");
        }
        else
        {
            self.HandleCreated += (sender, e) => SetCueText(self, text);
        }
    }

    private static class NativeMethods
    {
        public const uint ECM_FIRST = 0x1500;
        public const uint EM_SETCUEBANNER = ECM_FIRST + 1;
        public const int WM_NCPAINT = 0x85;

        [DllImport("user32")]
        public static extern IntPtr GetWindowDC(IntPtr hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 msg, IntPtr wp, string lp);
    }

    private sealed class StyledTextBox : TextBox
    {
        private static readonly Pen _unfocusedPen = new Pen(Color.FromArgb(230, 230, 230));
        private static readonly Pen _focusedPen = new Pen(Color.FromArgb(0, 120, 215));

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == NativeMethods.WM_NCPAINT)
            {
                var dc = NativeMethods.GetWindowDC(Handle);
                using (Graphics g = Graphics.FromHdc(dc))
                {
                    g.DrawRectangle(Focused ? _focusedPen : _unfocusedPen, 0, 0, Width - 1, Height - 1);
                }
            }
        }
    }
}
