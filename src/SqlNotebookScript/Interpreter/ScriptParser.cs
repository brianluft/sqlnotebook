using System.Collections.Generic;
using System.Text.RegularExpressions;
using SqlNotebookScript.Core;

namespace SqlNotebookScript.Interpreter;

public sealed class ScriptParser
{
    private readonly Notebook _notebook;
    private readonly MacroProcessor _preprocessor;

    public ScriptParser(Notebook notebook)
    {
        _notebook = notebook;
        _preprocessor = new MacroProcessor(notebook);
    }

    public Ast.Script Parse(string input)
    {
        var tokens = new TokenQueue(Notebook.Tokenize(input), _notebook);
        return ParseScript(tokens);
    }

    private Ast.Script ParseScript(TokenQueue q)
    {
        var script = new Ast.Script { SourceToken = q.SourceToken };
        script.Block = new Ast.Block { SourceToken = q.SourceToken };
        while (!q.Eof())
        {
            var stmt = ParseStmt(q);
            if (stmt != null)
            {
                script.Block.Statements.Add(stmt);
            }
        }
        return script;
    }

    private Ast.Stmt ParseStmt(TokenQueue q)
    { // or null
        switch (q.Peek(0))
        {
            case ";":
                q.Take(";");
                return null;
            case "declare":
                return ParseDeclareStmt(q);
            case "while":
                return ParseWhileStmt(q);
            case "break":
                return ParseBreakStmt(q);
            case "continue":
                return ParseContinueStmt(q);
            case "print":
                return ParsePrintStmt(q);
            case "exec":
            case "execute":
                return ParseExecuteStmt(q);
            case "return":
                return ParseReturnStmt(q);
            case "throw":
                return ParseThrowStmt(q);
            case "save":
                return ParseSaveStmt(q);
            case "set":
                return ParseSetStmt(q);
            case "if":
                return ParseIfStmt(q);
            case "begin":
                return q.Peek(1) == "try" ? ParseTryCatchStmt(q) : ParseSqlStmt(q);
            case "import":
                return ParseImportStmt(q);
            case "export":
                return ParseExportStmt(q);
            case "for":
                return ParseForStmt(q);
            case "foreach":
                return ParseForeachStmt(q);
            case "drop":
                return q.Peek(1) switch
                {
                    "script" => ParseDropScriptStmt(q),
                    "page" => ParseDropPageStmt(q),
                    _ => ParseSqlStmt(q),
                };
            case "create":
                return q.Peek(1) switch
                {
                    "script" => ParseCreateScriptStmt(q),
                    _ => ParseSqlStmt(q),
                };
            default:
                return ParseSqlStmt(q);
        }
    }

    private Ast.DeclareStmt ParseDeclareStmt(TokenQueue q)
    {
        var stmt = new Ast.DeclareStmt { SourceToken = q.SourceToken };
        q.Take("declare");
        if (q.Peek() == "parameter")
        {
            q.Take();
            stmt.IsParameter = true;
        }
        ParseAssignmentStmtCore(q, stmt);
        return stmt;
    }

    private Ast.Stmt ParseSetStmt(TokenQueue q)
    {
        var stmt = new Ast.SetStmt { SourceToken = q.SourceToken };
        q.Take("set");
        ParseAssignmentStmtCore(q, stmt);
        return stmt;
    }

    private void ParseAssignmentStmtCore(TokenQueue q, Ast.AssignmentStmt stmt)
    {
        stmt.VariableName = ParseVariableName(q);
        if (q.Peek() == "=")
        {
            q.Take();
            stmt.InitialValue = ParseExpr(q);
        }
        ConsumeSemicolon(q);
    }

