using System;
using System.Collections.Generic;
using System.Linq;
using SqlNotebookScript.Interpreter;
using SqlNotebookScript.Utils;
using Ast = SqlNotebookScript.Interpreter.Ast;

namespace SqlNotebookScript.Macros;

public sealed class ReadCsvMacro : CustomMacro
{
    public override bool Apply(Ast.SqlStmt input)
    {
        var tableFunctionCallNodes = input
            .Traverse()
            .OfType<Ast.SqliteSyntaxProduction>()
            .Where(x => x.Name == "table-or-subquery.table-function-call");
        foreach (var tableFunctionCallNode in tableFunctionCallNodes)
        {
            var functionName = tableFunctionCallNode
                .Traverse()
                .OfType<Ast.SqliteSyntaxProduction>()
                .FirstOrDefault(x => x.Name == "table-or-subquery.table-function-name")
                ?.Text;
            if (functionName.ToLowerInvariant() != "read_csv")
            {
                continue;
            }

            // read_csv(file-path, [has-header-row], [skip-rows], [file-encoding])
            var args = tableFunctionCallNode
                .TraverseDottedChildren()
                .Where(x => x.Name == "table-or-subquery.arg")
                .ToList();
            if (args.Count < 1 || args.Count > 4)
            {
                throw new MacroProcessorException($"READ_CSV: Between 1 and 4 arguments are required.");
            }

            var filePathExpr = args[0];
            var hasHeaderRowExpr = args.Count >= 2 ? args[1] : null;
            var skipRowsExpr = args.Count >= 3 ? args[2] : null;
            var fileEncodingExpr = args.Count >= 4 ? args[3] : null;
            var tempTableName = Guid.NewGuid().ToString();

            var importStmt = new Ast.ImportCsvStmt
            {
                SourceToken = tableFunctionCallNode.SourceToken,
                FilenameExpr = NewExpr(filePathExpr.Text),
                ImportTable = new Ast.ImportTable
                {
                    TableName = new Ast.IdentifierOrExpr { Identifier = tempTableName },
                },
                OptionsList = new Ast.OptionsList
                {
                    Options = new Dictionary<string, Ast.Expr>
                    {
                        ["temporary_table"] = NewExpr("1"),
                        ["skip_lines"] = NewExpr(skipRowsExpr?.Text ?? "0"),
                        ["header_row"] = NewExpr(hasHeaderRowExpr?.Text ?? "1"),
                        ["file_encoding"] = NewExpr(fileEncodingExpr?.Text ?? "0"),
                    },
                },
            };
            foreach (var n in importStmt.Traverse())
            {
                n.SourceToken = tableFunctionCallNode.SourceToken;
            }
            input.RunBefore.Add(importStmt);

            var dropTableStmt = NewSqlStmt($"DROP TABLE IF EXISTS {tempTableName.DoubleQuote()}");
            input.RunAfter.Add(dropTableStmt);

            // replace the READ_CSV(...) call with the table name
            var originalTokens = Core.Notebook.Tokenize(input.Sql).Select(x => x.Text).ToList();
            var tokenReplaceIndex = tableFunctionCallNode.StartToken - input.FirstTokenIndex;
            for (var i = 0; i < tableFunctionCallNode.NumTokens; i++)
            {
                originalTokens.RemoveAt(tokenReplaceIndex);
            }
            originalTokens.Insert(tokenReplaceIndex, tempTableName.DoubleQuote());
            var newSqlText = string.Join(" ", originalTokens);
            input.Sql = newSqlText;
            input.SqliteSyntax = ParseSqlStmt(newSqlText);
            return true;
        }
        return false;
    }
}
