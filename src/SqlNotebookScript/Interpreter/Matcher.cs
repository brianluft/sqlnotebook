// define this symbol to log Matcher stack frames to C:\temp\matcher.log
//#define MATCHER_LOG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SqlNotebookScript.Interpreter;

// possible results:
// IsMatch=true, ErrorMessage=null
// IsMatch=false, ErrorMessage=null
// IsMatch=false, ErrorMessage=non-null
public struct MatchResult
{
    public bool IsMatch;
    public string ErrorMessage;

    public static readonly MatchResult Matched = new MatchResult { IsMatch = true, ErrorMessage = null };
    public static readonly MatchResult NoMatch = new MatchResult { IsMatch = false, ErrorMessage = null };

    public static MatchResult Error(string message)
    {
        return new MatchResult { IsMatch = false, ErrorMessage = message };
    }
}

public enum OptionalTermState
{
    Start,
    Match,
}

public enum OrTermState
{
    Start,
    Match,
}

public enum ListTermState
{
    Start,
    MatchSeparator,
    MatchItem,
}

public sealed class MatchFrame
{
    // which production and term are we working on?
    public int ProdStartLoc;
    public SpecProd Prod;
    public int TermIndex;
    public Ast.SqliteSyntaxProduction AstProd;

    // OptionalTerm
    public OptionalTermState OptionalState;
    public int OptionalStartLoc;

    // OrTerm
    public OrTermState OrState;
    public int OrProdIndex;
    public int OrStartLoc;

    // ProdTerm
    public bool ProdMatched;

    // ListTerm
    public ListTermState ListState;
    public int ListCount;
    public int ListSeparatorStartLoc; // set before transitioning into MatchSeparator state

    // when a sub-frame returns, the result goes here
    public MatchResult SubResult;

    public void Clear(bool all = true)
    {
        if (all)
        {
            ProdStartLoc = 0;
            Prod = null;
            TermIndex = 0;
            AstProd = null;
        }
        OptionalState = OptionalTermState.Start;
        OptionalStartLoc = 0;
        OrState = OrTermState.Start;
        OrProdIndex = 0;
        OrStartLoc = 0;
        ProdMatched = false;
        ListState = ListTermState.Start;
        ListCount = 0;
        ListSeparatorStartLoc = 0;
        SubResult.ErrorMessage = "Uninitialized result";
        SubResult.IsMatch = false;
    }

    public override string ToString()
    {
        return Prod?.ToString() ?? "(unused)";
    }
}

// custom stack so we can reuse the frame objects, as our parsing process is very inefficient and requires
// building up and tearing down a huge call stack over and over again.
public sealed class MatchStack
{
    private readonly List<MatchFrame> _frames = new List<MatchFrame>(25);
    private int _topIndex = -1; // zero-based index of the top frame, -1 if the stack is empty

    public int Count
    {
        get { return _topIndex + 1; }
    }

    public TokenQueue Queue { get; set; }

    public bool Any()
    {
        return _topIndex >= 0;
    }

    public MatchFrame Push(SpecProd prod)
    {
        if (prod == null)
        {
            throw new ArgumentNullException("prod");
        }

        _topIndex++;

        if (_topIndex == _frames.Count)
        {
            _frames.Add(new MatchFrame());
        }

        _frames[_topIndex].Prod = prod;
        _frames[_topIndex].ProdStartLoc = Queue.GetLocation();
        _frames[_topIndex].AstProd = new Ast.SqliteSyntaxProduction { Name = prod.Name };
        return _frames[_topIndex];
    }

    public void Pop()
    {
        if (_topIndex == -1)
        {
            throw new InvalidOperationException("The stack is empty.");
        }

        _frames[_topIndex].Clear();
        _topIndex--;
    }

    public MatchFrame Peek()
    {
        return _topIndex < 0 ? null : _frames[_topIndex];
    }

    public void DebugDump(StreamWriter writer, int tokenLocation, string token, bool includeSubResult)
    {
        writer.WriteLine($"Token {tokenLocation}: {token}");
        for (int i = 0; i <= _topIndex; i++)
        {
            writer.WriteLine($"Frame {i}, Term {_frames[i].TermIndex}: {_frames[i].Prod.GetExpectedWithTermIndices()}");
        }
        if (includeSubResult && _topIndex >= 0)
        {
            var r = _frames[_topIndex].SubResult;
            writer.WriteLine($"  Result: IsMatch={r.IsMatch}, ErrorMessage={r.ErrorMessage ?? "(null)"}");
        }
        writer.WriteLine("-----");
    }
}

