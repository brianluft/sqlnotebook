using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SqlNotebook;

public partial class HelpSearchResultsForm : ZForm
{
    public HelpSearchResultsForm(string terms, List<HelpSearcher.Result> results)
    {
        InitializeComponent();
        Text = $"Search results for \"{terms}\"";

        Font titleFont = new("Segoe UI", 12f, FontStyle.Bold);
        Disposed += delegate
        {
            titleFont.Dispose();
        };

        Font snippetFont = new("Segoe UI", 9f);
        Disposed += delegate
        {
            snippetFont.Dispose();
        };

        Ui ui = new(this, 120, 40);
        ui.Init(_table);
        ui.Init(_resultsFlow);
        ui.Pad(_resultsFlow);
        ui.Init(_buttonFlow);
        ui.MarginTop(_buttonFlow);
        ui.Init(_closeButton);

        _resultsFlow.SuspendLayout();
        _resultsFlow.AutoScroll = true;

        foreach (var result in results.Take(25))
        {
            var resultFilePath = result.Path;
            LinkLabel titleLabel = new()
            {
                AutoSize = true,
                Text = result.Title,
                Font = titleFont,
                Margin = new(ui.XWidth(1), ui.XHeight(0.75), ui.XWidth(1), ui.XHeight(0.2)),
                Cursor = Cursors.Hand,
            };
            titleLabel.Click += OnResultClick;
            Label snippetLabel = new()
            {
                AutoSize = true,
                Text = result.Snippet,
                Font = snippetFont,
                Margin = new(ui.XWidth(1), 0, ui.XWidth(1), ui.XHeight(1)),
                Cursor = Cursors.Hand,
            };
            snippetLabel.Click += OnResultClick;
            _resultsFlow.Controls.Add(titleLabel);
            _resultsFlow.Controls.Add(snippetLabel);

            void OnResultClick(object sender, EventArgs e)
            {
                Process.Start(new ProcessStartInfo(resultFilePath) { UseShellExecute = true });
            }
        }

        _resultsFlow.Controls.Add(new Panel { AutoSize = false, Size = new(1, ui.XHeight(4)) });

        _resultsFlow.ResumeLayout(true);
    }
}
