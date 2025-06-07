//Copyright (c) Microsoft Corporation.  All rights reserved.
using Microsoft.WindowsAPICodePack.Controls.WindowsForms;

namespace Microsoft.WindowsAPICodePack.Controls
{
    /// <summary>These options control the results subsequent navigations of the ExplorerBrowser</summary>
    public class ExplorerBrowserNavigationOptions
    {
        private readonly ExplorerBrowser eb;

        internal ExplorerBrowserNavigationOptions(ExplorerBrowser eb)
        {
            this.eb = eb;
            PaneVisibility = new ExplorerBrowserPaneVisibility();
        }

        /// <summary>Always navigate, even if you are attempting to navigate to the current folder.</summary>
        public bool AlwaysNavigate
        {
            get => IsFlagSet(ExplorerBrowserNavigateOptions.AlwaysNavigate);
            set => SetFlag(ExplorerBrowserNavigateOptions.AlwaysNavigate, value);
        }

        /// <summary>The binary flags that are passed to the explorer browser control's GetOptions/SetOptions methods</summary>
        public ExplorerBrowserNavigateOptions Flags
        {
            get
            {
                var ebo = new ExplorerBrowserOptions();
                if (eb.explorerBrowserControl != null)
                {
                    eb.explorerBrowserControl.GetOptions(out ebo);
                    return (ExplorerBrowserNavigateOptions)ebo;
                }
                return (ExplorerBrowserNavigateOptions)ebo;
            }
            set
            {
                var ebo = (ExplorerBrowserOptions)value;
                if (eb.explorerBrowserControl != null)
                {
                    // Always forcing SHOWFRAMES because we handle IExplorerPaneVisibility
                    eb.explorerBrowserControl.SetOptions(ebo | ExplorerBrowserOptions.ShowFrames);
                }
            }
        }

        /// <summary>Do not navigate further than the initial navigation.</summary>
        public bool NavigateOnce
        {
            get => IsFlagSet(ExplorerBrowserNavigateOptions.NavigateOnce);
            set => SetFlag(ExplorerBrowserNavigateOptions.NavigateOnce, value);
        }

        /// <summary>Controls the visibility of the various ExplorerBrowser panes on subsequent navigation</summary>
        public ExplorerBrowserPaneVisibility PaneVisibility { get; private set; }

        private bool IsFlagSet(ExplorerBrowserNavigateOptions flag) => (Flags & flag) != 0;

        private void SetFlag(ExplorerBrowserNavigateOptions flag, bool value)
        {
            if (value)
            {
                Flags |= flag;
            }
            else
            {
                Flags = Flags & ~flag;
            }
        }
    }
}