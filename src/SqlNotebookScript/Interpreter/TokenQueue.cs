using System;
using System.Collections.Generic;
using System.Linq;
using SqlNotebookScript.Core;

namespace SqlNotebookScript.Interpreter;

public sealed class TokenQueue
{
    private readonly List<Token> _tokens;
    private int _peekIndex;
    private readonly int _eofLocation;

    public Notebook Notebook { get; }

    public TokenQueue(IEnumerable<Token> input, Notebook notebook)
    {
        Notebook = notebook;
        _tokens = input.ToList();
        if (_tokens.Any())
        {
            _eofLocation = (int)(_tokens.Last().Utf8Start + _tokens.Last().Utf8Length);
        }
        else
        {
            _eofLocation = 0;
        }
    }

    public Token SourceToken
    {
        get
        {
            if (_peekIndex < _tokens.Count)
            {
                return PeekToken();
            }
            else
            {
                return new Token
                {
                    Text = "",
                    Type = TokenType.Space,
                    Utf8Start = (ulong)_eofLocation,
                    Utf8Length = 0,
                };
            }
        }
    }

    public int GetLocation()
    {
        return _peekIndex;
    }

    public void Jump(int location)
    { // location was previously returned from GetLocation()
        _peekIndex = location;
    }

    public Token Take()
    {
        if (_peekIndex < _tokens.Count)
        {
            return _tokens[_peekIndex++];
        }
        else
        {
            return new Token { Text = "", Type = EofTokenType.Value };
        }
    }

    public Token Take(params string[] expectedTexts)
    {
        var text = Peek();
        if (expectedTexts.Any(x => x.Equals(text, StringComparison.OrdinalIgnoreCase)))
        {
            return Take();
        }
        else
        {
            throw new SyntaxException(expectedTexts, _tokens.Skip(_peekIndex).ToList());
        }
    }

    public bool TakeMaybe(params string[] expectedTexts)
    {
        var text = Peek();
        if (expectedTexts.Any(x => x.Equals(text, StringComparison.OrdinalIgnoreCase)))
        {
            Take();
            return true;
        }
        else
        {
            return false;
        }
    }

    public string Peek(int skip = 0)
    {
        int n = _peekIndex + skip;
        if (n < _tokens.Count)
        {
            return _tokens[n].Text.ToLower();
        }
        else
        {
            return "";
        }
    }

    public Token PeekToken(int skip = 0)
    {
        int n = _peekIndex + skip;
        if (n < _tokens.Count)
        {
            return _tokens[n];
        }
        else
        {
            return new Token { Text = "", Type = EofTokenType.Value };
        }
    }

    public bool Eof()
    {
        return _peekIndex >= _tokens.Count;
    }

    public override string ToString()
    {
        return string.Join(" ", _tokens.Skip(_peekIndex).Select(x => x.Text));
    }

    public static implicit operator List<Token>(TokenQueue q)
    {
        return q._tokens.Skip(q._peekIndex).ToList();
    }

    // get some tokens at the current cursor location
    public string GetSnippet()
    {
        if (_peekIndex == _eofLocation)
        {
            int num = Math.Min(5, _tokens.Count);
            return string.Join(" ", Enumerable.Range(_eofLocation - num, num).Select(i => _tokens[i].Text));
        }
        else
        {
            return string.Join(" ", _tokens.Skip(_peekIndex).Take(5).Select(x => x.Text));
        }
    }

    public string Substring(int startToken, int numTokens)
    {
        return string.Join(" ", _tokens.Skip(startToken).Take(numTokens).Select(x => x.Text));
    }
}
