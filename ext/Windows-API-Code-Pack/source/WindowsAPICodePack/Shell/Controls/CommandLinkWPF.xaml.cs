//Copyright (c) Microsoft Corporation.  All rights reserved.

using MS.WindowsAPICodePack.Internal;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.WindowsAPICodePack.Controls.WindowsPresentationFoundation
{
    /// <summary>Implements a CommandLink button that can be used in WPF user interfaces.</summary>
    public partial class CommandLink : UserControl, INotifyPropertyChanged
    {
        private ImageSource icon;

        private string link;

        private string note;

        /// <summary>Creates a new instance of this class.</summary>
        public CommandLink()
        {
            // Throw PlatformNotSupportedException if the user is not running Vista or beyond
            CoreHelpers.ThrowIfNotVista();

            DataContext = this;
            InitializeComponent();
            button.Click += new RoutedEventHandler(button_Click);
        }

        /// <summary>Occurs when the control is clicked.</summary>
        public event RoutedEventHandler Click;

        /// <summary>Occurs when a property value changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Indicates whether this feature is supported on the current platform.</summary>
        public static bool IsPlatformSupported => CoreHelpers.RunningOnVista;

        /// <summary>Routed UI command to use for this button</summary>
        public RoutedUICommand Command { get; set; }

        /// <summary>Icon to set for the command link button</summary>
        public ImageSource Icon
        {
            get => icon;
            set
            {
                icon = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Icon"));
                }
            }
        }

        /// <summary>Indicates if the button is in a checked state</summary>
        public bool? IsCheck
        {
            get => button.IsChecked;
            set => button.IsChecked = value;
        }

        /// <summary>Specifies the main instruction text</summary>
        public string Link
        {
            get => link;
            set
            {
                link = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Link"));
                }
            }
        }

        /// <summary>Specifies the supporting note text</summary>
        public string Note
        {
            get => note;
            set
            {
                note = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Note"));
                }
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            e.Source = this;
            if (Click != null)
            {
                Click(sender, e);
            }
        }
    }
}