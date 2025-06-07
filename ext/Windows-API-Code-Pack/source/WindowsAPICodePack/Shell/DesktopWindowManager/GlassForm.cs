using MS.WindowsAPICodePack.Internal;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>Windows Glass Form Inherit from this form to be able to enable glass on Windows Form</summary>
    public class GlassForm : Form
    {
        /// <summary>Fires when the availability of Glass effect changes.</summary>
        public event EventHandler<AeroGlassCompositionChangedEventArgs> AeroGlassCompositionChanged;

        /// <summary>Get determines if AeroGlass is enabled on the desktop. Set enables/disables AreoGlass on the desktop.</summary>
        public static bool AeroGlassCompositionEnabled
        {
            set => DesktopWindowManagerNativeMethods.DwmEnableComposition(
                    value ? CompositionEnable.Enable : CompositionEnable.Disable);
            get => DesktopWindowManagerNativeMethods.DwmIsCompositionEnabled();
        }

        /// <summary>Excludes a Control from the AeroGlass frame.</summary>
        /// <param name="control">The control to exclude.</param>
        /// <remarks>
        /// Many non-WPF rendered controls (i.e., the ExplorerBrowser control) will not render properly on top of an AeroGlass frame.
        /// </remarks>
        public void ExcludeControlFromAeroGlass(Control control)
        {
            if (control == null) { throw new ArgumentNullException("control"); }

            if (AeroGlassCompositionEnabled)
            {
                var clientScreen = RectangleToScreen(ClientRectangle);
                var controlScreen = control.RectangleToScreen(control.ClientRectangle);

                var margins = new Margins
                {
                    LeftWidth = controlScreen.Left - clientScreen.Left,
                    RightWidth = clientScreen.Right - controlScreen.Right,
                    TopHeight = controlScreen.Top - clientScreen.Top,
                    BottomHeight = clientScreen.Bottom - controlScreen.Bottom
                };

                // Extend the Frame into client area
                DesktopWindowManagerNativeMethods.DwmExtendFrameIntoClientArea(Handle, ref margins);
            }
        }

        /// <summary>Resets the AeroGlass exclusion area.</summary>
        public void ResetAeroGlass()
        {
            if (Handle != IntPtr.Zero)
            {
                var margins = new Margins(true);
                DesktopWindowManagerNativeMethods.DwmExtendFrameIntoClientArea(Handle, ref margins);
            }
        }

        /// <summary>Makes the background of current window transparent</summary>
        public void SetAeroGlassTransparency() => BackColor = Color.Transparent;

        /// <summary>Catches the DWM messages to this window and fires the appropriate event.</summary>
        /// <param name="m"></param>

        /// <summary>Initializes the Form for AeroGlass</summary>
        /// <param name="e">The arguments for this event</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ResetAeroGlass();
        }

        /// <summary>Overide OnPaint to paint the background as black.</summary>
        /// <param name="e">PaintEventArgs</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (DesignMode == false)
            {
                if (AeroGlassCompositionEnabled && e != null)
                {
                    // Paint the all the regions black to enable glass
                    e.Graphics.FillRectangle(Brushes.Black, ClientRectangle);
                }
            }
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == DWMMessages.WM_DWMCOMPOSITIONCHANGED
                || m.Msg == DWMMessages.WM_DWMNCRENDERINGCHANGED)
            {
                if (AeroGlassCompositionChanged != null)
                {
                    AeroGlassCompositionChanged.Invoke(this,
                        new AeroGlassCompositionChangedEventArgs(AeroGlassCompositionEnabled));
                }
            }

            base.WndProc(ref m);
        }
    }
}