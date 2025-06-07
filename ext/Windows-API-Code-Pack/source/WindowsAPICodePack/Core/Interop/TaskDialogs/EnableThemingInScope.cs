using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// http://support.microsoft.com/kb/830033
    /// <devdoc>
    /// This class is intended to use with the C# 'using' statement in to activate an activation context for turning on visual theming at the
    /// beginning of a scope, and have it automatically deactivated when the scope is exited.
    /// </devdoc>

    [SuppressUnmanagedCodeSecurity]
    internal class EnableThemingInScope : IDisposable
    {
        private const int ACTCTX_FLAG_ASSEMBLY_DIRECTORY_VALID = 0x004;

        private static bool contextCreationSucceeded = false;

        private static ACTCTX enableThemingActivationContext;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private static IntPtr hActCtx;

        // Private data
        private UIntPtr cookie;

        public EnableThemingInScope(bool enable)
        {
            cookie = UIntPtr.Zero;
            if (enable && OSFeature.Feature.IsPresent(OSFeature.Themes))
            {
                if (EnsureActivateContextCreated())
                {
                    if (!ActivateActCtx(hActCtx, out cookie))
                    {
                        // Be sure cookie always zero if activation failed
                        cookie = UIntPtr.Zero;
                    }
                }
            }
        }

        ~EnableThemingInScope()
        {
            Dispose();
        }

        void IDisposable.Dispose()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        [DllImport("Kernel32.dll")]
        private static extern bool ActivateActCtx(IntPtr hActCtx, out UIntPtr lpCookie);

        // All the pinvoke goo...
        [DllImport("Kernel32.dll")]
        private static extern IntPtr CreateActCtx(ref ACTCTX actctx);

        [DllImport("Kernel32.dll")]
        private static extern bool DeactivateActCtx(uint dwFlags, UIntPtr lpCookie);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity")]
        private static bool EnsureActivateContextCreated()
        {
            lock (typeof(EnableThemingInScope))
            {
                if (!contextCreationSucceeded)
                {
                    // Pull manifest from the .NET Framework install directory

                    string assemblyLoc = null;

                    var fiop = new FileIOPermission(PermissionState.None)
                    {
                        AllFiles = FileIOPermissionAccess.PathDiscovery
                    };
                    fiop.Assert();
                    try
                    {
                        assemblyLoc = typeof(object).Assembly.Location;
                    }
                    finally
                    {
                        CodeAccessPermission.RevertAssert();
                    }

                    string manifestLoc = null;
                    string installDir = null;
                    if (assemblyLoc != null)
                    {
                        installDir = Path.GetDirectoryName(assemblyLoc);
                        const string manifestName = "XPThemes.manifest";
                        manifestLoc = Path.Combine(installDir, manifestName);
                    }

                    if (manifestLoc != null && installDir != null)
                    {
                        enableThemingActivationContext = new ACTCTX
                        {
                            cbSize = Marshal.SizeOf(typeof(ACTCTX)),
                            lpSource = manifestLoc,

                            // Set the lpAssemblyDirectory to the install directory to prevent Win32 Side by Side from looking for comctl32
                            // in the application directory, which could cause a bogus dll to be placed there and open a security hole.
                            lpAssemblyDirectory = installDir,
                            dwFlags = ACTCTX_FLAG_ASSEMBLY_DIRECTORY_VALID
                        };

                        // Note this will fail gracefully if file specified by manifestLoc doesn't exist.
                        hActCtx = CreateActCtx(ref enableThemingActivationContext);
                        contextCreationSucceeded = (hActCtx != new IntPtr(-1));
                    }
                }

                // If we return false, we'll try again on the next call into EnsureActivateContextCreated(), which is fine.
                return contextCreationSucceeded;
            }
        }

        private void Dispose()
        {
            if (cookie != UIntPtr.Zero)
            {
                try
                {
                    if (DeactivateActCtx(0, cookie))
                    {
                        // deactivation succeeded...
                        cookie = UIntPtr.Zero;
                    }
                }
                catch (SEHException)
                {
                    //Hopefully solved this exception
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ACTCTX
        {
            public int cbSize;
            public uint dwFlags;
            public string lpSource;
            public ushort wProcessorArchitecture;
            public ushort wLangId;
            public string lpAssemblyDirectory;
            public string lpResourceName;
            public string lpApplicationName;
        }
    }
}