public abstract class SpecTerm
{
    public abstract MatchResult? MatchStep(MatchStack stack, MatchFrame frame, TokenQueue q);
    public abstract string GetExpected();
}

public sealed class SpecProd
{
    public string Name { get; set; }
    public SpecTerm[] Terms { get; set; }

    // numReq = number of terms that must be present for the production to be chosen.  if further input terms
    // don't match the production, then it's an error rather than just not matching this production.
    public int NumReq { get; set; }

    public string GetExpected()
    {
        return string.Join(" ", Terms.Select(x => x.GetExpected()));
    }

    public string GetExpectedWithTermIndices()
    {
        return string.Join(" ", Terms.Select((x, i) => $"{i}:{x.GetExpected()}"));
    }

    public override string ToString() => GetExpected();
}

public sealed class KeyTokenTerm : SpecTerm
{
    public TokenType Type { get; set; }

    public override string GetExpected()
    {
        return Type.ToString(); //TODO: convert to the actual source text, not the enum value name.
    }

    public override MatchResult? MatchStep(MatchStack stack, MatchFrame frame, TokenQueue q)
    {
        return q.Take().Type == Type ? MatchResult.Matched : MatchResult.NoMatch;
    }
}

public sealed class TokenSetTerm : SpecTerm
{
    private bool[] _bitmap = new bool[200];
    private IReadOnlyList<TokenType> _types;
    public IReadOnlyList<TokenType> Types
    {
        set
        {
            foreach (var t in value)
            {
                _bitmap[(int)t] = true;
            }
            _types = value;
        }
    }

    public override string GetExpected()
    {
        return "(" + string.Join("|", _types) + ")";
    }

    public override MatchResult? MatchStep(MatchStack stack, MatchFrame frame, TokenQueue q)
    {
        int idx = (int)q.Take().Type;
        var match = idx >= 0 && idx < _bitmap.Length && _bitmap[idx];
        return match ? MatchResult.Matched : MatchResult.NoMatch;
    }
}

public sealed class StringTokenTerm : SpecTerm
{
    public string Text { get; set; }

    public override string GetExpected()
    {
        return Text;
    }

    public override MatchResult? MatchStep(MatchStack stack, MatchFrame frame, TokenQueue q)
    {
        return q.Take().Text.ToLower() == Text.ToLower() ? MatchResult.Matched : MatchResult.NoMatch;
    }
}

public sealed class LiteralStringTerm : SpecTerm
{
    public string Desc { get; set; }

    public override string GetExpected()
    {
        return $"<{Desc}>";
    }

    public override MatchResult? MatchStep(MatchStack stack, MatchFrame frame, TokenQueue q)
    {
        return q.Take().Type == TokenType.String ? MatchResult.Matched : MatchResult.NoMatch;
    }
}

public sealed class OptionalTerm : SpecTerm
{
    public SpecProd Prod { get; set; }

    public override string GetExpected()
    {
        return $"[ {Prod.GetExpected()} ]";
    }

    public override MatchResult? MatchStep(MatchStack stack, MatchFrame frame, TokenQueue q)
    {
        if (frame.OptionalState == OptionalTermState.Start)
        {
            // try to match the sub-production
            stack.Push(Prod);
            frame.OptionalState = OptionalTermState.Match;
            frame.OptionalStartLoc = q.GetLocation();
            return null;
        }
        else if (frame.OptionalState == OptionalTermState.Match)
        {
            // done matching the sub-production
            var result = frame.SubResult;
            if (result.IsMatch)
            {
                // the optional term is indeed present.
                return MatchResult.Matched;
            }
            else if (result.ErrorMessage == null)
            {
                // it didn't match but wasn't an error.  this is fine, but we do have to walk the cursor back to
                // where it started since we effectively "matched" zero tokens.
                q.Jump(frame.OptionalStartLoc);
                return MatchResult.Matched;
            }
            else
            {
                // it started to match but then mismatched past the point of no return.  that's an error.
                return result;
            }
        }
        else
        {
            throw new Exception($"Unrecognized state: {frame.OptionalState}");
        }
    }
}

public sealed class IdentifierTerm : SpecTerm
{
    public string Desc { get; set; }
    public bool AllowVariable { get; set; }

    public override string GetExpected()
    {
        return $"<{Desc}>";
    }

    public override MatchResult? MatchStep(MatchStack stack, MatchFrame frame, TokenQueue q)
    {
        switch (q.Take().Type)
        {
            case TokenType.Id:
            case TokenType.Ties:
            case TokenType.Variable when AllowVariable:
                return MatchResult.Matched;
            default:
                return MatchResult.NoMatch;
        }
    }
}

