using System.Collections.Generic;

namespace SqlNotebookScript.Interpreter.Ast;

public sealed class CreateScriptStmt : Stmt
{
    public IdentifierOrExpr ScriptName { get; set; }
    public IdentifierOrExpr SqlCommands { get; set; }

    protected override IEnumerable<Node> GetChildren() => new Node[] { ScriptName, SqlCommands };
}