    private Ast.Stmt ParseWhileStmt(TokenQueue q)
    {
        var stmt = new Ast.WhileStmt { SourceToken = q.SourceToken };
        q.Take("while");
        stmt.Condition = ParseExpr(q);
        stmt.Block = ParseBlock(q);
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.Stmt ParseForStmt(TokenQueue q)
    {
        var stmt = new Ast.ForStmt { SourceToken = q.SourceToken };
        q.Take("for");
        stmt.VariableName = ParseVariableName(q);
        q.Take("=");
        stmt.FirstNumberExpr = ParseExpr(q);
        q.Take("to");
        stmt.LastNumberExpr = ParseExpr(q);
        if (q.TakeMaybe("step"))
        {
            stmt.StepExpr = ParseExpr(q);
        }
        stmt.Block = ParseBlock(q);
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.Stmt ParseForeachStmt(TokenQueue q)
    {
        var stmt = new Ast.ForeachStmt { SourceToken = q.SourceToken };
        q.Take("foreach");
        q.Take("(");

        // Parse comma-separated list of variable names
        stmt.VariableNames = new List<string>();
        do
        {
            stmt.VariableNames.Add(ParseVariableName(q));
        } while (q.TakeMaybe(","));

        q.Take(")");
        q.Take("in");
        stmt.TableExpr = ParseIdentifierOrExpr(q);
        stmt.Block = ParseBlock(q);
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.Stmt ParseBreakStmt(TokenQueue q)
    {
        var stmt = new Ast.BreakStmt { SourceToken = q.SourceToken };
        q.Take("break");
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.Stmt ParseContinueStmt(TokenQueue q)
    {
        var stmt = new Ast.ContinueStmt { SourceToken = q.SourceToken };
        q.Take("continue");
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.Stmt ParsePrintStmt(TokenQueue q)
    {
        var stmt = new Ast.PrintStmt { SourceToken = q.SourceToken };
        q.Take("print");
        stmt.Value = ParseExpr(q);
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.Stmt ParseExecuteStmt(TokenQueue q)
    {
        var stmt = new Ast.ExecuteStmt { SourceToken = q.SourceToken };
        q.Take("exec", "execute");

        if (q.Peek(1) == "=")
        {
            stmt.ReturnVariableName = ParseVariableName(q);
            q.Take("=");
        }

        if (q.PeekToken().Type == TokenType.String || q.PeekToken().Type == TokenType.Id)
        {
            stmt.ScriptName = q.Take().GetUnescapedText();
        }
        else
        {
            throw new SyntaxException(new[] { "string", "identifier" }, q);
        }

        if (IsVariableName(q.PeekToken()?.GetUnescapedText() ?? "") && q.Peek(1) == "=")
        {
            while (true)
            {
                var arg = new Ast.ArgumentPair();
                arg.Name = ParseVariableName(q);
                q.Take("=");
                if (q.Peek() == "default")
                {
                    q.Take();
                }
                else
                {
                    arg.Value = ParseExpr(q);
                }
                stmt.Arguments.Add(arg);
                if (!q.TakeMaybe(","))
                {
                    break;
                }
            }
        }

        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.Stmt ParseReturnStmt(TokenQueue q)
    {
        var stmt = new Ast.ReturnStmt { SourceToken = q.SourceToken };
        q.Take("return");
        if (PeekExpr(q))
        {
            stmt.Value = ParseExpr(q);
        }
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.Stmt ParseThrowStmt(TokenQueue q)
    {
        var stmt = new Ast.ThrowStmt { SourceToken = q.SourceToken };
        q.Take("throw");
        if (PeekExpr(q))
        {
            stmt.HasErrorValues = true;
            stmt.Message = ParseExpr(q);
        }
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.Stmt ParseSaveStmt(TokenQueue q)
    {
        var stmt = new Ast.SaveStmt { SourceToken = q.SourceToken };
        q.Take("save");
        if (PeekExpr(q))
        {
            stmt.FilenameExpr = ParseIdentifierOrExpr(q);
        }
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.Stmt ParseIfStmt(TokenQueue q)
    {
        var stmt = new Ast.IfStmt { SourceToken = q.SourceToken };
        q.Take("if");
        stmt.Condition = ParseExpr(q);
        stmt.Block = ParseBlock(q);
        if (q.Peek() == "else")
        {
            q.Take("else");
            stmt.ElseBlock = ParseBlock(q);
        }
        return stmt;
    }

    private Ast.Stmt ParseTryCatchStmt(TokenQueue q)
    {
        var stmt = new Ast.TryCatchStmt { SourceToken = q.SourceToken };

        q.Take("begin");
        q.Take("try");
        stmt.TryBlock = new Ast.Block { SourceToken = q.SourceToken };
        while (q.Peek() != "end")
        {
            var tryStmt = ParseStmt(q);
            if (tryStmt != null)
            {
                stmt.TryBlock.Statements.Add(tryStmt);
            }
        }
        q.Take("end");
        q.Take("try");

        q.Take("begin");
        q.Take("catch");
        stmt.CatchBlock = new Ast.Block { SourceToken = q.SourceToken };
        while (q.Peek() != "end")
        {
            var catchStmt = ParseStmt(q);
            if (catchStmt != null)
            {
                stmt.CatchBlock.Statements.Add(catchStmt);
            }
        }
        q.Take("end");
        q.Take("catch");
        return stmt;
    }

    private Ast.Stmt ParseDropScriptStmt(TokenQueue q)
    {
        var stmt = new Ast.DropScriptStmt { SourceToken = q.SourceToken };
        q.Take("drop");
        q.Take("script");
        stmt.ScriptName = ParseIdentifierOrExpr(q);
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.Stmt ParseDropPageStmt(TokenQueue q)
    {
        var stmt = new Ast.DropPageStmt { SourceToken = q.SourceToken };
        q.Take("drop");
        q.Take("page");
        stmt.PageName = ParseIdentifierOrExpr(q);
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.Stmt ParseCreateScriptStmt(TokenQueue q)
    {
        var stmt = new Ast.CreateScriptStmt { SourceToken = q.SourceToken };
        q.Take("create");
        q.Take("script");
        stmt.ScriptName = ParseIdentifierOrExpr(q);
        q.Take("as");
        stmt.SqlCommands = ParseIdentifierOrExpr(q);
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.Stmt ParseImportStmt(TokenQueue q) =>
        q.Peek(1) switch
        {
            "csv" => Check(q, ParseImportCsvStmt(q)),
            "txt" or "text" => Check(q, ParseImportTxtStmt(q)),
            "xls" or "xlsx" => Check(q, ParseImportXlsStmt(q)),
            "database" => Check(q, ParseImportDatabaseStmt(q)),
            _ => throw new SyntaxException($"Unknown import type: \"{q.Peek(1)}\""),
        };

    private Ast.Stmt ParseExportStmt(TokenQueue q) =>
        q.Peek(1) switch
        {
            "txt" or "text" => Check(q, ParseExportTxtStmt(q)),
            "csv" => Check(q, ParseExportCsvStmt(q)),
            _ => throw new SyntaxException($"Unknown export type: \"{q.Peek(1)}\""),
        };

    private Ast.ImportCsvStmt ParseImportCsvStmt(TokenQueue q)
    { // or null
        var stmt = new Ast.ImportCsvStmt { SourceToken = q.SourceToken };
        var start = q.GetLocation();
        if (!q.TakeMaybe("import"))
        {
            q.Jump(start);
            return null;
        }
        if (!q.TakeMaybe("csv"))
        {
            q.Jump(start);
            return null;
        }
        stmt.FilenameExpr = ParseExpr(q);
        q.Take("into");
        stmt.ImportTable = Check(q, ParseImportTable(q));
        if (q.TakeMaybe("options"))
        {
            stmt.OptionsList = Check(q, ParseOptionsList(q));
        }
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.ImportXlsStmt ParseImportXlsStmt(TokenQueue q)
    { // or null
        var stmt = new Ast.ImportXlsStmt { SourceToken = q.SourceToken };
        var start = q.GetLocation();
        if (!q.TakeMaybe("import"))
        {
            q.Jump(start);
            return null;
        }
        if (!q.TakeMaybe("xls", "xlsx"))
        {
            q.Jump(start);
            return null;
        }
        stmt.FilenameExpr = ParseExpr(q);
        if (q.TakeMaybe("worksheet"))
        {
            stmt.WhichSheetExpr = ParseExpr(q);
        }
        q.Take("into");
        stmt.ImportTable = Check(q, ParseImportTable(q));
        if (q.TakeMaybe("options"))
        {
            stmt.OptionsList = Check(q, ParseOptionsList(q));
        }
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.ImportDatabaseStmt ParseImportDatabaseStmt(TokenQueue q)
    {
        Ast.ImportDatabaseStmt stmt = new() { SourceToken = q.SourceToken };
        q.Take("import");
        q.Take("database");
        stmt.VendorExpr = ParseExpr(q);
        q.Take("connection");
        stmt.ConnectionStringExpr = ParseExpr(q);
        if (q.TakeMaybe("schema"))
        {
            stmt.SrcSchemaNameExprOrNull = ParseIdentifierOrExpr(q);
            q.Take("table");
            stmt.SrcTableNameExprOrNull = ParseIdentifierOrExpr(q);
        }
        else if (q.TakeMaybe("table"))
        {
            stmt.SrcTableNameExprOrNull = ParseIdentifierOrExpr(q);
        }
        else if (q.TakeMaybe("query"))
        {
            stmt.SqlExprOrNull = ParseExpr(q);
        }
        else
        {
            q.Take("schema", "table", "query"); // This will throw.
        }
        if (q.TakeMaybe("into"))
        {
            stmt.DstTableNameExprOrNull = ParseIdentifierOrExpr(q);
        }
        if (q.TakeMaybe("options"))
        {
            stmt.OptionsList = Check(q, ParseOptionsList(q));
        }
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.ImportTable ParseImportTable(TokenQueue q)
    {
        var n = new Ast.ImportTable { SourceToken = q.SourceToken };
        n.TableName = Check(q, ParseIdentifierOrExpr(q));
        if (q.Peek() == "(")
        {
            q.Take("(");
            do
            {
                n.ImportColumns.Add(Check(q, ParseImportColumn(q)));
            } while (q.TakeMaybe(","));
            q.Take(")");
        }
        return n;
    }

    private Ast.ImportColumn ParseImportColumn(TokenQueue q)
    {
        var n = new Ast.ImportColumn { SourceToken = q.SourceToken };
        Check(q, n.ColumnName = ParseIdentifierOrExpr(q));
        if (q.Peek() == "as")
        {
            q.Take("as");
            n.AsName = Check(q, ParseIdentifierOrExpr(q));
        }
        switch (q.Peek())
        {
            case "text":
                q.Take();
                n.TypeConversion = Ast.TypeConversion.Text;
                break;
            case "integer":
                q.Take();
                n.TypeConversion = Ast.TypeConversion.Integer;
                break;
            case "real":
                q.Take();
                n.TypeConversion = Ast.TypeConversion.Real;
                break;
            case "date":
                q.Take();
                n.TypeConversion = Ast.TypeConversion.Date;
                break;
            case "datetime":
                q.Take();
                n.TypeConversion = Ast.TypeConversion.DateTime;
                break;
        }
        return n;
    }

    private Ast.OptionsList ParseOptionsList(TokenQueue q)
    { // or null
        var n = new Ast.OptionsList { SourceToken = q.SourceToken };
        if (!q.TakeMaybe("("))
        {
            return null;
        }
        do
        {
            var key = Check(q, ParseIdentifier(q)).ToLower();
            q.Take(":");
            var value = ParseExpr(q);
            n.Options[key] = value;
        } while (q.TakeMaybe(","));
        q.Take(")");
        return n;
    }

    private Ast.IdentifierOrExpr ParseIdentifierOrExpr(TokenQueue q)
    { // or null
        var token = q.PeekToken();
        if (token.Type == TokenType.Id)
        {
            q.Take();
            return new Ast.IdentifierOrExpr { SourceToken = token, Identifier = token.GetUnescapedText() };
        }
        else if (PeekExpr(q))
        {
            return new Ast.IdentifierOrExpr { SourceToken = token, Expr = ParseExpr(q) };
        }
        else
        {
            return null;
        }
    }

    private string ParseIdentifier(TokenQueue q)
    { // or null
        var token = q.PeekToken();
        if (token.Type == TokenType.Id)
        {
            return q.Take().GetUnescapedText();
        }
        else
        {
            return null;
        }
    }

    private Ast.ImportTxtStmt ParseImportTxtStmt(TokenQueue q)
    { // or null
        var stmt = new Ast.ImportTxtStmt { SourceToken = q.SourceToken };
        var start = q.GetLocation();
        if (!q.TakeMaybe("import"))
        {
            q.Jump(start);
            return null;
        }
        if (!q.TakeMaybe("txt", "text"))
        {
            q.Jump(start);
            return null;
        }
        stmt.FilenameExpr = ParseExpr(q);
        q.Take("into");
        stmt.TableName = Check(q, ParseIdentifierOrExpr(q));
        if (q.Peek() == "(")
        {
            q.Take("(");
            stmt.LineNumberColumnName = Check(q, ParseIdentifierOrExpr(q));
            q.Take(",");
            stmt.TextColumnName = Check(q, ParseIdentifierOrExpr(q));
            q.Take(")");
        }
        if (q.TakeMaybe("options"))
        {
            stmt.OptionsList = Check(q, ParseOptionsList(q));
        }
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.ExportCsvStmt ParseExportCsvStmt(TokenQueue q)
    {
        Ast.ExportCsvStmt stmt = new() { SourceToken = q.SourceToken };
        q.Take("export");
        q.Take("csv");
        stmt.FilenameExpr = ParseExpr(q);
        q.Take("from");
        if (q.TakeMaybe("table"))
        {
            stmt.TableNameOrNull = ParseIdentifierOrExpr(q);
        }
        else if (q.TakeMaybe("script"))
        {
            stmt.ScriptNameOrNull = ParseIdentifierOrExpr(q);
        }
        else if (q.TakeMaybe("("))
        {
            stmt.SelectStmtOrNull = ParseSqlStmt(q, "select-stmt");
            q.Take(")");
        }
        else
        {
            q.Take("table", "script", "("); // throw
        }
        if (q.TakeMaybe("options"))
        {
            stmt.OptionsList = Check(q, ParseOptionsList(q));
        }
        ConsumeSemicolon(q);
        return stmt;
    }

    private Ast.ExportTxtStmt ParseExportTxtStmt(TokenQueue q)
    { // or null
        var stmt = new Ast.ExportTxtStmt { SourceToken = q.SourceToken };
        var start = q.GetLocation();
        if (!q.TakeMaybe("export"))
        {
            q.Jump(start);
            return null;
        }
        if (!q.TakeMaybe("txt", "text"))
        {
            q.Jump(start);
            return null;
        }
        stmt.FilenameExpr = ParseExpr(q);
        q.Take("from");
        q.Take("(");
        stmt.SelectStmt = ParseSqlStmt(q, "select-stmt");
        q.Take(")");
        if (q.TakeMaybe("options"))
        {
            stmt.OptionsList = Check(q, ParseOptionsList(q));
        }
        ConsumeSemicolon(q);
        return stmt;
    }

    // ----

    private static T Check<T>(TokenQueue q, T shouldBeNonNull)
    {
        if (shouldBeNonNull == null)
        {
            throw new SyntaxException(q);
        }
        else
        {
            return shouldBeNonNull;
        }
    }

    private static string ParseVariableName(TokenQueue q)
    {
        var t = q.PeekToken();
        if (t.Type == TokenType.Variable && IsVariableName(t.GetUnescapedText()))
        {
            q.Take();
            return t.GetUnescapedText();
        }
        else
        {
            throw new SyntaxException(new[] { "variable name starting with @ $ :" }, q);
        }
    }

    private static readonly Regex _variableNameRegex = new Regex(@"^[:@$][A-Za-z_][A-Za-z0-9_]*$");

    private static bool IsVariableName(string tok)
    {
        return _variableNameRegex.IsMatch(tok);
    }

    private Ast.Block ParseBlock(TokenQueue q)
    {
        var stmt = new Ast.Block { SourceToken = q.SourceToken };
        if (q.Peek() == "begin")
        {
            q.Take("begin");
            while (q.Peek() != "end")
            {
                var blockStmt = ParseStmt(q);
                if (blockStmt != null)
                {
                    stmt.Statements.Add(blockStmt);
                }
            }
            q.Take("end");
        }
        else
        {
            var blockStmt = ParseStmt(q);
            if (blockStmt != null)
            {
                stmt.Statements.Add(blockStmt);
            }
        }
        return stmt;
    }

    private Ast.SqlStmt ParseSqlStmt(TokenQueue q, string rootProdName = "sql-stmt")
    {
        var start = q.GetLocation();
        var tok = q.SourceToken;
        Ast.SqliteSyntaxProduction syntaxNode;
        var result = SqliteParser.ReadStmt(q, out syntaxNode, rootProdName);
        if (result.IsValid)
        {
            var stmt = new Ast.SqlStmt
            {
                Sql = q.Substring(start, result.NumValidTokens),
                SourceToken = tok,
                SqliteSyntax = syntaxNode,
                FirstTokenIndex = start,
            };
            _preprocessor.PreprocessStmt(stmt);
            return stmt;
        }
        else if (result.InvalidMessage != null)
        {
            throw new SyntaxException(result.InvalidMessage);
        }
        else
        {
            throw new SyntaxException(q);
        }
    }

    private Ast.Expr ParseExpr(TokenQueue q)
    {
        var start = q.GetLocation();
        var tok = q.SourceToken;
        Ast.SqliteSyntaxProduction syntaxNode;
        var result = SqliteParser.ReadExpr(q, out syntaxNode);
        if (result.IsValid)
        {
            return new Ast.Expr
            {
                Sql = q.Substring(start, result.NumValidTokens),
                SourceToken = tok,
                SqliteSyntax = syntaxNode,
            };
        }
        else if (result.InvalidMessage != null)
        {
            throw new SyntaxException(result.InvalidMessage);
        }
        else
        {
            throw new SyntaxException(q);
        }
    }

    private bool PeekExpr(TokenQueue q)
    {
        var start = q.GetLocation();
        var tok = q.SourceToken;
        Ast.SqliteSyntaxProduction ast;
        var result = SqliteParser.ReadExpr(q, out ast);
        q.Jump(start);
        return result.IsValid;
    }

    private void ConsumeSemicolon(TokenQueue q)
    {
        if (q.Peek() == ";")
        {
            q.Take();
        }
    }
}