public sealed class OrTerm : SpecTerm
{
    public SpecProd[] Prods { get; set; }

    public override string GetExpected()
    {
        return $"( {string.Join(" | ", Prods.Select(x => $"({x.GetExpected()})"))} )";
    }

    public override MatchResult? MatchStep(MatchStack stack, MatchFrame frame, TokenQueue q)
    {
        if (frame.OrState == OrTermState.Start)
        {
            // try to match the first sub-production
            stack.Push(Prods[0]);
            frame.OrState = OrTermState.Match;
            frame.OrProdIndex = 0;
            frame.OrStartLoc = q.GetLocation();
            return null;
        }
        else if (frame.OrState == OrTermState.Match)
        {
            // we have finished matching one of the productions.  if it matched, then we're done.  if not, move on
            // to the next production.
            var result = frame.SubResult;
            if (result.IsMatch)
            {
                return MatchResult.Matched;
            }
            else if (result.ErrorMessage == null)
            {
                // no match.  rewind to the beginning and retry with the next one.
                q.Jump(frame.OrStartLoc);
                frame.OrProdIndex++;
                if (frame.OrProdIndex >= Prods.Length)
                {
                    // we have exhausted all of the possibilities and none of them matched.
                    return MatchResult.NoMatch;
                }
                stack.Push(Prods[frame.OrProdIndex]);
                return null;
            }
            else
            {
                // started to match but mismatched past the point of no return.
                return result;
            }
        }
        else
        {
            throw new Exception($"Unrecognized state: {frame.OrState}");
        }
    }
}

public sealed class ProdTerm : SpecTerm
{
    private SpecProd _prod;
    public string ProdName { get; set; }

    public override string GetExpected()
    {
        return $"<{ProdName}>";
    }

    public override MatchResult? MatchStep(MatchStack stack, MatchFrame frame, TokenQueue q)
    {
        if (!frame.ProdMatched)
        {
            if (_prod == null)
            {
                _prod = SqliteGrammar.Prods[ProdName];
            }
            stack.Push(_prod);
            frame.ProdMatched = true;
            return null;
        }
        else
        {
            return frame.SubResult;
        }
    }
}

public sealed class ListTerm : SpecTerm
{
    public int Min { get; set; }
    public SpecProd ItemProd { get; set; }
    public SpecProd SeparatorProd { get; set; } // or null

    public override string GetExpected()
    {
        var s = "";
        if (Min > 0)
        {
            s += $"{ItemProd.GetExpected()} ";
        }
        s += $"[ {SeparatorProd?.GetExpected() ?? ""} {ItemProd.GetExpected()} ]*";
        return s;
    }

    public override MatchResult? MatchStep(MatchStack stack, MatchFrame frame, TokenQueue q)
    {
        if (frame.ListState == ListTermState.Start)
        {
            frame.ListState = ListTermState.MatchItem;
            stack.Push(ItemProd);
            return null; // -> MatchItem
        }
        else if (frame.ListState == ListTermState.MatchSeparator)
        {
            var result = frame.SubResult;
            if (result.IsMatch)
            {
                // we have a separator.  now try to match the item following it.
                frame.ListState = ListTermState.MatchItem;
                stack.Push(ItemProd);
                return null; // -> MatchItem
            }
            else if (result.ErrorMessage == null)
            {
                // we didn't find a separator.  this list is done.  back up to the beginning of where
                // the not-separator started and we're done.
                q.Jump(frame.ListSeparatorStartLoc);
                if (frame.ListCount < Min)
                {
                    return MatchResult.Error(
                        $"At least {Min} list item{(Min == 1 ? " is" : "s are")} required, but only "
                            + $"{frame.ListCount} {(frame.ListCount == 1 ? "was" : "were")} provided. "
                            + $"Expected list item: {ItemProd.GetExpected()}"
                    );
                }
                else
                {
                    return MatchResult.Matched;
                }
            }
            else
            {
                return result; // error
            }
        }
        else if (frame.ListState == ListTermState.MatchItem)
        {
            var result = frame.SubResult;
            if (result.IsMatch)
            {
                // we have an item.  is there another?
                frame.ListCount++;
                if (SeparatorProd == null)
                {
                    // there is no separator, so match the next item
                    frame.ListState = ListTermState.MatchItem;
                    frame.ListSeparatorStartLoc = q.GetLocation();
                    stack.Push(ItemProd);
                    return null; // -> MatchItem
                }
                else
                {
                    // match separator + item
                    frame.ListState = ListTermState.MatchSeparator;
                    frame.ListSeparatorStartLoc = q.GetLocation();
                    stack.Push(SeparatorProd);
                    return null; // -> MatchSeparator
                }
            }
            else if (result.ErrorMessage == null)
            {
                if (frame.ListCount == 0)
                {
                    // the first item might be missing because the list can potentially be optional.
                    return Min == 0 ? MatchResult.Matched : MatchResult.NoMatch;
                }
                else if (SeparatorProd == null)
                {
                    // there's no separator, so eventually we'll end up here when the list ends.
                    q.Jump(frame.ListSeparatorStartLoc);
                    if (frame.ListCount < Min)
                    {
                        return MatchResult.Error(
                            $"At least {Min} list item{(Min == 1 ? " is" : "s are")} required, but only "
                                + $"{frame.ListCount} {(frame.ListCount == 1 ? "was" : "were")} provided. "
                                + $"Expected list item: {ItemProd.GetExpected()}"
                        );
                    }
                    else
                    {
                        return MatchResult.Matched;
                    }
                }
                else
                {
                    // subsequent items must be present because, in the MatchItem state, we've already consumed a
                    // separator so there must be an item following it.
                    return MatchResult.Error($"Expected list item: {ItemProd.GetExpected()}");
                }
            }
            else
            {
                return result; // error
            }
        }
        else
        {
            throw new Exception($"Unrecognized state: {frame.ListState}");
        }
    }
}

