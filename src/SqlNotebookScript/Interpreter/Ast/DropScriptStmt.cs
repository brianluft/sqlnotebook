using System.Collections.Generic;

namespace SqlNotebookScript.Interpreter.Ast;

public sealed class DropScriptStmt : Stmt
{
    public IdentifierOrExpr ScriptName { get; set; }

    protected override IEnumerable<Node> GetChildren() => new Node[] { ScriptName };
}
