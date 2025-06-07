using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.Interop;
using Microsoft.WindowsAPICodePack.ShellExtensions.Resources;
using MS.WindowsAPICodePack.Internal;
using System;
using System.Diagnostics;

/* Unmerged change from project 'ShellExtensions (net452)'
Before:
using MS.WindowsAPICodePack.Internal;
After:
using MS.WindowsAPICodePack.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
*/

/* Unmerged change from project 'ShellExtensions (net462)'
Before:
using MS.WindowsAPICodePack.Internal;
After:
using MS.WindowsAPICodePack.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
*/

/* Unmerged change from project 'ShellExtensions (net472)'
Before:
using MS.WindowsAPICodePack.Internal;
After:
using MS.WindowsAPICodePack.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
*/

using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.ShellExtensions
{
    /// <summary>
    /// This is the base class for all preview handlers and provides their basic functionality. To create a custom preview handler a class
    /// must derive from this, use the <see cref="PreviewHandlerAttribute"/>, and implement 1 or more of the following interfaces:
    /// <see cref="IPreviewFromStream"/>, <see cref="IPreviewFromShellObject"/>, <see cref="IPreviewFromFile"/>.
    /// </summary>
    public abstract class PreviewHandler : ICustomQueryInterface, IPreviewHandler, IPreviewHandlerVisuals,
        IOleWindow, IObjectWithSite, IInitializeWithStream, IInitializeWithItem, IInitializeWithFile
    {
        private IPreviewHandlerFrame _frame;
        private bool _isPreviewShowing;
        private IntPtr _parentHwnd;

        /// <summary>Gets whether the preview is currently showing</summary>
        public bool IsPreviewShowing => _isPreviewShowing;

        /// <summary>This should return the window handle to be displayed in the Preview.</summary>
        protected abstract IntPtr Handle { get; }

        void IOleWindow.ContextSensitiveHelp(bool fEnterMode) =>
                    // Preview handlers don't support context sensitive help. (As far as I know.)
                    throw new NotImplementedException();

        void IPreviewHandler.DoPreview()
        {
            _isPreviewShowing = true;
            try
            {
                Initialize();
            }
            catch (Exception exc)
            {
                HandleInitializeException(exc);
            }
        }

        CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;
            // Forces COM to not use the managed (free threaded) marshaler
            if (iid == Interop.HandlerNativeMethods.IMarshalGuid)
            {
                return CustomQueryInterfaceResult.Failed;
            }

            if ((iid == Interop.HandlerNativeMethods.IInitializeWithStreamGuid && !(this is IPreviewFromStream))
                || (iid == Interop.HandlerNativeMethods.IInitializeWithItemGuid && !(this is IPreviewFromShellObject))
                || (iid == Interop.HandlerNativeMethods.IInitializeWithFileGuid && !(this is IPreviewFromFile)))
            {
                return CustomQueryInterfaceResult.Failed;
            }

            return CustomQueryInterfaceResult.NotHandled;
        }

        void IObjectWithSite.GetSite(ref Guid riid, out object ppvSite) => ppvSite = _frame;

        void IOleWindow.GetWindow(out IntPtr phwnd) => phwnd = Handle;

        void IInitializeWithStream.Initialize(System.Runtime.InteropServices.ComTypes.IStream stream, Shell.AccessModes fileMode)
        {
            var preview = this as IPreviewFromStream;
            if (preview == null)
            {
                throw new InvalidOperationException(
                    string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    LocalizedMessages.PreviewHandlerUnsupportedInterfaceCalled,
                    "IPreviewFromStream"));
            }
            using (var storageStream = new StorageStream(stream, fileMode != Shell.AccessModes.ReadWrite))
            {
                preview.Load(storageStream);
            }
        }

        void IInitializeWithItem.Initialize(object shellItem, Shell.AccessModes accessMode)
        {
            var preview = this as IPreviewFromShellObject;
            if (preview == null)
            {
                throw new InvalidOperationException(
                    string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    LocalizedMessages.PreviewHandlerUnsupportedInterfaceCalled,
                    "IPreviewFromShellObject"));
            }
            using (var shellObject = Shell.ShellObjectFactory.Create((IShellItem)shellItem))
            {
                preview.Load(shellObject);
            }
        }

        void IInitializeWithFile.Initialize(string filePath, Shell.AccessModes fileMode)
        {
            var preview = this as IPreviewFromFile;
            if (preview == null)
            {
                throw new InvalidOperationException(
                    string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    LocalizedMessages.PreviewHandlerUnsupportedInterfaceCalled,
                    "IPreviewFromFile"));
            }
            preview.Load(new FileInfo(filePath));
        }

        void IPreviewHandler.QueryFocus(out IntPtr phwnd) => phwnd = Shell.Interop.HandlerNativeMethods.GetFocus();

        void IPreviewHandlerVisuals.SetBackgroundColor(COLORREF color) => SetBackground((int)color.Dword);

        void IPreviewHandlerVisuals.SetFont(ref Shell.Interop.LogFont plf) => SetFont(new Interop.LogFont(plf));

        void IPreviewHandler.SetRect(ref RECT rect) => UpdateBounds(NativeRect.FromRECT(rect));

        void IObjectWithSite.SetSite(object pUnkSite) => _frame = pUnkSite as IPreviewHandlerFrame;

        void IPreviewHandlerVisuals.SetTextColor(COLORREF color) => SetForeground((int)color.Dword);

        void IPreviewHandler.SetWindow(IntPtr hwnd, ref RECT rect)
        {
            _parentHwnd = hwnd;
            UpdateBounds(NativeRect.FromRECT(rect));
            SetParentHandle(_parentHwnd);
        }

        HResult IPreviewHandler.TranslateAccelerator(ref MSG pmsg) => _frame != null ? _frame.TranslateAccelerator(ref pmsg) : HResult.False;

        void IPreviewHandler.Unload()
        {
            Uninitialize();
            _isPreviewShowing = false;
        }

        /// <summary>Called when an exception occurs during the initialization of the control</summary>
        /// <param name="caughtException"></param>
        protected abstract void HandleInitializeException(Exception caughtException);

        /// <summary>Called immediately before the preview is to be shown.</summary>
        protected virtual void Initialize() { }

        /// <summary>Called when a request is received to set or change the background color according to the user's preferences.</summary>
        /// <param name="color">An int representing the ARGB color</param>
        protected abstract void SetBackground(int color);

        void IPreviewHandler.SetFocus() => SetFocus();

        /// <summary>Called to set the font of the preview control according to the user's preferences.</summary>
        /// <param name="font"></param>
        protected abstract void SetFont(Interop.LogFont font);

        /// <summary>Called when a request is received to set or change the foreground color according to the user's preferences.</summary>
        /// <param name="color">An int representing the ARGB color</param>
        protected abstract void SetForeground(int color);

        /// <summary>Called to set the parent of the preview control.</summary>
        /// <param name="handle"></param>
        protected abstract void SetParentHandle(IntPtr handle);

        /// <summary>Called when the preview is no longer shown.</summary>
        protected virtual void Uninitialize() { }

        /// <summary>Called to update the bounds and position of the preview control</summary>
        /// <param name="bounds"></param>
        protected abstract void UpdateBounds(NativeRect bounds);

        /// <summary>Called when the preview control obtains focus.</summary>
        protected abstract void SetFocus();

        /// <summary>Called when the assembly is registered via RegAsm.</summary>
        /// <param name="registerType">Type to register.</param>
        [ComRegisterFunction]
        private static void Register(Type registerType)
        {
            if (registerType != null && registerType.IsSubclassOf(typeof(PreviewHandler)))
            {
                var attrs = registerType.GetCustomAttributes(typeof(PreviewHandlerAttribute), true);
                if (attrs != null && attrs.Length == 1)
                {
                    var attr = attrs[0] as PreviewHandlerAttribute;
                    ThrowIfNotValid(registerType);
                    RegisterPreviewHandler(registerType.GUID, attr);
                }
                else
                {
                    throw new NotSupportedException(
                        string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        LocalizedMessages.PreviewHandlerInvalidAttributes, registerType.Name));
                }
            }
        }

        private static void RegisterPreviewHandler(Guid previewerGuid, PreviewHandlerAttribute attribute)
        {
            var guid = previewerGuid.ToString("B");
            // Create a new prevhost AppID so that this always runs in its own isolated process
            using (var appIdsKey = Registry.ClassesRoot.OpenSubKey("AppID", true))
            using (var appIdKey = appIdsKey.CreateSubKey(attribute.AppId))
            {
                appIdKey.SetValue("DllSurrogate", @"%SystemRoot%\system32\prevhost.exe", RegistryValueKind.ExpandString);
            }

            // Add preview handler to preview handler list
            using (var handlersKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\PreviewHandlers", true))
            {
                handlersKey.SetValue(guid, attribute.Name, RegistryValueKind.String);
            }

            // Modify preview handler registration
            using (var clsidKey = Registry.ClassesRoot.OpenSubKey("CLSID"))
            using (var idKey = clsidKey.OpenSubKey(guid, true))
            {
                idKey.SetValue("DisplayName", attribute.Name, RegistryValueKind.String);
                idKey.SetValue("AppID", attribute.AppId, RegistryValueKind.String);
                idKey.SetValue("DisableLowILProcessIsolation", attribute.DisableLowILProcessIsolation ? 1 : 0, RegistryValueKind.DWord);

                using (var inproc = idKey.OpenSubKey("InprocServer32", true))
                {
                    inproc.SetValue("ThreadingModel", "Apartment", RegistryValueKind.String);
                }
            }

            foreach (var extension in attribute.Extensions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                Trace.WriteLine("Registering extension '" + extension + "' with previewer '" + guid + "'");

                // Set preview handler for specific extension
                using (var extensionKey = Registry.ClassesRoot.CreateSubKey(extension))
                using (var shellexKey = extensionKey.CreateSubKey("shellex"))
                using (var previewKey = shellexKey.CreateSubKey(Shell.Interop.HandlerNativeMethods.PreviewHandlerGuid.ToString("B")))
                {
                    previewKey.SetValue(null, guid, RegistryValueKind.String);
                }
            }
        }

        private static void ThrowIfNotValid(Type type)
        {
            var interfaces = type.GetInterfaces();
            if (!interfaces.Any(x =>
                x == typeof(IPreviewFromStream)
                || x == typeof(IPreviewFromShellObject)
                || x == typeof(IPreviewFromFile)))
            {
                throw new NotImplementedException(
                    string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    LocalizedMessages.PreviewHandlerInterfaceNotImplemented,
                    type.Name));
            }
        }

        /// <summary>Called when the assembly is Unregistered via RegAsm.</summary>
        /// <param name="registerType">Type to unregister</param>
        [ComUnregisterFunction]
        private static void Unregister(Type registerType)
        {
            if (registerType != null && registerType.IsSubclassOf(typeof(PreviewHandler)))
            {
                var attrs = registerType.GetCustomAttributes(typeof(PreviewHandlerAttribute), true);
                if (attrs != null && attrs.Length == 1)
                {
                    var attr = attrs[0] as PreviewHandlerAttribute;
                    UnregisterPreviewHandler(registerType.GUID, attr);
                }
            }
        }

        private static void UnregisterPreviewHandler(Guid previewerGuid, PreviewHandlerAttribute attribute)
        {
            var guid = previewerGuid.ToString("B");
            foreach (var extension in attribute.Extensions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                Trace.WriteLine("Unregistering extension '" + extension + "' with previewer '" + guid + "'");
                using (var shellexKey = Registry.ClassesRoot.OpenSubKey(extension + "\\shellex", true))
                {
                    shellexKey.DeleteSubKey(Shell.Interop.HandlerNativeMethods.PreviewHandlerGuid.ToString(), false);
                }
            }

            using (var appIdsKey = Registry.ClassesRoot.OpenSubKey("AppID", true))
            {
                appIdsKey.DeleteSubKey(attribute.AppId, false);
            }

            using (var classesKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\PreviewHandlers", true))
            {
                classesKey.DeleteValue(guid, false);
            }
        }
    }
}