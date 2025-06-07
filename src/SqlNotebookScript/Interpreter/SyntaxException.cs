using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SqlNotebookScript.Interpreter;

public sealed class SyntaxException : Exception
{
    public SyntaxException(IEnumerable<string> expecteds, TokenQueue q)
        : base($"Syntax error at \"{q.GetSnippet()}\".  Expected: {string.Join(", ", expecteds)}") { }

    public SyntaxException(IEnumerable<string> expecteds, List<Token> tokens)
        : base(
            $"Syntax error at \"{string.Join(" ", tokens.Select(x => x.Text).Take(5)).Trim()}\".  Expected: {string.Join(", ", expecteds)}"
        ) { }

    public SyntaxException(TokenQueue q)
        : base($"Syntax error at \"{q.GetSnippet()}\"") { }

    public SyntaxException(List<Token> tokens)
        : base($"Syntax error at \"{string.Join(" ", tokens.Select(x => x.Text).Take(5)).Trim()}\"") { }

    public SyntaxException(string message)
        : base(message) { }
}
