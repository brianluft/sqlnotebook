using System.Collections.Generic;

namespace SqlNotebookScript.Interpreter.Ast;

public sealed class ForeachStmt : Stmt
{
    public List<string> VariableNames { get; set; }
    public IdentifierOrExpr TableExpr { get; set; }
    public Block Block { get; set; }

    protected override IEnumerable<Node> GetChildren() => new Node[] { TableExpr, Block };
}
