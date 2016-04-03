﻿// SQL Notebook
// Copyright (C) 2016 Brian Luft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
// OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace SqlNotebook {
    public partial class UserControlDockContent : DockContent {
        public IDocumentControl Content { get; private set; }

        public UserControlDockContent(string title, UserControl control) {
            InitializeComponent();
            Text = title;
            control.Dock = DockStyle.Fill;
            Controls.Add(control);
            Content = control as IDocumentControl;

            // disable floating windows
            DockAreas = DockAreas.DockLeft | DockAreas.DockTop | DockAreas.DockRight | DockAreas.DockBottom | DockAreas.Document;
        }
    }
}
