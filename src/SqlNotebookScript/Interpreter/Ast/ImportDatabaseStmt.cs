using System.Collections.Generic;

namespace SqlNotebookScript.Interpreter.Ast;

public sealed class ImportDatabaseStmt : Stmt
{
    public Expr VendorExpr { get; set; }
    public Expr ConnectionStringExpr { get; set; }
    public IdentifierOrExpr SrcSchemaNameExprOrNull { get; set; }
    public IdentifierOrExpr SrcTableNameExprOrNull { get; set; }
    public Expr SqlExprOrNull { get; set; }
    public IdentifierOrExpr DstTableNameExprOrNull { get; set; }
    public OptionsList OptionsList { get; set; }

    protected override IEnumerable<Node> GetChildren() =>
        new Node[]
        {
            VendorExpr,
            ConnectionStringExpr,
            SrcSchemaNameExprOrNull,
            SrcTableNameExprOrNull,
            SqlExprOrNull,
            DstTableNameExprOrNull,
            OptionsList,
        };
}
