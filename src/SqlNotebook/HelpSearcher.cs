using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using HtmlAgilityPack;
using SqlNotebookScript;
using SqlNotebookScript.Core;
using SqlNotebookScript.Utils;

namespace SqlNotebook;

public static class HelpSearcher
{
    private static byte[] _cachedNotebookBytes;

    public static List<Result> Search(string keyword)
    {
        var tempFilePath = Path.GetTempFileName();
        try
        {
            var hasCache = _cachedNotebookBytes != null;
            if (hasCache)
            {
                File.WriteAllBytes(tempFilePath, _cachedNotebookBytes);
            }
            List<Result> results = null;
            using (var notebook = hasCache ? Notebook.Open(tempFilePath) : Notebook.New())
            {
                if (!hasCache)
                {
                    InitHelpNotebook(notebook, tempFilePath);
                }
                results = SearchQuery(notebook, keyword);
                if (!hasCache)
                {
                    notebook.SaveAs(tempFilePath);
                }
            }
            if (!hasCache)
            {
                _cachedNotebookBytes = File.ReadAllBytes(tempFilePath);
            }
            return results;
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    private static void InitHelpNotebook(Notebook notebook, string sqlnbFilePath)
    {
        var exeDir = Path.GetDirectoryName(Application.ExecutablePath);
        var docDir = Path.Combine(exeDir, "doc");
        var htmlFiles = (
            from htmlFilePath in Directory.GetFiles(docDir, "*.html", SearchOption.AllDirectories)
            let content = File.ReadAllText(htmlFilePath)
            select (FilePath: htmlFilePath, Content: content)
        ).ToList();

        notebook.Execute("BEGIN");

        notebook.Execute(
            @"CREATE TABLE docs (
                id INTEGER PRIMARY KEY,
                path TEXT NOT NULL,
                book TEXT NOT NULL, 
                title TEXT NOT NULL,
                html TEXT NOT NULL
            )"
        );
        notebook.Execute(@"CREATE TABLE books_txt (number INTEGER PRIMARY KEY, line TEXT NOT NULL)");
        notebook.Execute(@"CREATE TABLE art (file_path TEXT PRIMARY KEY, content BLOB)");
        notebook.Execute("CREATE VIRTUAL TABLE docs_fts USING fts5 (id, title, text)");

        using var status = WaitStatus.StartCustom("Documentation index");
        for (var i = 0; i < htmlFiles.Count; i++)
        {
            status.SetProgress($"{i * 100 / htmlFiles.Count}% complete");
            var (filePath, content) = htmlFiles[i];
            var filename = Path.GetFileName(filePath);
            if (filename == "doc.html" || filename == "index.html")
            {
                continue;
            }

            // Parse out the title.
            var title = "(no title)";
            {
                var startIndex = content.IndexOf("<title>");
                var endIndex = content.IndexOf("</title>");
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    title = WebUtility.HtmlDecode(content[(startIndex + "<title>".Length)..endIndex]).Trim();
                    content = content[..startIndex] + content[(endIndex + "</title>".Length)..];
                }
            }

            // Remove our navigation header
            {
                var startIndex = content.IndexOf("<header>");
                var endIndex = content.IndexOf("</header>");
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    content = content[..startIndex] + content[(endIndex + "</header>".Length)..];
                }
            }

            var text = ParseHtml(content);

            title = title.Replace("- SQL Notebook", "").Trim();

            notebook.Execute(
                "INSERT INTO docs VALUES (@id, @path, @book, @title, @html)",
                new Dictionary<string, object>
                {
                    ["@id"] = i,
                    ["@path"] = filePath,
                    ["@book"] = "SQLite Documentation",
                    ["@title"] = title,
                    ["@html"] = content,
                }
            );
            notebook.Execute(
                "INSERT INTO docs_fts VALUES (@id, @title, @text)",
                new Dictionary<string, object>
                {
                    ["@id"] = i,
                    ["@title"] = title,
                    ["@text"] = text,
                }
            );
        }

        status.SetProgress("100% complete");
        notebook.Execute("COMMIT");
        notebook.Execute("ANALYZE");
    }

    private static string ParseHtml(string html)
    {
        HtmlAgilityPack.HtmlDocument htmlDoc = new() { OptionDefaultStreamEncoding = Encoding.UTF8 };
        htmlDoc.LoadHtml(html);
        var stack = new Stack<HtmlNode>();
        stack.Push(htmlDoc.DocumentNode);
        while (stack.Any())
        {
            var node = stack.Pop();
            if (node.Name == "style" || node.Name == "script")
            {
                node.InnerHtml = "";
                continue;
            }

            if (node.Name == "li" || node.Name == "p" || node.Name == "td" || node.Name == "br")
            {
                node.InnerHtml = " " + node.InnerHtml + " ";
            }

            foreach (var child in node.ChildNodes)
            {
                stack.Push(child);
            }
        }
        var text = WebUtility.HtmlDecode(
            htmlDoc
                .DocumentNode.InnerText.Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Replace("&nbsp;", " ")
        );
        while (text.Contains("  "))
        {
            text = text.Replace("  ", " ");
        }
        return text;
    }

    private static List<Result> SearchQuery(Notebook notebook, string keyword)
    { // run from notebook thread
        using var dt = notebook.Query(
            @"SELECT
                f.id, d.path, d.book,
                HIGHLIGHT(docs_fts, 1, '', ''),
                SNIPPET(docs_fts, 2, '', '', '...', 25)
            FROM docs_fts f
            INNER JOIN docs d ON f.id = d.id
            WHERE f.docs_fts MATCH @keyword
            ORDER BY rank",
            new Dictionary<string, object>
            {
                ["@keyword"] = string.Join(
                    " ",
                    keyword.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.DoubleQuote())
                ),
            }
        );
        return (
            from row in dt.Rows
            let path = (string)row[1]
            let title = (string)row[3]
            let snippet = (string)row[4]
            select new Result
            {
                Path = path,
                Title = title,
                Snippet = snippet,
            }
        ).ToList();
    }

    public sealed class Result
    {
        public string Path;
        public string Title;
        public string Snippet;
    }
}
