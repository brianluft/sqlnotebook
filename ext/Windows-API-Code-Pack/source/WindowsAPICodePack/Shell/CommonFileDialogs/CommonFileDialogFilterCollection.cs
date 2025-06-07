//Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.Shell;
using System.Collections.ObjectModel;

namespace Microsoft.WindowsAPICodePack.Dialogs
{
    /// <summary>Provides a strongly typed collection for file dialog filters.</summary>
    public class CommonFileDialogFilterCollection : Collection<CommonFileDialogFilter>
    {
        // Make the default constructor internal so users can't instantiate this collection by themselves.
        internal CommonFileDialogFilterCollection() { }

        internal ShellNativeMethods.FilterSpec[] GetAllFilterSpecs()
        {
            var filterSpecs = new ShellNativeMethods.FilterSpec[Count];

            for (var i = 0; i < Count; i++)
            {
                filterSpecs[i] = this[i].GetFilterSpec();
            }

            return filterSpecs;
        }
    }
}