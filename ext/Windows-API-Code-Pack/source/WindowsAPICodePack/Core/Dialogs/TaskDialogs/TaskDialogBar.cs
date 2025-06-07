//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>Defines a common class for all task dialog bar controls, such as the progress and marquee bars.</summary>
    public class TaskDialogBar : TaskDialogControl
    {
        private TaskDialogProgressBarState state;

        /// <summary>Creates a new instance of this class.</summary>
        public TaskDialogBar() { }

        /// <summary>Creates a new instance of this class with the specified name.</summary>
        /// <param name="name">The name for this control.</param>
        protected TaskDialogBar(string name) : base(name) { }

        /// <summary>Gets or sets the state of the progress bar.</summary>
        public TaskDialogProgressBarState State
        {
            get => state;
            set
            {
                CheckPropertyChangeAllowed("State");
                state = value;
                ApplyPropertyChange("State");
            }
        }

        /// <summary>Resets the state of the control to normal.</summary>
        protected internal virtual void Reset() => state = TaskDialogProgressBarState.Normal;
    }
}