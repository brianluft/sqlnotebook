//Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.WindowsAPICodePack.Controls.WindowsForms;
using Microsoft.WindowsAPICodePack.Shell.Resources;
using MS.WindowsAPICodePack.Internal;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.Controls
{
    /// <summary>These options control how the content of the Explorer Browser is rendered.</summary>
    public class ExplorerBrowserContentOptions
    {
        // This is a one-way property of the explorer browser. Keeping it around for the get implementations.
        internal FolderSettings folderSettings = new FolderSettings();

        private readonly ExplorerBrowser eb;

        internal ExplorerBrowserContentOptions(ExplorerBrowser eb) => this.eb = eb;

        /// <summary>The view should be left-aligned.</summary>
        public bool AlignLeft
        {
            get => IsFlagSet(ExplorerBrowserContentSectionOptions.AlignLeft);
            set => SetFlag(ExplorerBrowserContentSectionOptions.AlignLeft, value);
        }

        /// <summary>Automatically arrange the elements in the view.</summary>
        public bool AutoArrange
        {
            get => IsFlagSet(ExplorerBrowserContentSectionOptions.AutoArrange);
            set => SetFlag(ExplorerBrowserContentSectionOptions.AutoArrange, value);
        }

        /// <summary>Turns on check mode for the view</summary>
        public bool CheckSelect
        {
            get => IsFlagSet(ExplorerBrowserContentSectionOptions.CheckSelect);
            set => SetFlag(ExplorerBrowserContentSectionOptions.CheckSelect, value);
        }

        /// <summary>When the view is in "tile view mode" the layout of a single item should be extended to the width of the view.</summary>
        public bool ExtendedTiles
        {
            get => IsFlagSet(ExplorerBrowserContentSectionOptions.ExtendedTiles);
            set => SetFlag(ExplorerBrowserContentSectionOptions.ExtendedTiles, value);
        }

        /// <summary>The binary representation of the ExplorerBrowser content flags</summary>
        public ExplorerBrowserContentSectionOptions Flags
        {
            get => (ExplorerBrowserContentSectionOptions)folderSettings.Options;
            set
            {
                folderSettings.Options = (FolderOptions)value | FolderOptions.UseSearchFolders | FolderOptions.NoWebView;
                if (eb.explorerBrowserControl != null)
                {
                    eb.explorerBrowserControl.SetFolderSettings(folderSettings);
                }
            }
        }

        /// <summary>When an item is selected, the item and all its sub-items are highlighted.</summary>
        public bool FullRowSelect
        {
            get => IsFlagSet(ExplorerBrowserContentSectionOptions.FullRowSelect);
            set => SetFlag(ExplorerBrowserContentSectionOptions.FullRowSelect, value);
        }

        /// <summary>The view should not display file names</summary>
        public bool HideFileNames
        {
            get => IsFlagSet(ExplorerBrowserContentSectionOptions.HideFileNames);
            set => SetFlag(ExplorerBrowserContentSectionOptions.HideFileNames, value);
        }

        /// <summary>The view should not save view state in the browser.</summary>
        public bool NoBrowserViewState
        {
            get => IsFlagSet(ExplorerBrowserContentSectionOptions.NoBrowserViewState);
            set => SetFlag(ExplorerBrowserContentSectionOptions.NoBrowserViewState, value);
        }

        /// <summary>Do not display a column header in the view in any view mode.</summary>
        public bool NoColumnHeader
        {
            get => IsFlagSet(ExplorerBrowserContentSectionOptions.NoColumnHeader);
            set => SetFlag(ExplorerBrowserContentSectionOptions.NoColumnHeader, value);
        }

        /// <summary>Only show the column header in details view mode.</summary>
        public bool NoHeaderInAllViews
        {
            get => IsFlagSet(ExplorerBrowserContentSectionOptions.NoHeaderInAllViews);
            set => SetFlag(ExplorerBrowserContentSectionOptions.NoHeaderInAllViews, value);
        }

        /// <summary>The view should not display icons.</summary>
        public bool NoIcons
        {
            get => IsFlagSet(ExplorerBrowserContentSectionOptions.NoIcons);
            set => SetFlag(ExplorerBrowserContentSectionOptions.NoIcons, value);
        }

        /// <summary>Do not show subfolders.</summary>
        public bool NoSubfolders
        {
            get => IsFlagSet(ExplorerBrowserContentSectionOptions.NoSubfolders);
            set => SetFlag(ExplorerBrowserContentSectionOptions.NoSubfolders, value);
        }

        /// <summary>Navigate with a single click</summary>
        public bool SingleClickActivate
        {
            get => IsFlagSet(ExplorerBrowserContentSectionOptions.SingleClickActivate);
            set => SetFlag(ExplorerBrowserContentSectionOptions.SingleClickActivate, value);
        }

        /// <summary>Do not allow more than a single item to be selected.</summary>
        public bool SingleSelection
        {
            get => IsFlagSet(ExplorerBrowserContentSectionOptions.SingleSelection);
            set => SetFlag(ExplorerBrowserContentSectionOptions.SingleSelection, value);
        }

        /// <summary>The size of the thumbnails in pixels</summary>
        public int ThumbnailSize
        {
            get
            {
                var iconSize = 0;
                var iFV2 = eb.GetFolderView2();
                if (iFV2 != null)
                {
                    try
                    {
                        var hr = iFV2.GetViewModeAndIconSize(out var fvm, out iconSize);
                        if (hr != HResult.Ok)
                        {
                            throw new CommonControlException(LocalizedMessages.ExplorerBrowserIconSize, hr);
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(iFV2);
                        iFV2 = null;
                    }
                }

                return iconSize;
            }
            set
            {
                var iFV2 = eb.GetFolderView2();
                if (iFV2 != null)
                {
                    try
                    {
                        var hr = iFV2.GetViewModeAndIconSize(out var fvm, out var iconSize);
                        if (hr != HResult.Ok)
                        {
                            throw new CommonControlException(LocalizedMessages.ExplorerBrowserIconSize, hr);
                        }
                        hr = iFV2.SetViewModeAndIconSize(fvm, value);
                        if (hr != HResult.Ok)
                        {
                            throw new CommonControlException(LocalizedMessages.ExplorerBrowserIconSize, hr);
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(iFV2);
                        iFV2 = null;
                    }
                }
            }
        }

        /// <summary>The viewing mode of the Explorer Browser</summary>
        public ExplorerBrowserViewMode ViewMode
        {
            get => (ExplorerBrowserViewMode)folderSettings.ViewMode;
            set
            {
                folderSettings.ViewMode = (FolderViewMode)value;

                if (eb.explorerBrowserControl != null)
                {
                    eb.explorerBrowserControl.SetFolderSettings(folderSettings);
                }
            }
        }

        private bool IsFlagSet(ExplorerBrowserContentSectionOptions flag) => (folderSettings.Options & (FolderOptions)flag) != 0;

        private void SetFlag(ExplorerBrowserContentSectionOptions flag, bool value)
        {
            if (value)
                folderSettings.Options |= (FolderOptions)flag;
            else
                folderSettings.Options = folderSettings.Options & ~(FolderOptions)flag;

            if (eb.explorerBrowserControl != null)
                eb.explorerBrowserControl.SetFolderSettings(folderSettings);
        }
    }
}