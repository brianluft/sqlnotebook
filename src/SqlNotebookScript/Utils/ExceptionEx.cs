using System;

namespace SqlNotebookScript.Utils;

public sealed class ExceptionEx : Exception
{
    public string Heading { get; }
    public string Details { get; }

    public ExceptionEx(string heading, string details)
        : base($"{heading}\r\n\r\n{details}")
    {
        Heading = heading;
        Details = details;
    }
}
