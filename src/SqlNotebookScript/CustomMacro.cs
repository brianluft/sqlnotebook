using SqlNotebookScript.Core;
using SqlNotebookScript.Interpreter;
using Ast = SqlNotebookScript.Interpreter.Ast;

namespace SqlNotebookScript;

public abstract class CustomMacro
{
    public Notebook Notebook { get; set; }

    // return true if a macro expansion was made, false if not
    public abstract bool Apply(Ast.SqlStmt stmt);

    protected Ast.Expr NewExpr(string text)
    {
        var tokens = Notebook.Tokenize(text);
        var q = new TokenQueue(tokens, Notebook);
        Ast.SqliteSyntaxProduction ast;
        var result = SqliteParser.ReadExpr(q, out ast);
        if (result.IsValid && q.Eof())
        {
            return new Ast.Expr { Sql = text, SqliteSyntax = ast };
        }
        else
        {
            throw new MacroProcessorException($"\"{text}\" is not a valid SQL expression.");
        }
    }

    protected Ast.SqlStmt NewSqlStmt(string text)
    {
        var ast = ParseSqlStmt(text);
        return new Ast.SqlStmt
        {
            Sql = text,
            SqliteSyntax = ast,
            FirstTokenIndex = -1,
        };
    }

    protected Ast.SqliteSyntaxProduction ParseSqlStmt(string text)
    {
        var tokens = Notebook.Tokenize(text);
        var q = new TokenQueue(tokens, Notebook);
        Ast.SqliteSyntaxProduction ast;
        var result = SqliteParser.ReadStmt(q, out ast);
        if (result.IsValid && q.Eof())
        {
            return ast;
        }
        else
        {
            throw new MacroProcessorException($"\"{text}\" is not a valid SQL statement.");
        }
    }
}
