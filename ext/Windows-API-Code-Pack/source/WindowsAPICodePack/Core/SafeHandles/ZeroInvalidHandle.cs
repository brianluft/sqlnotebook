//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;

namespace MS.WindowsAPICodePack.Internal
{
    /// <summary>Base class for Safe handles with Null IntPtr as invalid</summary>
    public abstract class ZeroInvalidHandle : SafeHandle
    {
        /// <summary>Default constructor</summary>
        protected ZeroInvalidHandle()
            : base(IntPtr.Zero, true)
        {
        }

        /// <summary>Determines if this is a valid handle</summary>
        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}