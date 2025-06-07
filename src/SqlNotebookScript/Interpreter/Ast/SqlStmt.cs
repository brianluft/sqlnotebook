using System.Collections.Generic;
using System.Linq;

namespace SqlNotebookScript.Interpreter.Ast;

public sealed class SqlStmt : Stmt
{
    public string Sql { get; set; }
    public SqliteSyntaxProduction SqliteSyntax { get; set; }
    public int FirstTokenIndex { get; set; }

    // statements may be added here by the preprocessor
    public List<Stmt> RunBefore { get; set; } = new List<Stmt>();
    public List<Stmt> RunAfter { get; set; } = new List<Stmt>();

    protected override IEnumerable<Node> GetChildren() =>
        new Node[] { SqliteSyntax }
            .Concat(RunBefore)
            .Concat(RunAfter);
}
