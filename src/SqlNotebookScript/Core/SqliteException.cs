using System;

namespace SqlNotebookScript.Core;

public sealed class SqliteException : Exception
{
    public string Snippet { get; set; }

    public SqliteException(string message)
        : base(message) { }
}
