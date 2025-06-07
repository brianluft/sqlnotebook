//Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.Resources;

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    internal static class TaskDialogDefaults
    {
        public const int IdealWidth = 0;

        // For generating control ID numbers that won't collide with the standard button return IDs.
        public const int MinimumDialogControlId =
            (int)TaskDialogNativeMethods.TaskDialogCommonButtonReturnIds.Close + 1;

        public const int ProgressBarMaximumValue = 100;
        public const int ProgressBarMinimumValue = 0;
        public static string Caption => LocalizedMessages.TaskDialogDefaultCaption;
        public static string Content => LocalizedMessages.TaskDialogDefaultContent;
        public static string MainInstruction => LocalizedMessages.TaskDialogDefaultMainInstruction;
    }
}