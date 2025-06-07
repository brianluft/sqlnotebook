/* Unmerged change from project 'Shell (net452)'
Before:
using System;
using System.Windows.Interop;
using System.Windows.Media;
using MS.WindowsAPICodePack.Internal;
using Microsoft.WindowsAPICodePack.Shell.Interop;
After:
using Microsoft.WindowsAPICodePack.Shell.Interop;
using MS.WindowsAPICodePack.Internal;
using System;
using MS.Windows;
using System.Windows.Interop;
*/

/* Unmerged change from project 'Shell (net462)'
Before:
using System;
using System.Windows.Interop;
using System.Windows.Media;
using MS.WindowsAPICodePack.Internal;
using Microsoft.WindowsAPICodePack.Shell.Interop;
After:
using Microsoft.WindowsAPICodePack.Shell.Interop;
using MS.WindowsAPICodePack.Internal;
using System;
using MS.Windows;
using System.Windows.Interop;
*/

/* Unmerged change from project 'Shell (net472)'
Before:
using System;
using System.Windows.Interop;
using System.Windows.Media;
using MS.WindowsAPICodePack.Internal;
using Microsoft.WindowsAPICodePack.Shell.Interop;
After:
using Microsoft.WindowsAPICodePack.Shell.Interop;
using MS.WindowsAPICodePack.Internal;
using System;
using MS.Windows;
using System.Windows.Interop;
*/

using MS.WindowsAPICodePack.Internal;
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Microsoft.WindowsAPICodePack.Shell
{
    /// <summary>WPF Glass Window Inherit from this window class to enable glass on a WPF window</summary>
    public class GlassWindow : Window
    {
        private IntPtr windowHandle;

        /// <summary>Fires when the availability of Glass effect changes.</summary>
        public event EventHandler<AeroGlassCompositionChangedEventArgs> AeroGlassCompositionChanged;

        /// <summary>Get determines if AeroGlass is enabled on the desktop. Set enables/disables AreoGlass on the desktop.</summary>
        public static bool AeroGlassCompositionEnabled
        {
            set => DesktopWindowManagerNativeMethods.DwmEnableComposition(
                    value ? CompositionEnable.Enable : CompositionEnable.Disable);
            get => DesktopWindowManagerNativeMethods.DwmIsCompositionEnabled();
        }

        /// <summary>Excludes a UI element from the AeroGlass frame.</summary>
        /// <param name="element">The element to exclude.</param>
        /// <remarks>
        /// Many non-WPF rendered controls (i.e., the ExplorerBrowser control) will not render properly on top of an AeroGlass frame.
        /// </remarks>
        public void ExcludeElementFromAeroGlass(FrameworkElement element)
        {
            if (AeroGlassCompositionEnabled && element != null)
            {
                // calculate total size of window nonclient area
                var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
                DesktopWindowManagerNativeMethods.GetWindowRect(hwndSource.Handle, out var windowRect);
                DesktopWindowManagerNativeMethods.GetClientRect(hwndSource.Handle, out var clientRect);
                var nonClientSize = new Size(
                        windowRect.Right - windowRect.Left - (double)(clientRect.Right - clientRect.Left),
                        windowRect.Bottom - windowRect.Top - (double)(clientRect.Bottom - clientRect.Top));

                // calculate size of element relative to nonclient area
                var transform = element.TransformToAncestor(this);
                var topLeftFrame = transform.Transform(new Point(0, 0));
                var bottomRightFrame = transform.Transform(new Point(
                            element.ActualWidth + nonClientSize.Width,
                            element.ActualHeight + nonClientSize.Height));

                // Create a margin structure
                var margins = new Margins
                {
                    LeftWidth = (int)topLeftFrame.X,
                    RightWidth = (int)(ActualWidth - bottomRightFrame.X),
                    TopHeight = (int)(topLeftFrame.Y),
                    BottomHeight = (int)(ActualHeight - bottomRightFrame.Y)
                };

                // Extend the Frame into client area
                DesktopWindowManagerNativeMethods.DwmExtendFrameIntoClientArea(windowHandle, ref margins);
            }
        }

        /// <summary>Resets the AeroGlass exclusion area.</summary>
        public void ResetAeroGlass()
        {
            var margins = new Margins(true);
            DesktopWindowManagerNativeMethods.DwmExtendFrameIntoClientArea(windowHandle, ref margins);
        }

        /// <summary>Makes the background of current window transparent from both Wpf and Windows Perspective</summary>
        public void SetAeroGlassTransparency()
        {
            // Set the Background to transparent from Win32 perpective
            HwndSource.FromHwnd(windowHandle).CompositionTarget.BackgroundColor = System.Windows.Media.Colors.Transparent;

            // Set the Background to transparent from WPF perpective
            Background = Brushes.Transparent;
        }

        /// <summary>
        /// OnSourceInitialized Override SourceInitialized to initialize windowHandle for this window. A valid windowHandle is available only
        /// after the sourceInitialized is completed
        /// </summary>
        /// <param name="e">EventArgs</param>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var interopHelper = new WindowInteropHelper(this);
            windowHandle = interopHelper.Handle;

            // add Window Proc hook to capture DWM messages
            var source = HwndSource.FromHwnd(windowHandle);
            source.AddHook(new HwndSourceHook(WndProc));

            ResetAeroGlass();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == DWMMessages.WM_DWMCOMPOSITIONCHANGED
                || msg == DWMMessages.WM_DWMNCRENDERINGCHANGED)
            {
                if (AeroGlassCompositionChanged != null)
                {
                    AeroGlassCompositionChanged.Invoke(this,
                        new AeroGlassCompositionChangedEventArgs(AeroGlassCompositionEnabled));
                }

                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}