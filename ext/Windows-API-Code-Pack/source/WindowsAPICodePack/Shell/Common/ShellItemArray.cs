//Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MS.WindowsAPICodePack.Internal;
using System;
using System.Collections.Generic;

namespace Microsoft.WindowsAPICodePack.Shell
{
    internal class ShellItemArray : IShellItemArray
    {
        private readonly List<IShellItem> shellItemsList = new List<IShellItem>();

        internal ShellItemArray(IShellItem[] shellItems) => shellItemsList.AddRange(shellItems);

        public HResult BindToHandler(IntPtr pbc, ref Guid rbhid, ref Guid riid, out IntPtr ppvOut) => throw new NotSupportedException();

        public HResult EnumItems(out IntPtr ppenumShellItems) => throw new NotSupportedException();

        public HResult GetAttributes(ShellNativeMethods.ShellItemAttributeOptions dwAttribFlags, ShellNativeMethods.ShellFileGetAttributesOptions sfgaoMask, out ShellNativeMethods.ShellFileGetAttributesOptions psfgaoAttribs) => throw new NotSupportedException();

        public HResult GetCount(out uint pdwNumItems)
        {
            pdwNumItems = (uint)shellItemsList.Count;
            return HResult.Ok;
        }

        public HResult GetItemAt(uint dwIndex, out IShellItem ppsi)
        {
            var index = (int)dwIndex;

            if (index < shellItemsList.Count)
            {
                ppsi = shellItemsList[index];
                return HResult.Ok;
            }
            else
            {
                ppsi = null;
                return HResult.Fail;
            }
        }

        public HResult GetPropertyDescriptionList(ref PropertyKey keyType, ref Guid riid, out IntPtr ppv) => throw new NotSupportedException();

        public HResult GetPropertyStore(int Flags, ref Guid riid, out IntPtr ppv) => throw new NotSupportedException();
    }
}