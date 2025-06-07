//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>Identifies the default button that can be set via TaskDialog.</summary>
    public enum TaskDialogDefaultButton
    {
        /// <summary>No default button.</summary>
        None = 0,

        /// <summary>An "OK" button.</summary>
        Ok = 1,

        /// <summary>A "Yes" button.</summary>
        Yes = 6,

        /// <summary>A "No" button.</summary>
        No = 7,

        /// <summary>A "Cancel" button.</summary>
        Cancel = 2,

        /// <summary>A "Retry" button.</summary>
        Retry = 4,

        /// <summary>A "Close" button.</summary>
        Close = 8
    }
}