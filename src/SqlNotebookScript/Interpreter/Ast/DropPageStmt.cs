using System.Collections.Generic;

namespace SqlNotebookScript.Interpreter.Ast;

public sealed class DropPageStmt : Stmt
{
    public IdentifierOrExpr PageName { get; set; }

    protected override IEnumerable<Node> GetChildren() => new Node[] { PageName };
}
