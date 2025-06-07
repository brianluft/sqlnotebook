//Copyright (c) Microsoft Corporation.  All rights reserved.

using System.ComponentModel;

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>Data associated with <see cref="TaskDialog.Closing"/> event.</summary>
    public class TaskDialogClosingEventArgs : CancelEventArgs
    {
        private string customButton;
        private TaskDialogResult taskDialogResult;

        /// <summary>Gets or sets the text of the custom button that was clicked.</summary>
        public string CustomButton
        {
            get => customButton;
            set => customButton = value;
        }

        /// <summary>Gets or sets the standard button that was clicked.</summary>
        public TaskDialogResult TaskDialogResult
        {
            get => taskDialogResult;
            set => taskDialogResult = value;
        }
    }
}