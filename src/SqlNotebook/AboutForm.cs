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

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SqlNotebook.Properties;

namespace SqlNotebook {
    public partial class AboutForm : Form {
        public AboutForm() {
            InitializeComponent();

            Ui ui = new(this, 100, 35);
            ui.Init(_table);
            ui.Init(_linkFlow);
            ui.Pad(_linkFlow);
            ui.MarginTop(_browserPanel);
            ui.Init(_buttonFlow);
            ui.MarginTop(_buttonFlow);
            ui.Init(_okBtn);

            _browser.DocumentText = Resources.ThirdPartyLicensesHtml;
            Text += $" {Application.ProductVersion}";
        }

        private void OkBtn_Click(object sender, EventArgs e) {
            Close();
        }

        private void GithubLnk_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            Process.Start(new ProcessStartInfo("https://github.com/electroly/sqlnotebook") { UseShellExecute = true });
        }

        private void WebsiteLnk_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            Process.Start(new ProcessStartInfo("https://sqlnotebook.com/") { UseShellExecute = true });
        }
    }
}
