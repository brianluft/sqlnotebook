using System;

namespace SqlNotebookScript.Interpreter;

public class ScriptException : Exception
{
    public ScriptException(string message, Exception innerException = null)
        : base(message, innerException) { }
}