public sealed class BreakpointTerm : SpecTerm
{
    public override string GetExpected() => "";

    public override MatchResult? MatchStep(MatchStack stack, MatchFrame frame, TokenQueue q)
    {
        System.Diagnostics.Debugger.Break();
        return MatchResult.Matched;
    }
}

public static class Matcher
{
    public static MatchResult Match(string rootProdName, TokenQueue q, out Ast.SqliteSyntaxProduction ast)
    {
        // we use an explicit stack rather than function call recursion because our BNF grammar is deeply nested,
        // particularly the productions for 'expr'.
        var stack = new MatchStack { Queue = q };
        stack.Push(SqliteGrammar.Prods[rootProdName]);
        MatchResult? rootResult = null;
        Ast.SqliteSyntaxProduction rootAst = null;

        Action<MatchResult, Ast.SqliteSyntaxProduction> finishFrame = (frameResult, frameAstProd) =>
        {
            stack.Pop();
            var parentFrame = stack.Peek();
            if (parentFrame == null)
            {
                rootResult = frameResult;
                rootAst = frameAstProd;
            }
            else
            {
                parentFrame.SubResult = frameResult;
                if (frameResult.IsMatch)
                {
                    parentFrame.AstProd.Items.Add(frameAstProd);
                }
            }
        };

#if MATCHER_LOG
        var matcherLogWriter = File.CreateText(@"C:\temp\matcher.log");
        int matcherLogPreviousDepth = 0;
#endif

        // trampoline loop
        while (!rootResult.HasValue && stack.Any())
        {
#if MATCHER_LOG
            stack.DebugDump(
                matcherLogWriter,
                q.GetLocation(),
                q.Substring(q.GetLocation(), 1),
                matcherLogPreviousDepth > stack.Count
            );
            matcherLogPreviousDepth = stack.Count;
#endif

            var frame = stack.Peek();
            var result = frame.Prod.Terms[frame.TermIndex].MatchStep(stack, frame, q);
            if (result.HasValue)
            {
                // we are done matching this term
                if (result.Value.IsMatch)
                {
                    // move to the next term in the production.
                    frame.Clear(all: false);
                    frame.TermIndex++;
                    if (frame.TermIndex >= frame.Prod.Terms.Length)
                    {
                        // we have matched this full production
                        var prodEndLoc = q.GetLocation();
                        frame.AstProd.StartToken = frame.ProdStartLoc;
                        frame.AstProd.NumTokens = prodEndLoc - frame.ProdStartLoc;
                        frame.AstProd.Text = q.Substring(frame.ProdStartLoc, prodEndLoc - frame.ProdStartLoc);
                        finishFrame(MatchResult.Matched, frame.AstProd);
                    }
                }
                else
                {
                    // we needed a match and didn't find one.  we have to abandon this production.
                    finishFrame(result.Value, null);
                }
            }
        }

#if MATCHER_LOG
        matcherLogWriter.Close();
#endif

        if (!rootResult.HasValue && !stack.Any())
        { // detect bugs
            throw new Exception("Expected a MatchResult but one was not set.");
        }

        ast = rootAst;
        return rootResult.Value;
    }
}
