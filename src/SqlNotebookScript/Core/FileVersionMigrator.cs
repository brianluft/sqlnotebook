using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using SqlNotebookScript.Core.SqliteInterop;
using SqlNotebookScript.Utils;

namespace SqlNotebookScript.Core;

public static class FileVersionMigrator
{
    public static int MigrateIfNeeded(string filePath)
    {
        var version = GetFileVersion(filePath);
        if (version == 1)
        {
            MigrateFileVersion1to2(filePath);
            version = 2;
        }

        return version;
    }

    private static int GetFileVersion(string filePath)
    {
        if (IsVersion1(filePath))
        {
            return 1;
        }

        // Versions 2+ are SQLite databases. Open it and read the version number inside.
        var sqlite = IntPtr.Zero; //sqlite3*
        var stmt = IntPtr.Zero; // sqlite3_stmt*
        try
        {
            using NativeString filePathNative = new(filePath);
            using NativeBuffer sqliteBuffer = new(IntPtr.Size);
            var result = NativeMethods.sqlite3_open(filePathNative.Ptr, sqliteBuffer.Ptr);
            if (result != NativeMethods.SQLITE_OK)
            {
                throw new Exception("This is not an SQL Notebook file.");
            }
            sqlite = Marshal.ReadIntPtr(sqliteBuffer.Ptr);

            var versionSql = "SELECT version FROM _sqlnotebook_version";
            using NativeString versionSqlNative = new(versionSql);
            using NativeBuffer versionStmtNative = new(IntPtr.Size);
            result = NativeMethods.sqlite3_prepare_v2(
                sqlite,
                versionSqlNative.Ptr,
                versionSqlNative.ByteCount,
                versionStmtNative.Ptr,
                IntPtr.Zero
            );
            stmt = Marshal.ReadIntPtr(versionStmtNative.Ptr); // sqlite3_stmt*
            if (result != NativeMethods.SQLITE_OK || stmt == IntPtr.Zero)
            {
                // it's ok, a plain SQLite database is a valid version 2 SQL Notebook file
                return 2;
            }

            if (NativeMethods.sqlite3_step(stmt) == NativeMethods.SQLITE_ROW)
            {
                return NativeMethods.sqlite3_column_int(stmt, 0);
            }
            return 2; // missing version row; it's still a valid version 2 file
        }
        finally
        {
            if (stmt != IntPtr.Zero)
            {
                SqliteUtil.ThrowIfError(sqlite, NativeMethods.sqlite3_finalize(stmt));
            }
            if (sqlite != IntPtr.Zero)
            {
                SqliteUtil.ThrowIfError(IntPtr.Zero, NativeMethods.sqlite3_close(sqlite));
            }
        }
    }

    private static bool IsVersion1(string filePath)
    {
        // Version 1 is a zip file. First two bytes are "PK".
        using var stream = File.OpenRead(filePath);
        if (stream.Length > 2)
        {
            var a = stream.ReadByte();
            var b = stream.ReadByte();
            if (a == 'P' && b == 'K')
            {
                return true;
            }
        }
        return false;
    }

    private sealed class V1NotebookUserData
    {
        public List<V1NotebookItem> Items { get; set; }
        public List<V1ScriptParameter> ScriptParameters { get; set; }
    }

    private sealed class V1NotebookItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
    }

    private sealed class V1ScriptParameter
    {
        public string ScriptName { get; set; }
        public List<string> ParamNames { get; set; } = new();
    }

    // Convert a file from version 1 (zip file, up through 0.6.0) to version 2 (sqlite db, starting 1.0.0)
    private static void MigrateFileVersion1to2(string filePath)
    {
        using TempFile dbFile = new(".v1");
        V1NotebookUserData v1UserData = null;

        // read the two entries from the zip file
        using (var zip = ZipFile.OpenRead(filePath))
        {
            // extract the user items
            {
                var entry = zip.GetEntry("notebook.json");
                using var inStream = entry.Open();
                using StreamReader reader = new(inStream, Encoding.UTF8, false, 8192, false);
                var json = reader.ReadToEnd();
                v1UserData = JsonSerializer.Deserialize<V1NotebookUserData>(json, new JsonSerializerOptions());
            }

            // extract the sqlite database
            {
                var entry = zip.GetEntry("sqlite.db");
                using var inStream = entry.Open();
                using FileStream outStream = new(dbFile.FilePath, FileMode.Create);
                inStream.CopyTo(outStream);
            }
        }

        // store the user data in the new table format
        using (var notebook = Notebook.Open(dbFile.FilePath))
        {
            var scriptParameters = v1UserData.ScriptParameters.ToDictionary(x => x.ScriptName, x => x.ParamNames);
            NotebookUserData newUserData = new() { Items = new() };
            foreach (var item in v1UserData.Items)
            {
                if (item.Type == "Script")
                {
                    newUserData.Items.Add(
                        new ScriptNotebookItemRecord
                        {
                            Name = item.Name,
                            Sql = item.Data,
                            Parameters = scriptParameters.TryGetValue(item.Name, out var p) ? p : new(),
                        }
                    );
                }
                else if (item.Type == "Note")
                {
                    HtmlDocument doc = new();
                    doc.LoadHtml(item.Data);
                    var str = Regex
                        .Replace(doc.DocumentNode.InnerText.Replace("\r", ""), "\n\n\n+", "\n\n")
                        .Replace("\n", "\r\n");
                    newUserData.Items.Add(
                        new PageNotebookItemRecord
                        {
                            Name = item.Name,
                            Blocks = new() { new TextPageBlockRecord { Content = str } },
                        }
                    );
                }
            }
            notebook.UserData = newUserData;
            notebook.Save();
        }

        File.Move(dbFile.FilePath, filePath, overwrite: true);
    }
}
