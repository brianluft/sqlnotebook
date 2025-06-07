//Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.Resources;
using MS.WindowsAPICodePack.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>
    /// Encapsulates a new-to-Vista Win32 TaskDialog window
    /// - a powerful successor to the MessageBox available in previous versions of Windows.
    /// </summary>
    public sealed class TaskDialog : IDialogControlHost, IDisposable
    {
        // Global instance of TaskDialog, to be used by static Show() method. As most parameters of a dialog created via static Show() will
        // have identical parameters, we'll create one TaskDialog and treat it as a NativeTaskDialog generator for all static Show() calls.
        private static TaskDialog staticDialog;

        private readonly DialogControlCollection<TaskDialogControl> controls;

        private List<TaskDialogButtonBase> buttons = new List<TaskDialogButtonBase>();

        private bool cancelable;

        private string caption;

        private string checkBoxText;

        private List<TaskDialogButtonBase> commandLinks = new List<TaskDialogButtonBase>();

        private TaskDialogDefaultButton defaultButton = TaskDialogDefaultButton.None;

        private string detailsCollapsedLabel;

        private bool detailsExpanded;

        private string detailsExpandedLabel;

        private string detailsExpandedText;

        // Dispose pattern - cleans up data and structs for
        // a) any native dialog currently showing, and
        // b) anything else that the outer TaskDialog has.
        private bool disposed;

        private TaskDialogExpandedDetailsLocation expansionMode;

        private bool? footerCheckBoxChecked = null;

        private TaskDialogStandardIcon footerIcon;

        private string footerText;

        private bool hyperlinksEnabled;

        private TaskDialogStandardIcon icon;

        private string instructionText;

        // Main current native dialog.
        private NativeTaskDialog nativeDialog;

        private IntPtr ownerWindow;
        private TaskDialogProgressBar progressBar;
        private List<TaskDialogButtonBase> radioButtons = new List<TaskDialogButtonBase>();
        private TaskDialogStandardButtons standardButtons = TaskDialogStandardButtons.None;

        private TaskDialogStartupLocation startupLocation;

        // Main content (maps to MessageBox's "message").
        private string text;

        /// <summary>TaskDialog Finalizer</summary>
        ~TaskDialog()
        {
            Dispose(false);
        }

        /// <summary>Occurs when the TaskDialog is closing.</summary>
        public event EventHandler<TaskDialogClosingEventArgs> Closing;

        /// <summary>Occurs when a user clicks on Help.</summary>
        public event EventHandler HelpInvoked;

        /// <summary>Occurs when a user clicks a hyperlink.</summary>
        public event EventHandler<TaskDialogHyperlinkClickedEventArgs> HyperlinkClick;

        /// <summary>Occurs when the TaskDialog is opened.</summary>
        public event EventHandler Opened;

        /// <summary>Occurs when a progress bar changes.</summary>
        public event EventHandler<TaskDialogTickEventArgs> Tick;

        /// <summary>Indicates whether this feature is supported on the current platform.</summary>
        public static bool IsPlatformSupported =>
                // We need Windows Vista onwards ...
                CoreHelpers.RunningOnVista;

        /// <summary>Gets or sets a value that determines if Cancelable is set.</summary>
        public bool Cancelable
        {
            get => cancelable;
            set
            {
                ThrowIfDialogShowing(LocalizedMessages.CancelableCannotBeChanged);
                cancelable = value;
            }
        }

        /// <summary>Gets or sets a value that contains the caption text.</summary>
        public string Caption
        {
            get => caption;
            set
            {
                ThrowIfDialogShowing(LocalizedMessages.CaptionCannotBeChanged);
                caption = value;
            }
        }

        /// <summary>Gets a value that contains the TaskDialog controls.</summary>
        public DialogControlCollection<TaskDialogControl> Controls => controls;

        /// <summary>Gets or sets a value that contains the default button.</summary>
        public TaskDialogDefaultButton DefaultButton
        {
            get => defaultButton;
            set
            {
                ThrowIfDialogShowing(LocalizedMessages.DefaultButtonCannotBeChanged);
                defaultButton = value;
            }
        }

        /// <summary>Gets or sets a value that contains the collapsed control text.</summary>
        public string DetailsCollapsedLabel
        {
            get => detailsCollapsedLabel;
            set
            {
                ThrowIfDialogShowing(LocalizedMessages.CollapsedTextCannotBeChanged);
                detailsCollapsedLabel = value;
            }
        }

        /// <summary>Gets or sets a value that determines if the details section is expanded.</summary>
        public bool DetailsExpanded
        {
            get => detailsExpanded;
            set
            {
                ThrowIfDialogShowing(LocalizedMessages.ExpandingStateCannotBeChanged);
                detailsExpanded = value;
            }
        }

        /// <summary>Gets or sets a value that contains the expanded control text.</summary>
        public string DetailsExpandedLabel
        {
            get => detailsExpandedLabel;
            set
            {
                ThrowIfDialogShowing(LocalizedMessages.ExpandedLabelCannotBeChanged);
                detailsExpandedLabel = value;
            }
        }

        /// <summary>Gets or sets a value that contains the expanded text in the details section.</summary>
        public string DetailsExpandedText
        {
            get => detailsExpandedText;
            set
            {
                // Set local value, then update native dialog if showing.
                detailsExpandedText = value;
                if (NativeDialogShowing) { nativeDialog.UpdateExpandedText(detailsExpandedText); }
            }
        }

        /// <summary>Gets or sets a value that contains the expansion mode for this dialog.</summary>
        public TaskDialogExpandedDetailsLocation ExpansionMode
        {
            get => expansionMode;
            set
            {
                ThrowIfDialogShowing(LocalizedMessages.ExpandedDetailsCannotBeChanged);
                expansionMode = value;
            }
        }

        /// <summary>Gets or sets a value that indicates if the footer checkbox is checked.</summary>
        public bool? FooterCheckBoxChecked
        {
            get => footerCheckBoxChecked.GetValueOrDefault(false);
            set
            {
                // Set local value, then update native dialog if showing.
                footerCheckBoxChecked = value;
                if (NativeDialogShowing) { nativeDialog.UpdateCheckBoxChecked(footerCheckBoxChecked.Value); }
            }
        }

        /// <summary>Gets or sets a value that contains the footer check box text.</summary>
        public string FooterCheckBoxText
        {
            get => checkBoxText;
            set
            {
                ThrowIfDialogShowing(LocalizedMessages.CheckBoxCannotBeChanged);
                checkBoxText = value;
            }
        }

        /// <summary>Gets or sets a value that contains the footer icon.</summary>
        public TaskDialogStandardIcon FooterIcon
        {
            get => footerIcon;
            set
            {
                // Set local value, then update native dialog if showing.
                footerIcon = value;
                if (NativeDialogShowing) { nativeDialog.UpdateFooterIcon(footerIcon); }
            }
        }

        /// <summary>Gets or sets a value that contains the footer text.</summary>
        public string FooterText
        {
            get => footerText;
            set
            {
                // Set local value, then update native dialog if showing.
                footerText = value;
                if (NativeDialogShowing) { nativeDialog.UpdateFooterText(footerText); }
            }
        }

        /// <summary>Gets or sets a value that determines if hyperlinks are enabled.</summary>
        public bool HyperlinksEnabled
        {
            get => hyperlinksEnabled;
            set
            {
                ThrowIfDialogShowing(LocalizedMessages.HyperlinksCannotBetSet);
                hyperlinksEnabled = value;
            }
        }

        /// <summary>Gets or sets a value that contains the TaskDialog main icon.</summary>
        public TaskDialogStandardIcon Icon
        {
            get => icon;
            set
            {
                // Set local value, then update native dialog if showing.
                icon = value;
                if (NativeDialogShowing) { nativeDialog.UpdateMainIcon(icon); }
            }
        }

        /// <summary>Gets or sets a value that contains the instruction text.</summary>
        public string InstructionText
        {
            get => instructionText;
            set
            {
                // Set local value, then update native dialog if showing.
                instructionText = value;
                if (NativeDialogShowing) { nativeDialog.UpdateInstruction(instructionText); }
            }
        }

        /// <summary>Gets or sets a value that contains the owner window's handle.</summary>
        public IntPtr OwnerWindowHandle
        {
            get => ownerWindow;
            set
            {
                ThrowIfDialogShowing(LocalizedMessages.OwnerCannotBeChanged);
                ownerWindow = value;
            }
        }

        /// <summary>
        /// Gets or sets the progress bar on the taskdialog. ProgressBar a visual representation of the progress of a long running operation.
        /// </summary>
        public TaskDialogProgressBar ProgressBar
        {
            get => progressBar;
            set
            {
                ThrowIfDialogShowing(LocalizedMessages.ProgressBarCannotBeChanged);
                if (value != null)
                {
                    if (value.HostingDialog != null)
                    {
                        throw new InvalidOperationException(LocalizedMessages.ProgressBarCannotBeHostedInMultipleDialogs);
                    }

                    value.HostingDialog = this;
                }
                progressBar = value;
            }
        }

        /// <summary>Gets or sets a value that contains the standard buttons.</summary>
        public TaskDialogStandardButtons StandardButtons
        {
            get => standardButtons;
            set
            {
                ThrowIfDialogShowing(LocalizedMessages.StandardButtonsCannotBeChanged);
                standardButtons = value;
            }
        }

        /// <summary>Gets or sets a value that contains the startup location.</summary>
        public TaskDialogStartupLocation StartupLocation
        {
            get => startupLocation;
            set
            {
                ThrowIfDialogShowing(LocalizedMessages.StartupLocationCannotBeChanged);
                startupLocation = value;
            }
        }

        /// <summary>Gets or sets a value that contains the message text.</summary>
        public string Text
        {
            get => text;
            set
            {
                // Set local value, then update native dialog if showing.
                text = value;
                if (NativeDialogShowing) { nativeDialog.UpdateText(text); }
            }
        }

        /// <summary>Creates a basic TaskDialog window</summary>
        public TaskDialog()
        {
            CoreHelpers.ThrowIfNotVista();

            // Initialize various data structs.
            controls = new DialogControlCollection<TaskDialogControl>(this);
        }

        private bool NativeDialogShowing => (nativeDialog != null)
                            && (nativeDialog.ShowState == DialogShowState.Showing
                            || nativeDialog.ShowState == DialogShowState.Closing);

        /// <summary>Creates and shows a task dialog with the specified message text.</summary>
        /// <param name="text">The text to display.</param>
        /// <returns>The dialog result.</returns>
        public static TaskDialogResult Show(string text) => ShowCoreStatic(
                text,
                TaskDialogDefaults.MainInstruction,
                TaskDialogDefaults.Caption);

        /// <summary>Creates and shows a task dialog with the specified supporting text and main instruction.</summary>
        /// <param name="text">The supporting text to display.</param>
        /// <param name="instructionText">The main instruction text to display.</param>
        /// <returns>The dialog result.</returns>
        public static TaskDialogResult Show(string text, string instructionText) => ShowCoreStatic(
                text,
                instructionText,
                TaskDialogDefaults.Caption);

        /// <summary>Creates and shows a task dialog with the specified supporting text, main instruction, and dialog caption.</summary>
        /// <param name="text">The supporting text to display.</param>
        /// <param name="instructionText">The main instruction text to display.</param>
        /// <param name="caption">The caption for the dialog.</param>
        /// <returns>The dialog result.</returns>
        public static TaskDialogResult Show(string text, string instructionText, string caption) => ShowCoreStatic(text, instructionText, caption);

        /// <summary>Close TaskDialog</summary>
        /// <exception cref="InvalidOperationException">if TaskDialog is not showing.</exception>
        public void Close()
        {
            if (!NativeDialogShowing)
            {
                throw new InvalidOperationException(LocalizedMessages.TaskDialogCloseNonShowing);
            }

            nativeDialog.NativeClose(TaskDialogResult.Cancel);
            // TaskDialog's own cleanup code - which runs post show - will handle disposal of native dialog.
        }

        /// <summary>Close TaskDialog with a given TaskDialogResult</summary>
        /// <param name="closingResult">TaskDialogResult to return from the TaskDialog.Show() method</param>
        /// <exception cref="InvalidOperationException">if TaskDialog is not showing.</exception>
        public void Close(TaskDialogResult closingResult)
        {
            if (!NativeDialogShowing)
            {
                throw new InvalidOperationException(LocalizedMessages.TaskDialogCloseNonShowing);
            }

            nativeDialog.NativeClose(closingResult);
            // TaskDialog's own cleanup code - which runs post show - will handle disposal of native dialog.
        }

        /// <summary>Dispose TaskDialog Resources</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Dispose TaskDialog Resources</summary>
        /// <param name="disposing">If true, indicates that this is being called via Dispose rather than via the finalizer.</param>
        public void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;

                if (disposing)
                {
                    // Clean up managed resources.
                    if (nativeDialog != null && nativeDialog.ShowState == DialogShowState.Showing)
                    {
                        nativeDialog.NativeClose(TaskDialogResult.Cancel);
                    }

                    buttons = null;
                    radioButtons = null;
                    commandLinks = null;
                }

                // Clean up unmanaged resources SECOND, NTD counts on being closed before being disposed.
                if (nativeDialog != null)
                {
                    nativeDialog.Dispose();
                    nativeDialog = null;
                }

                if (staticDialog != null)
                {
                    staticDialog.Dispose();
                    staticDialog = null;
                }
            }
        }

        /// <summary>Creates and shows a task dialog.</summary>
        /// <returns>The dialog result.</returns>
        public TaskDialogResult Show() => ShowCore();

        // CORE SHOW METHODS: All static Show() calls forward here - it is responsible for retrieving or creating our cached TaskDialog
        // instance, getting it configured, and in turn calling the appropriate instance Show.

        // Called whenever controls have been added or removed.
        void IDialogControlHost.ApplyCollectionChanged() =>
            // If we're showing, we should never get here - the changing notification would have thrown and the property would not have been changed.
            Debug.Assert(!NativeDialogShowing,
                "Collection changed notification received despite show state of dialog");

        // Called when a control currently in the collection has a property changed - this handles propagating the new property values to the
        // Win32 API. If there isn't a way to change the Win32 value, then we should have already screened out the property set in NotifyControlPropertyChanging.
        void IDialogControlHost.ApplyControlPropertyChange(string propertyName, DialogControl control)
        {
            // We only need to apply changes to the native dialog when it actually exists.
            if (NativeDialogShowing)
            {
                TaskDialogButton button;
                TaskDialogRadioButton radioButton;
                if (control is TaskDialogProgressBar)
                {
                    if (!progressBar.HasValidValues)
                    {
                        throw new ArgumentException(LocalizedMessages.TaskDialogProgressBarValueInRange);
                    }

                    switch (propertyName)
                    {
                        case "State":
                            nativeDialog.UpdateProgressBarState(progressBar.State);
                            break;

                        case "Value":
                            nativeDialog.UpdateProgressBarValue(progressBar.Value);
                            break;

                        case "Minimum":
                        case "Maximum":
                            nativeDialog.UpdateProgressBarRange(progressBar.Minimum, progressBar.Maximum);
                            break;

                        default:
                            Debug.Assert(true, "Unknown property being set");
                            break;
                    }
                }
                else if ((button = control as TaskDialogButton) != null)
                {
                    switch (propertyName)
                    {
                        case "ShowElevationIcon":
                            nativeDialog.UpdateElevationIcon(button.Id, button.UseElevationIcon);
                            break;

                        case "Enabled":
                            nativeDialog.UpdateButtonEnabled(button.Id, button.Enabled);
                            break;

                        default:
                            Debug.Assert(true, "Unknown property being set");
                            break;
                    }
                }
                else if ((radioButton = control as TaskDialogRadioButton) != null)
                {
                    switch (propertyName)
                    {
                        case "Enabled":
                            nativeDialog.UpdateRadioButtonEnabled(radioButton.Id, radioButton.Enabled);
                            break;

                        default:
                            Debug.Assert(true, "Unknown property being set");
                            break;
                    }
                }
                else
                {
                    // Do nothing with property change - note that this shouldn't ever happen, we should have either thrown on the changing
                    // event, or we handle above.
                    Debug.Assert(true, "Control property changed notification not handled properly - being ignored");
                }
            }
        }

        // Called whenever controls are being added to or removed from the dialog control collection.
        bool IDialogControlHost.IsCollectionChangeAllowed() =>
            // Only allow additions to collection if dialog is NOT showing.
            !NativeDialogShowing;

        // We're explicitly implementing this interface as the user will never need to know about it or use it directly - it is only for the
        // internal implementation of "pseudo controls" within the dialogs. Called when a control currently in the collection has a property
        // changing - this is basically to screen out property changes that cannot occur while the dialog is showing because the Win32 API
        // has no way for us to propagate the changes until we re-invoke the Win32 call.
        bool IDialogControlHost.IsControlPropertyChangeAllowed(string propertyName, DialogControl control)
        {
            Debug.Assert(control is TaskDialogControl,
                "Property changing for a control that is not a TaskDialogControl-derived type");
            Debug.Assert(propertyName != "Name",
                "Name changes at any time are not supported - public API should have blocked this");

            var canChange = false;

            if (!NativeDialogShowing)
            {
                // Certain properties can't be changed if the dialog is not showing we need a handle created before we can set these...
                switch (propertyName)
                {
                    case "Enabled":
                        canChange = false;
                        break;

                    default:
                        canChange = true;
                        break;
                }
            }
            else
            {
                // If the dialog is showing, we can only allow some properties to change.
                switch (propertyName)
                {
                    // Properties that CAN'T be changed while dialog is showing.
                    case "Text":
                    case "Default":
                        canChange = false;
                        break;

                    // Properties that CAN be changed while dialog is showing.
                    case "ShowElevationIcon":
                    case "Enabled":
                        canChange = true;
                        break;

                    default:
                        Debug.Assert(true, "Unknown property name coming through property changing handler");
                        break;
                }
            }
            return canChange;
        }

        // All Raise*() methods are called by the NativeTaskDialog when various pseudo-controls are triggered.
        internal void RaiseButtonClickEvent(int id)
        {
            // First check to see if the ID matches a custom button.
            var button = GetButtonForId(id);

            // If a custom button was found, raise the event - if not, it's a standard button, and we don't support custom event handling for
            // the standard buttons
            if (button != null) { button.RaiseClickEvent(); }
        }

        // Gives event subscriber a chance to prevent the dialog from closing, based on the current state of the app and the button used to
        // commit. Note that we don't have full access at this stage to the full dialog state.
        internal int RaiseClosingEvent(int id)
        {
            var handler = Closing;
            if (handler != null)
            {
                TaskDialogButtonBase customButton = null;
                var e = new TaskDialogClosingEventArgs();

                // Try to identify the button - is it a standard one?
                var buttonClicked = MapButtonIdToStandardButton(id);

                // If not, it had better be a custom button...
                if (buttonClicked == TaskDialogStandardButtons.None)
                {
                    customButton = GetButtonForId(id);

                    // ... or we have a problem.
                    if (customButton == null)
                    {
                        throw new InvalidOperationException(LocalizedMessages.TaskDialogBadButtonId);
                    }

                    e.CustomButton = customButton.Name;
                    e.TaskDialogResult = TaskDialogResult.CustomButtonClicked;
                }
                else
                {
                    e.TaskDialogResult = (TaskDialogResult)buttonClicked;
                }

                // Raise the event and determine how to proceed.
                handler(this, e);
                if (e.Cancel) { return (int)HResult.False; }
            }

            // It's okay to let the dialog close.
            return (int)HResult.Ok;
        }

        internal void RaiseHelpInvokedEvent()
        {
            if (HelpInvoked != null) { HelpInvoked(this, EventArgs.Empty); }
        }

        internal void RaiseHyperlinkClickEvent(string link)
        {
            var handler = HyperlinkClick;
            if (handler != null)
            {
                handler(this, new TaskDialogHyperlinkClickedEventArgs(link));
            }
        }

        internal void RaiseOpenedEvent()
        {
            if (Opened != null) { Opened(this, EventArgs.Empty); }
        }

        internal void RaiseTickEvent(int ticks)
        {
            if (Tick != null) { Tick(this, new TaskDialogTickEventArgs(ticks)); }
        }

        private static void ApplyElevatedIcons(NativeTaskDialogSettings settings, List<TaskDialogButtonBase> controls)
        {
            foreach (TaskDialogButton control in controls)
            {
                if (control.UseElevationIcon)
                {
                    if (settings.ElevatedButtons == null) { settings.ElevatedButtons = new List<int>(); }
                    settings.ElevatedButtons.Add(control.Id);
                }
            }
        }

        private static TaskDialogNativeMethods.TaskDialogButton[] BuildButtonStructArray(List<TaskDialogButtonBase> controls)
        {
            TaskDialogNativeMethods.TaskDialogButton[] buttonStructs;
            TaskDialogButtonBase button;

            var totalButtons = controls.Count;
            buttonStructs = new TaskDialogNativeMethods.TaskDialogButton[totalButtons];
            for (var i = 0; i < totalButtons; i++)
            {
                button = controls[i];
                buttonStructs[i] = new TaskDialogNativeMethods.TaskDialogButton(button.Id, button.ToString());
            }
            return buttonStructs;
        }

        // Analyzes the final state of the NativeTaskDialog instance and creates the final TaskDialogResult that will be returned from the
        // public API
        private static TaskDialogResult ConstructDialogResult(NativeTaskDialog native)
        {
            Debug.Assert(native.ShowState == DialogShowState.Closed, "dialog result being constructed for unshown dialog.");

            var result = TaskDialogResult.Cancel;

            var standardButton = MapButtonIdToStandardButton(native.SelectedButtonId);

            // If returned ID isn't a standard button, let's fetch
            if (standardButton == TaskDialogStandardButtons.None)
            {
                result = TaskDialogResult.CustomButtonClicked;
            }
            else { result = (TaskDialogResult)standardButton; }

            return result;
        }

        // Searches list of controls and returns the ID of the default control, or 0 if no default was specified.
        private static int FindDefaultButtonId(List<TaskDialogButtonBase> controls)
        {
            var defaults = controls.FindAll(control => control.Default);

            if (defaults.Count == 1) { return defaults[0].Id; }
            else if (defaults.Count > 1)
            {
                throw new InvalidOperationException(LocalizedMessages.TaskDialogOnlyOneDefaultControl);
            }

            return TaskDialogNativeMethods.NoDefaultButtonSpecified;
        }

        private static TaskDialogStandardButtons MapButtonIdToStandardButton(int id)
        {
            switch ((TaskDialogNativeMethods.TaskDialogCommonButtonReturnIds)id)
            {
                case TaskDialogNativeMethods.TaskDialogCommonButtonReturnIds.Ok:
                    return TaskDialogStandardButtons.Ok;

                case TaskDialogNativeMethods.TaskDialogCommonButtonReturnIds.Cancel:
                    return TaskDialogStandardButtons.Cancel;

                case TaskDialogNativeMethods.TaskDialogCommonButtonReturnIds.Abort:
                    // Included for completeness in API - we can't pass in an Abort standard button.
                    return TaskDialogStandardButtons.None;

                case TaskDialogNativeMethods.TaskDialogCommonButtonReturnIds.Retry:
                    return TaskDialogStandardButtons.Retry;

                case TaskDialogNativeMethods.TaskDialogCommonButtonReturnIds.Ignore:
                    // Included for completeness in API - we can't pass in an Ignore standard button.
                    return TaskDialogStandardButtons.None;

                case TaskDialogNativeMethods.TaskDialogCommonButtonReturnIds.Yes:
                    return TaskDialogStandardButtons.Yes;

                case TaskDialogNativeMethods.TaskDialogCommonButtonReturnIds.No:
                    return TaskDialogStandardButtons.No;

                case TaskDialogNativeMethods.TaskDialogCommonButtonReturnIds.Close:
                    return TaskDialogStandardButtons.Close;

                default:
                    return TaskDialogStandardButtons.None;
            }
        }

        private static TaskDialogResult ShowCoreStatic(
                                                                                                                                    string text,
            string instructionText,
            string caption)
        {
            CoreHelpers.ThrowIfNotVista();

            // If no instance cached yet, create it.
            if (staticDialog == null)
            {
                // New TaskDialog will automatically pick up defaults when a new config structure is created as part of ShowCore().
                staticDialog = new TaskDialog();
            }

            // Set the few relevant properties, and go with the defaults for the others.
            staticDialog.text = text;
            staticDialog.instructionText = instructionText;
            staticDialog.caption = caption;

            return staticDialog.Show();
        }

        private void ApplyControlConfiguration(NativeTaskDialogSettings settings)
        {
            // Deal with progress bars/marquees.
            if (progressBar != null)
            {
                if (progressBar.State == TaskDialogProgressBarState.Marquee)
                {
                    settings.NativeConfiguration.taskDialogFlags |= TaskDialogNativeMethods.TaskDialogOptions.ShowMarqueeProgressBar;
                }
                else
                {
                    settings.NativeConfiguration.taskDialogFlags |= TaskDialogNativeMethods.TaskDialogOptions.ShowProgressBar;
                }
            }

            // Build the native struct arrays that NativeTaskDialog needs - though NTD will handle the heavy lifting marshalling to make sure
            // all the cleanup is centralized there.
            if (buttons.Count > 0 || commandLinks.Count > 0)
            {
                // These are the actual arrays/lists of the structs that we'll copy to the unmanaged heap.
                var sourceList = (buttons.Count > 0 ? buttons : commandLinks);
                settings.Buttons = BuildButtonStructArray(sourceList);

                // Apply option flag that forces all custom buttons to render as command links.
                if (commandLinks.Count > 0)
                {
                    settings.NativeConfiguration.taskDialogFlags |= TaskDialogNativeMethods.TaskDialogOptions.UseCommandLinks;
                }

                // Set default button and add elevation icons to appropriate buttons.
                settings.NativeConfiguration.defaultButtonIndex = FindDefaultButtonId(sourceList);

                ApplyElevatedIcons(settings, sourceList);
            }

            if (radioButtons.Count > 0)
            {
                settings.RadioButtons = BuildButtonStructArray(radioButtons);

                // Set default radio button - radio buttons don't support.
                var defaultRadioButton = FindDefaultButtonId(radioButtons);
                settings.NativeConfiguration.defaultRadioButtonIndex = defaultRadioButton;

                if (defaultRadioButton == TaskDialogNativeMethods.NoDefaultButtonSpecified)
                {
                    settings.NativeConfiguration.taskDialogFlags |= TaskDialogNativeMethods.TaskDialogOptions.NoDefaultRadioButton;
                }
            }
        }

        private void ApplyCoreSettings(NativeTaskDialogSettings settings)
        {
            ApplyGeneralNativeConfiguration(settings.NativeConfiguration);
            ApplyTextConfiguration(settings.NativeConfiguration);
            ApplyOptionConfiguration(settings.NativeConfiguration);
            ApplyControlConfiguration(settings);
        }

        private void ApplyGeneralNativeConfiguration(TaskDialogNativeMethods.TaskDialogConfiguration dialogConfig)
        {
            // If an owner wasn't specifically specified, we'll use the app's main window.
            if (ownerWindow != IntPtr.Zero)
            {
                dialogConfig.parentHandle = ownerWindow;
            }

            // Other miscellaneous sets.
            dialogConfig.mainIcon = new TaskDialogNativeMethods.IconUnion((int)icon);
            dialogConfig.footerIcon = new TaskDialogNativeMethods.IconUnion((int)footerIcon);
            dialogConfig.commonButtons = (TaskDialogNativeMethods.TaskDialogCommonButtons)standardButtons;
            dialogConfig.defaultButtonIndex = (int)defaultButton;
        }

        private void ApplyOptionConfiguration(TaskDialogNativeMethods.TaskDialogConfiguration dialogConfig)
        {
            // Handle options - start with no options set.
            var options = TaskDialogNativeMethods.TaskDialogOptions.None;
            if (cancelable)
            {
                options |= TaskDialogNativeMethods.TaskDialogOptions.AllowCancel;
            }
            if (footerCheckBoxChecked.HasValue && footerCheckBoxChecked.Value)
            {
                options |= TaskDialogNativeMethods.TaskDialogOptions.CheckVerificationFlag;
            }
            if (hyperlinksEnabled)
            {
                options |= TaskDialogNativeMethods.TaskDialogOptions.EnableHyperlinks;
            }
            if (detailsExpanded)
            {
                options |= TaskDialogNativeMethods.TaskDialogOptions.ExpandedByDefault;
            }
            if (Tick != null)
            {
                options |= TaskDialogNativeMethods.TaskDialogOptions.UseCallbackTimer;
            }
            if (startupLocation == TaskDialogStartupLocation.CenterOwner)
            {
                options |= TaskDialogNativeMethods.TaskDialogOptions.PositionRelativeToWindow;
            }

            // Note: no validation required, as we allow this to be set even if there is no expanded information text because that could be
            // added later. Default for Win32 API is to expand into (and after) the content area.
            if (expansionMode == TaskDialogExpandedDetailsLocation.ExpandFooter)
            {
                options |= TaskDialogNativeMethods.TaskDialogOptions.ExpandFooterArea;
            }

            // Finally, apply options to config.
            dialogConfig.taskDialogFlags = options;
        }

        // Builds the actual configuration that the NativeTaskDialog (and underlying Win32 API) expects, by parsing the various control
        // lists, marshalling to the unmanaged heap, etc.
        private void ApplySupplementalSettings(NativeTaskDialogSettings settings)
        {
            if (progressBar != null)
            {
                if (progressBar.State != TaskDialogProgressBarState.Marquee)
                {
                    settings.ProgressBarMinimum = progressBar.Minimum;
                    settings.ProgressBarMaximum = progressBar.Maximum;
                    settings.ProgressBarValue = progressBar.Value;
                    settings.ProgressBarState = progressBar.State;
                }
            }

            if (HelpInvoked != null) { settings.InvokeHelp = true; }
        }

        /// <summary>Sets important text properties.</summary>
        /// <param name="dialogConfig">An instance of a <see cref="TaskDialogNativeMethods.TaskDialogConfiguration"/> object.</param>
        private void ApplyTextConfiguration(TaskDialogNativeMethods.TaskDialogConfiguration dialogConfig)
        {
            // note that nulls or empty strings are fine here.
            dialogConfig.content = text;
            dialogConfig.windowTitle = caption;
            dialogConfig.mainInstruction = instructionText;
            dialogConfig.expandedInformation = detailsExpandedText;
            dialogConfig.expandedControlText = detailsExpandedLabel;
            dialogConfig.collapsedControlText = detailsCollapsedLabel;
            dialogConfig.footerText = footerText;
            dialogConfig.verificationText = checkBoxText;
        }

        // Cleans up data and structs from a single native dialog Show() invocation.
        private void CleanUp()
        {
            // Reset values that would be considered 'volatile' in a given instance.
            if (progressBar != null)
            {
                progressBar.Reset();
            }

            // Clean out sorted control lists - though we don't of course clear the main controls collection, so the controls are still
            // around; we'll resort on next show, since the collection may have changed.
            if (buttons != null) { buttons.Clear(); }
            if (commandLinks != null) { commandLinks.Clear(); }
            if (radioButtons != null) { radioButtons.Clear(); }
            progressBar = null;

            // Have the native dialog clean up the rest.
            if (nativeDialog != null)
            {
                nativeDialog.Dispose();
            }
        }

        // NOTE: we are going to require names be unique across both buttons and radio buttons, even though the Win32 API allows them to be separate.
        private TaskDialogButtonBase GetButtonForId(int id) => (TaskDialogButtonBase)controls.GetControlbyId(id);

        private TaskDialogResult ShowCore()
        {
            TaskDialogResult result;

            try
            {
                // Populate control lists, based on current contents - note we are somewhat late-bound on our control lists, to support XAML scenarios.
                SortDialogControls();

                // First, let's make sure it even makes sense to try a show.
                ValidateCurrentDialogSettings();

                // Create settings object for new dialog, based on current state.
                var settings = new NativeTaskDialogSettings();
                ApplyCoreSettings(settings);
                ApplySupplementalSettings(settings);

                // Show the dialog.
                // NOTE: this is a BLOCKING call; the dialog proc callbacks will be executed by the same thread as the Show() call before the
                // thread of execution contines to the end of this method.
                nativeDialog = new NativeTaskDialog(settings, this);
                nativeDialog.NativeShow();

                // Build and return dialog result to public API - leaving it null after an exception is thrown is fine in this case
                result = ConstructDialogResult(nativeDialog);
                footerCheckBoxChecked = nativeDialog.CheckBoxChecked;
            }
            finally
            {
                CleanUp();
                nativeDialog = null;
            }

            return result;
        }

        // Here we walk our controls collection and sort the various controls by type.
        private void SortDialogControls()
        {
            foreach (var control in controls)
            {
                var buttonBase = control as TaskDialogButtonBase;
                var commandLink = control as TaskDialogCommandLink;

                if (buttonBase != null && string.IsNullOrEmpty(buttonBase.Text) &&
                    commandLink != null && string.IsNullOrEmpty(commandLink.Instruction))
                {
                    throw new InvalidOperationException(LocalizedMessages.TaskDialogButtonTextEmpty);
                }

                TaskDialogRadioButton radButton;
                TaskDialogProgressBar progBar;

                // Loop through child controls and sort the controls based on type.
                if (commandLink != null)
                {
                    commandLinks.Add(commandLink);
                }
                else if ((radButton = control as TaskDialogRadioButton) != null)
                {
                    if (radioButtons == null) { radioButtons = new List<TaskDialogButtonBase>(); }
                    radioButtons.Add(radButton);
                }
                else if (buttonBase != null)
                {
                    if (buttons == null) { buttons = new List<TaskDialogButtonBase>(); }
                    buttons.Add(buttonBase);
                }
                else if ((progBar = control as TaskDialogProgressBar) != null)
                {
                    progressBar = progBar;
                }
                else
                {
                    throw new InvalidOperationException(LocalizedMessages.TaskDialogUnkownControl);
                }
            }
        }

        // Helper to map the standard button IDs returned by TaskDialogIndirect to the standard button ID enum - note that we can't just
        // cast, as the Win32 typedefs differ incoming and outgoing.
        private void ThrowIfDialogShowing(string message)
        {
            if (NativeDialogShowing) { throw new NotSupportedException(message); }
        }

        // Helper that looks at the current state of the TaskDialog and verifies that there aren't any abberant combinations of properties.
        // NOTE that this method is designed to throw rather than return a bool.
        private void ValidateCurrentDialogSettings()
        {
            if (footerCheckBoxChecked.HasValue &&
                footerCheckBoxChecked.Value == true &&
                string.IsNullOrEmpty(checkBoxText))
            {
                throw new InvalidOperationException(LocalizedMessages.TaskDialogCheckBoxTextRequiredToEnableCheckBox);
            }

            // Progress bar validation. Make sure the progress bar values are valid. the Win32 API will valiantly try to rationalize bizarre
            // min/max/value combinations, but we'll save it the trouble by validating.
            if (progressBar != null && !progressBar.HasValidValues)
            {
                throw new InvalidOperationException(LocalizedMessages.TaskDialogProgressBarValueInRange);
            }

            // Validate Buttons collection. Make sure we don't have buttons AND command-links - the Win32 API treats them as different
            // flavors of a single button struct.
            if (buttons.Count > 0 && commandLinks.Count > 0)
            {
                throw new NotSupportedException(LocalizedMessages.TaskDialogSupportedButtonsAndLinks);
            }
            if (buttons.Count > 0 && standardButtons != TaskDialogStandardButtons.None)
            {
                throw new NotSupportedException(LocalizedMessages.TaskDialogSupportedButtonsAndButtons);
            }
        }
    }
}