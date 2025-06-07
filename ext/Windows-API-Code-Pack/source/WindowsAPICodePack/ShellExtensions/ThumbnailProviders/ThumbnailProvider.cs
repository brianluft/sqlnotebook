using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.Interop;
using Microsoft.WindowsAPICodePack.ShellExtensions.Resources;
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Drawing;

/* Unmerged change from project 'ShellExtensions (net452)'
Before:
using Microsoft.WindowsAPICodePack.Taskbar;
After:
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
*/

/* Unmerged change from project 'ShellExtensions (net462)'
Before:
using Microsoft.WindowsAPICodePack.Taskbar;
After:
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
*/

/* Unmerged change from project 'ShellExtensions (net472)'
Before:
using Microsoft.WindowsAPICodePack.Taskbar;
After:
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Drawing;
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
    /// This is the base class for all thumbnail providers and provides their basic functionality. To create a custom thumbnail provider a
    /// class must derive from this, use the <see cref="ThumbnailProviderAttribute"/>, and implement 1 or more of the following interfaces:
    /// <see cref="IThumbnailFromStream"/>, <see cref="IThumbnailFromShellObject"/>, <see cref="IThumbnailFromFile"/>.
    /// </summary>
    public abstract class ThumbnailProvider : IThumbnailProvider, ICustomQueryInterface, IDisposable,
        IInitializeWithStream, IInitializeWithItem, IInitializeWithFile
    {
        private FileInfo _info = null;

        private ShellObject _shellObject = null;

        private StorageStream _stream = null;

        /// <summary>Finalizer for the thumbnail provider.</summary>
        ~ThumbnailProvider()
        {
            Dispose(false);
        }

        /// <summary>Disposes the thumbnail provider.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Sets the AlphaType of the generated thumbnail. Override this method in a derived class to change the thumbnails AlphaType,
        /// default is Unknown.
        /// </summary>
        /// <returns>ThumnbailAlphaType</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public virtual ThumbnailAlphaType GetThumbnailAlphaType() => ThumbnailAlphaType.Unknown;

        CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;

            // Forces COM to not use the managed (free threaded) marshaler
            if (iid == Interop.HandlerNativeMethods.IMarshalGuid)
            {
                return CustomQueryInterfaceResult.Failed;
            }

            if ((iid == Interop.HandlerNativeMethods.IInitializeWithStreamGuid && !(this is IThumbnailFromStream))
                || (iid == Interop.HandlerNativeMethods.IInitializeWithItemGuid && !(this is IThumbnailFromShellObject))
                || (iid == Interop.HandlerNativeMethods.IInitializeWithFileGuid && !(this is IThumbnailFromFile)))
            {
                return CustomQueryInterfaceResult.Failed;
            }

            return CustomQueryInterfaceResult.NotHandled;
        }

        void IThumbnailProvider.GetThumbnail(uint sideLength, out IntPtr hBitmap, out uint alphaType)
        {
            using (var map = GetBitmap((int)sideLength))
            {
                hBitmap = map.GetHbitmap();
            }
            alphaType = (uint)GetThumbnailAlphaType();
        }

        void IInitializeWithStream.Initialize(System.Runtime.InteropServices.ComTypes.IStream stream, Shell.AccessModes fileMode) => _stream = new StorageStream(stream, fileMode != Shell.AccessModes.ReadWrite);

        void IInitializeWithItem.Initialize(object shellItem, Shell.AccessModes accessMode) => _shellObject = ShellObjectFactory.Create((IShellItem)shellItem);

        void IInitializeWithFile.Initialize(string filePath, Shell.AccessModes fileMode) => _info = new FileInfo(filePath);

        /// <summary>Disploses the thumbnail provider.</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _stream != null)
            {
                _stream.Dispose();
            }
        }

        /// <summary>Called when the assembly is registered via RegAsm.</summary>
        /// <param name="registerType">Type to be registered.</param>
        [ComRegisterFunction]
        private static void Register(Type registerType)
        {
            if (registerType != null && registerType.IsSubclassOf(typeof(ThumbnailProvider)))
            {
                var attributes = registerType.GetCustomAttributes(typeof(ThumbnailProviderAttribute), true);
                if (attributes != null && attributes.Length == 1)
                {
                    var attribute = attributes[0] as ThumbnailProviderAttribute;
                    ThrowIfInvalid(registerType, attribute);
                    RegisterThumbnailHandler(registerType.GUID.ToString("B"), attribute);
                }
            }
        }

        private static void RegisterThumbnailHandler(string guid, ThumbnailProviderAttribute attribute)
        {
            // set process isolation
            using (var clsidKey = Registry.ClassesRoot.OpenSubKey("CLSID"))
            using (var guidKey = clsidKey.OpenSubKey(guid, true))
            {
                guidKey.SetValue("DisableProcessIsolation", attribute.DisableProcessIsolation ? 1 : 0, RegistryValueKind.DWord);

                using (var inproc = guidKey.OpenSubKey("InprocServer32", true))
                {
                    inproc.SetValue("ThreadingModel", "Apartment", RegistryValueKind.String);
                }
            }

            // register file as an approved extension
            using (var approvedShellExtensions = Registry.LocalMachine.OpenSubKey(
                 @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", true))
            {
                approvedShellExtensions.SetValue(guid, attribute.Name, RegistryValueKind.String);
            }

            // register extension with each extension in the list
            var extensions = attribute.Extensions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var extension in extensions)
            {
                using (var extensionKey = Registry.ClassesRoot.CreateSubKey(extension)) // Create makes it writable
                using (var shellExKey = extensionKey.CreateSubKey("shellex"))
                using (var providerKey = shellExKey.CreateSubKey(Shell.Interop.HandlerNativeMethods.ThumbnailProviderGuid.ToString("B")))
                {
                    providerKey.SetValue(null, guid, RegistryValueKind.String);

                    if (attribute.ThumbnailCutoff == ThumbnailCutoffSize.Square20)
                    {
                        extensionKey.DeleteValue("ThumbnailCutoff", false);
                    }
                    else
                    {
                        extensionKey.SetValue("ThumbnailCutoff", (int)attribute.ThumbnailCutoff, RegistryValueKind.DWord);
                    }

                    if (attribute.TypeOverlay != null)
                    {
                        extensionKey.SetValue("TypeOverlay", attribute.TypeOverlay, RegistryValueKind.String);
                    }

                    if (attribute.ThumbnailAdornment == ThumbnailAdornment.Default)
                    {
                        extensionKey.DeleteValue("Treatment", false);
                    }
                    else
                    {
                        extensionKey.SetValue("Treatment", (int)attribute.ThumbnailAdornment, RegistryValueKind.DWord);
                    }
                }
            }
        }

        private static void ThrowIfInvalid(Type type, ThumbnailProviderAttribute attribute)
        {
            var interfaces = type.GetInterfaces();
            var interfaced = interfaces.Any(x => x == typeof(IThumbnailFromStream));

            if (interfaces.Any(x => x == typeof(IThumbnailFromShellObject) || x == typeof(IThumbnailFromFile)))
            {
                // According to MSDN (http://msdn.microsoft.com/en-us/library/cc144114(v=VS.85).aspx) A thumbnail provider that does not
                // implement IInitializeWithStream must opt out of running in the isolated process. The default behavior of the indexer opts
                // in to process isolation regardless of which interfaces are implemented.
                if (!interfaced && !attribute.DisableProcessIsolation)
                {
                    throw new InvalidOperationException(
                        string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        LocalizedMessages.ThumbnailProviderDisabledProcessIsolation,
                        type.Name));
                }
                interfaced = true;
            }

            if (!interfaced)
            {
                throw new InvalidOperationException(
                        string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        LocalizedMessages.ThumbnailProviderInterfaceNotImplemented,
                        type.Name));
            }
        }

        /// <summary>Called when the assembly is registered via RegAsm.</summary>
        /// <param name="registerType">Type to register.</param>
        [ComUnregisterFunction]
        private static void Unregister(Type registerType)
        {
            if (registerType != null && registerType.IsSubclassOf(typeof(ThumbnailProvider)))
            {
                var attributes = registerType.GetCustomAttributes(typeof(ThumbnailProviderAttribute), true);
                if (attributes != null && attributes.Length == 1)
                {
                    var attribute = attributes[0] as ThumbnailProviderAttribute;
                    UnregisterThumbnailHandler(registerType.GUID.ToString("B"), attribute);
                }
            }
        }

        private static void UnregisterThumbnailHandler(string guid, ThumbnailProviderAttribute attribute)
        {
            var extensions = attribute.Extensions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var extension in extensions)
            {
                using (var extKey = Registry.ClassesRoot.OpenSubKey(extension, true))
                using (var shellexKey = extKey.OpenSubKey("shellex", true))
                {
                    shellexKey.DeleteSubKey(Shell.Interop.HandlerNativeMethods.ThumbnailProviderGuid.ToString("B"), false);

                    extKey.DeleteValue("ThumbnailCutoff", false);
                    extKey.DeleteValue("TypeOverlay", false);
                    extKey.DeleteValue("Treatment", false); // Thumbnail adornment
                }
            }

            using (var approvedShellExtensions = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved", true))
            {
                approvedShellExtensions.DeleteValue(guid, false);
            }
        }

        // Determines which interface should be called to return a bitmap
        private Bitmap GetBitmap(int sideLength)
        {
            IThumbnailFromStream stream;
            IThumbnailFromShellObject shellObject;
            IThumbnailFromFile file;

            if (_stream != null && (stream = this as IThumbnailFromStream) != null)
            {
                return stream.ConstructBitmap(_stream, sideLength);
            }
            if (_shellObject != null && (shellObject = this as IThumbnailFromShellObject) != null)
            {
                return shellObject.ConstructBitmap(_shellObject, sideLength);
            }
            if (_info != null && (file = this as IThumbnailFromFile) != null)
            {
                return file.ConstructBitmap(_info, sideLength);
            }

            throw new InvalidOperationException(
                string.Format(System.Globalization.CultureInfo.InvariantCulture,
                LocalizedMessages.ThumbnailProviderInterfaceNotImplemented,
                GetType().Name));
        }
    }
}