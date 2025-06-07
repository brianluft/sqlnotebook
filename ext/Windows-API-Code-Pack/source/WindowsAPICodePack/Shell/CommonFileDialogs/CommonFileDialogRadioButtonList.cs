//Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.Shell.Resources;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Markup;

namespace Microsoft.WindowsAPICodePack.Dialogs.Controls
{
    /// <summary>Represents a radio button list for the Common File Dialog.</summary>
    [ContentProperty("Items")]
    public class CommonFileDialogRadioButtonList : CommonFileDialogControl, ICommonFileDialogIndexedControls
    {
        private readonly Collection<CommonFileDialogRadioButtonListItem> items = new Collection<CommonFileDialogRadioButtonListItem>();
        private int selectedIndex = -1;

        /// <summary>Creates a new instance of this class.</summary>
        public CommonFileDialogRadioButtonList() { }

        /// <summary>Creates a new instance of this class with the specified name.</summary>
        /// <param name="name">The name of this control.</param>
        public CommonFileDialogRadioButtonList(string name) : base(name, string.Empty) { }

        /// <summary>Occurs when the user changes the SelectedIndex.</summary>
        /// <remarks>
        /// By initializing the SelectedIndexChanged event with an empty delegate, we can skip the test to determine if the
        /// SelectedIndexChanged is null. test.
        /// </remarks>
        public event EventHandler SelectedIndexChanged = delegate { };

        /// <summary>Gets the collection of CommonFileDialogRadioButtonListItem objects</summary>
        public Collection<CommonFileDialogRadioButtonListItem> Items => items;

        /// <summary>Gets or sets the current index of the selected item.</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                // Don't update this property if it hasn't changed
                if (selectedIndex == value) { return; }

                // If the native dialog has not been created yet
                if (HostingDialog == null)
                {
                    selectedIndex = value;
                }
                else if (value >= 0 && value < items.Count)
                {
                    selectedIndex = value;
                    ApplyPropertyChange("SelectedIndex");
                }
                else
                {
                    throw new IndexOutOfRangeException(LocalizedMessages.RadioButtonListIndexOutOfBounds);
                }
            }
        }

        /// <summary>Occurs when the user changes the SelectedIndex.</summary>
        /// <remarks>
        /// Because this method is defined in an interface, we can either have it as public, or make it private and explicitly implement
        /// (like below). Making it public doesn't really help as its only internal (but can't have this internal because of the interface)
        /// </remarks>
        void ICommonFileDialogIndexedControls.RaiseSelectedIndexChangedEvent()
        {
            // Make sure that this control is enabled and has a specified delegate
            if (Enabled) { SelectedIndexChanged(this, EventArgs.Empty); }
        }

        /// <summary>Attach the RadioButtonList control to the dialog object</summary>
        /// <param name="dialog">The target dialog</param>
        internal override void Attach(IFileDialogCustomize dialog)
        {
            Debug.Assert(dialog != null, "CommonFileDialogRadioButtonList.Attach: dialog parameter can not be null");

            // Add the radio button list control
            dialog.AddRadioButtonList(Id);

            // Add the radio button list items
            for (var index = 0; index < items.Count; index++)
            {
                dialog.AddControlItem(Id, index, items[index].Text);
            }

            // Set the currently selected item
            if (selectedIndex >= 0 && selectedIndex < items.Count)
            {
                dialog.SetSelectedControlItem(Id, selectedIndex);
            }
            else if (selectedIndex != -1)
            {
                throw new IndexOutOfRangeException(LocalizedMessages.RadioButtonListIndexOutOfBounds);
            }

            // Sync unmanaged properties with managed properties
            SyncUnmanagedProperties();
        }
    }

    /// <summary>Represents a list item for the CommonFileDialogRadioButtonList object.</summary>
    public class CommonFileDialogRadioButtonListItem
    {
        /// <summary>Creates a new instance of this class.</summary>
        public CommonFileDialogRadioButtonListItem() : this(string.Empty) { }

        /// <summary>Creates a new instance of this class with the specified text.</summary>
        /// <param name="text">The string that you want to display for this list item.</param>
        public CommonFileDialogRadioButtonListItem(string text) => Text = text;

        /// <summary>Gets or sets the string that will be displayed for this list item.</summary>
        public string Text { get; set; }
    }
}