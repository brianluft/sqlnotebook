using System;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.ShellExtensions.Interop
{
    internal enum SetWindowPositionInsertAfter
    {
        NoTopMost = -2,
        TopMost = -1,
        Top = 0,
        Bottom = 1
    }

    [Flags]
    internal enum SetWindowPositionOptions
    {
        AsyncWindowPos = 0x4000,
        DeferErase = 0x2000,
        DrawFrame = FrameChanged,
        FrameChanged = 0x0020,  // The frame changed: send WM_NCCALCSIZE
        HideWindow = 0x0080,
        NoActivate = 0x0010,
        CoCopyBits = 0x0100,
        NoMove = 0x0002,
        NoOwnerZOrder = 0x0200,  // Don't do owner Z ordering
        NoRedraw = 0x0008,
        NoResposition = NoOwnerZOrder,
        NoSendChanging = 0x0400,  // Don't send WM_WINDOWPOSCHANGING
        NoSize = 0x0001,
        NoZOrder = 0x0004,
        ShowWindow = 0x0040
    }

    internal static class HandlerNativeMethods
    {
        internal static readonly Guid IInitializeWithFileGuid = new Guid("b7d14566-0509-4cce-a71f-0a554233bd9b");

        internal static readonly Guid IInitializeWithItemGuid = new Guid("7f73be3f-fb79-493c-a6c7-7ee14e245841");

        internal static readonly Guid IInitializeWithStreamGuid = new Guid("b824b49d-22ac-4161-ac8a-9916e8fa3f7f");

        internal static readonly Guid IMarshalGuid = new Guid("00000003-0000-0000-C000-000000000046");

        [DllImport("user32.dll")]
        internal static extern void SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            SetWindowPositionOptions flags);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeColorRef
    {
        public uint Dword { get; set; }
    }

    /// <summary>Class for marshaling to native LogFont struct</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class LogFont
    {
        /// <summary>Font height</summary>
        public int Height { get; set; }

        /// <summary>Font width</summary>
        public int Width { get; set; }

        /// <summary>Font escapement</summary>
        public int Escapement { get; set; }

        /// <summary>Font orientation</summary>
        public int Orientation { get; set; }

        /// <summary>Font weight</summary>
        public int Weight { get; set; }

        /// <summary>Font italic</summary>
        public byte Italic { get; set; }

        /// <summary>Font underline</summary>
        public byte Underline { get; set; }

        /// <summary>Font strikeout</summary>
        public byte Strikeout { get; set; }

        /// <summary>Font character set</summary>
        public byte CharacterSet { get; set; }

        /// <summary>Font out precision</summary>
        public byte OutPrecision { get; set; }

        /// <summary>Font clip precision</summary>
        public byte ClipPrecision { get; set; }

        /// <summary>Font quality</summary>
        public byte Quality { get; set; }

        /// <summary>Font pitch and family</summary>
        public byte PitchAndFamily { get; set; }

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        private string faceName = string.Empty;

        /// <summary>Font face name</summary>
        public string FaceName { get => faceName; set => faceName = value; }

        internal LogFont(in Shell.Interop.LogFont lf)
        {
            CharacterSet = lf.charSet;
            ClipPrecision = lf.clipPrecision;
            Escapement = lf.escapement;
            Height = lf.height;
            Italic = lf.italic;
            FaceName = lf.lfFaceName;
            Orientation = lf.orientation;
            OutPrecision = lf.outPrecision;
            PitchAndFamily = lf.pitchAndFamily;
            Quality = lf.quality;
            Strikeout = lf.strikeOut;
            Underline = lf.underline;
            Weight = lf.weight;
            Width = lf.width;
        }
    }
}