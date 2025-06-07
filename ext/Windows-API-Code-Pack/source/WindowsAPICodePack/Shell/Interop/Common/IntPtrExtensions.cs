using System;
using System.
/* Unmerged change from project 'Shell (net452)'
Before:
using System.Text;
using System.Runtime.InteropServices;
After:
using System.Runtime.InteropServices;
using System.Text;
*/

/* Unmerged change from project 'Shell (net462)'
Before:
using System.Text;
using System.Runtime.InteropServices;
After:
using System.Runtime.InteropServices;
using System.Text;
*/

/* Unmerged change from project 'Shell (net472)'
Before:
using System.Text;
using System.Runtime.InteropServices;
After:
using System.Runtime.InteropServices;
using System.Text;
*/
Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.Shell
{
    internal static class IntPtrExtensions
    {
        public static T MarshalAs<T>(this IntPtr ptr) => (T)Marshal.PtrToStructure(ptr, typeof(T));
    }
}