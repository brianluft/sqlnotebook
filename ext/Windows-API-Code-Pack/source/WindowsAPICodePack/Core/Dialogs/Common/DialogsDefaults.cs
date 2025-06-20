﻿//Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.Resources;

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    internal static class DialogsDefaults
    {
        internal const int IdealWidth = 0;

        // For generating control ID numbers that won't collide with the standard button return IDs.
        internal const int MinimumDialogControlId =
            (int)TaskDialogNativeMethods.TaskDialogCommonButtonReturnIds.Close + 1;

        internal const int ProgressBarMaximumValue = 100;
        internal const int ProgressBarMinimumValue = 0;
        internal const int ProgressBarStartingValue = 0;
        internal static string Caption => LocalizedMessages.DialogDefaultCaption;
        internal static string Content => LocalizedMessages.DialogDefaultContent;
        internal static string MainInstruction => LocalizedMessages.DialogDefaultMainInstruction;
    }
}