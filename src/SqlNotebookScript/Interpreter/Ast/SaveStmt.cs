namespace SqlNotebookScript.Interpreter.Ast;

public sealed class SaveStmt : Stmt
{
    public IdentifierOrExpr FilenameExpr { get; set; } // may be null

    protected override Node GetChild() => FilenameExpr;
}
