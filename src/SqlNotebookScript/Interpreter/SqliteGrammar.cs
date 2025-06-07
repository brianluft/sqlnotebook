using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlNotebookScript.Interpreter;

public static class SqliteGrammar
{
    private static readonly Dictionary<string, SpecProd> _dict = new Dictionary<string, SpecProd>();
    public static IReadOnlyDictionary<string, SpecProd> Prods { get; } = _dict;

    static SqliteGrammar()
    {
        string p;

        // sql-stmt ::= [ EXPLAIN [ QUERY PLAN ] ] ( <alter-table-stmt> | <analyze-stmt> | <attach-stmt> |
        //      <begin-stmt> | <commit-stmt> | <create-index-stmt> | <create-table-stmt> | <create-trigger-stmt> |
        //      <create-view-stmt> | <create-virtual-table-stmt> | <delete-stmt> |
        //      <detach-stmt> | <drop-index-stmt> | <drop-table-stmt> | <drop-trigger-stmt> | <drop-view-stmt> |
        //      <insert-stmt> | <pragma-stmt> | <reindex-stmt> | <release-stmt> | <rollback-stmt> |
        //      <savepoint-stmt> | <select-stmt> | <update-stmt> | <update-stmt-limited> | <vacuum-stmt> )
        TopProd(
            p = "sql-stmt",
            2,
            Opt(1, Tok(TokenType.Explain), Opt(1, Tok(TokenType.Query), Tok(TokenType.Plan))),
            Or(
                SubProd("select-stmt"),
                SubProd("update-stmt"),
                SubProd("insert-stmt"),
                SubProd("alter-table-stmt"),
                SubProd("analyze-stmt"),
                SubProd("attach-stmt"),
                SubProd("begin-stmt"),
                SubProd("commit-stmt"),
                SubProd("create-index-stmt"),
                SubProd("create-table-stmt"),
                SubProd("create-trigger-stmt"),
                SubProd("create-view-stmt"),
                SubProd("create-virtual-table-stmt"),
                SubProd("delete-stmt"),
                SubProd("detach-stmt"),
                SubProd("drop-index-stmt"),
                SubProd("drop-table-stmt"),
                SubProd("drop-trigger-stmt"),
                SubProd("drop-view-stmt"),
                SubProd("pragma-stmt"),
                SubProd("reindex-stmt"),
                SubProd("release-stmt"),
                SubProd("rollback-stmt"),
                SubProd("savepoint-stmt"),
                SubProd("vacuum-stmt")
            )
        );

        // alter-table-stmt ::=
        //      ALTER TABLE [ database-name "." ] table-name
        //      (
        //          ( RENAME TO new-table-name ) |
        //          ( RENAME [COLUMN] column-name TO new-column-name ) |
        //          ( ADD [COLUMN] <column-def> ) |
        //          ( DROP [COLUMN] column-name )
        //      )
        TopProd(
            p = "alter-table-stmt",
            1,
            Tok(TokenType.Alter),
            Tok(TokenType.Table),
            Opt(Id("database name"), Tok(TokenType.Dot)),
            Id("table name"),
            Or(
                Prod(
                    $"{p}.rename",
                    1,
                    Tok(TokenType.Rename),
                    Or(
                        Prod($"{p}.rename-table", 1, Tok(TokenType.To), Id("new table name")),
                        Prod(
                            $"{p}.rename-column",
                            1,
                            Opt(Tok(TokenType.Columnkw)),
                            Id("column name"),
                            Tok(TokenType.To),
                            Id("new column name")
                        )
                    )
                ),
                Prod($"{p}.add", 1, Tok(TokenType.Add), Opt(Tok(TokenType.Columnkw)), SubProd("column-def")),
                Prod($"{p}.drop", 1, Tok(TokenType.Drop), Opt(Tok(TokenType.Columnkw)), Id("column name"))
            )
        );

        // analyze-stmt ::= ANALYZE [ database-table-index-name [ "." table-or-index-name ] ]
        TopProd(
            p = "analyze-stmt",
            1,
            Tok(TokenType.Analyze),
            Opt(Id("database, table, or index name"), Opt(1, Tok(TokenType.Dot), Id("table or index name")))
        );

        // attach-stmt ::= ATTACH [ DATABASE ] <expr> AS database-name
        TopProd(
            p = "attach-stmt",
            1,
            Tok(TokenType.Attach),
            Opt(Tok(TokenType.Database)),
            SubProd("expr"),
            Tok(TokenType.As),
            Id("database name")
        );

        // begin-stmt ::= BEGIN [ DEFERRED | IMMEDIATE | EXCLUSIVE ] [ TRANSACTION ]
        TopProd(
            p = "begin-stmt",
            1,
            Tok(TokenType.Begin),
            Opt(Or(Tok(TokenType.Deferred), Tok(TokenType.Immediate), Tok(TokenType.Exclusive))),
            Opt(Tok(TokenType.Transaction))
        );

        // commit-stmt ::= ( COMMIT | END ) [ TRANSACTION ]
        TopProd(p = "commit-stmt", 1, Or(Tok(TokenType.Commit), Tok(TokenType.End)), Opt(Tok(TokenType.Transaction)));

        // rollback-stmt ::= ROLLBACK [ TRANSACTION ] [ TO [ SAVEPOINT ] savepoint-name ]
        TopProd(
            p = "rollback-stmt",
            1,
            Tok(TokenType.Rollback),
            Opt(Tok(TokenType.Transaction)),
            Opt(1, Tok(TokenType.To), Opt(Tok(TokenType.Savepoint)), Id("savepoint name"))
        );

        // savepoint-stmt ::= SAVEPOINT savepoint-name
        TopProd(p = "savepoint-stmt", 1, Tok(TokenType.Savepoint), Id("savepoint name"));

        // release-stmt ::= RELEASE [ SAVEPOINT ] savepoint-name
        TopProd(p = "release-stmt", 1, Tok(TokenType.Release), Opt(Tok(TokenType.Savepoint)), Id("savepoint name"));

        // create-index-stmt ::= CREATE [ UNIQUE ] INDEX [ IF NOT EXISTS ]
        //      [ database-name "." ] index-name ON table-name "(" <indexed-column> [ "," <indexed-column> ]* ")"
        //      [ WHERE <expr> ]
        TopProd(
            p = "create-index-stmt",
            3,
            Tok(TokenType.Create),
            Opt(Tok(TokenType.Unique)),
            Tok(TokenType.Index),
            Opt(1, Tok(TokenType.If), Tok(TokenType.Not), Tok(TokenType.Exists)),
            Opt(Id("database name"), Tok(TokenType.Dot)),
            Id("index name"),
            Tok(TokenType.On),
            Id("table name"),
            Tok(TokenType.Lp),
            Lst($"{p}.column", TokenType.Comma, 1, SubProd("indexed-column")),
            Tok(TokenType.Rp),
            Opt(1, Tok(TokenType.Where), SubProd("expr"))
        );

        // indexed-column ::= ( column-name | expr ) [ COLLATE collation-name ] [ ASC | DESC ]
        TopProd(
            p = "indexed-column",
            1,
            Or(SubProd("expr"), Id("column name")),
            Opt(1, Tok(TokenType.Collate), Id("collation name")),
            Opt(Or(Tok(TokenType.Asc), Tok(TokenType.Desc)))
        );

        // table-options-item ::= WITHOUT ROWID | STRICT
        // table-options ::= <table-options-item> [ "," <table-options-item> ]*
        TopProd(
            p = "table-options-item",
            1,
            Or(
                Prod($"{p}.without-rowid", 1, Tok(TokenType.Without), Tok("rowid")),
                Prod($"{p}.strict", 1, Tok("strict"))
            )
        );
        TopProd(p = "table-options", 1, Lst($"{p}.item", TokenType.Comma, 1, SubProd("table-options-item")));

        // create-table-stmt ::= CREATE [ TEMP | TEMPORARY ] TABLE [ IF NOT EXISTS ]
        //      [database-name "."] table-name
        //      (
        //          "(" <column-def> [ "," <column-def> ]* [ "," <table-constraint> ]* ")"
        //          (WITHOUT ROWID)*
        //        | AS <select-stmt>
        //      )
        TopProd(
            p = "create-table-stmt",
            3,
            Tok(TokenType.Create),
            Opt(Or(Tok(TokenType.Temp), Tok("temporary"))),
            Tok(TokenType.Table),
            Opt(1, Tok(TokenType.If), Tok(TokenType.Not), Tok(TokenType.Exists)),
            Opt(Id("database name"), Tok(TokenType.Dot)),
            Id("table name"),
            Or(
                Prod(
                    $"{p}.column-defs",
                    1,
                    Tok(TokenType.Lp),
                    Lst($"{p}.def", TokenType.Comma, 1, Or(SubProd("table-constraint"), SubProd("column-def"))),
                    // this definition allows column definitions to appear after table constraints, which is illegal.
                    // it would be nice to improve here.
                    Tok(TokenType.Rp),
                    Opt(1, SubProd("table-options"))
                ),
                Prod($"{p}.as", 1, Tok(TokenType.As), SubProd("select-stmt"))
            )
        );

        // column-def ::= column-name [ <type-name> ] [ <column-constraint> ]*
        TopProd(
            p = "column-def",
            1,
            Or(
                Id("column name"),
                // per SQLite test suite file misc1.test, these are valid to use as column names
                Toks(
                    TokenType.Abort,
                    TokenType.Asc,
                    TokenType.Begin,
                    TokenType.Conflict,
                    TokenType.Desc,
                    TokenType.End,
                    TokenType.Explain,
                    TokenType.Fail,
                    TokenType.Ignore,
                    TokenType.Key,
                    TokenType.Offset,
                    TokenType.Pragma,
                    TokenType.Replace,
                    TokenType.Temp,
                    TokenType.Vacuum,
                    TokenType.View
                )
            ),
            Opt(SubProd("type-name")),
            Lst($"{p}.constraint", null, 0, SubProd("column-constraint"))
        );

        // type-name ::= name+ [ "(" <signed-number> ")" | "(" <signed-number> "," <signed-number> ")" ]
        // implemented as: name+ [ "(" <signed-number> [ "," <signed-number> ] ")" ]
        TopProd(
            p = "type-name",
            1,
            Lst(
                $"{p}.part",
                null,
                1,
                Or(
                    Id("data type"),
                    Toks(
                        // these tokens are okay to enter as part of the data type.  the list was created by testing
                        // SQLite; these are not enumerated in the grammar.
                        TokenType.Explain,
                        TokenType.Query,
                        TokenType.Plan,
                        TokenType.Begin,
                        TokenType.Deferred,
                        TokenType.Immediate,
                        TokenType.Exclusive,
                        TokenType.End,
                        TokenType.Rollback,
                        TokenType.Savepoint,
                        TokenType.Release,
                        TokenType.If,
                        TokenType.Temp,
                        TokenType.Without,
                        TokenType.Abort,
                        TokenType.Action,
                        TokenType.After,
                        TokenType.Analyze,
                        TokenType.Asc,
                        TokenType.Attach,
                        TokenType.Before,
                        TokenType.By,
                        TokenType.Cascade,
                        TokenType.Cast,
                        TokenType.Columnkw,
                        TokenType.Conflict,
                        TokenType.Database,
                        TokenType.Desc,
                        TokenType.Detach,
                        TokenType.Each,
                        TokenType.Fail,
                        TokenType.For,
                        TokenType.Ignore,
                        TokenType.Initially,
                        TokenType.Instead,
                        TokenType.LikeKw,
                        TokenType.Match,
                        TokenType.No,
                        TokenType.Key,
                        TokenType.Of,
                        TokenType.Offset,
                        TokenType.Pragma,
                        TokenType.Raise,
                        TokenType.Recursive,
                        TokenType.Replace,
                        TokenType.Restrict,
                        TokenType.Row,
                        TokenType.Trigger,
                        TokenType.Vacuum,
                        TokenType.View,
                        TokenType.Virtual,
                        TokenType.With,
                        TokenType.Reindex,
                        TokenType.Rename,
                        TokenType.CtimeKw,
                        TokenType.Any,
                        TokenType.Rem,
                        TokenType.Concat,
                        TokenType.Autoincr,
                        TokenType.Deferrable,
                        TokenType.Isnot,
                        TokenType.Function,
                        TokenType.AggFunction,
                        TokenType.Register
                    )
                )
            ),
            Opt(
                Tok(TokenType.Lp),
                SubProd("signed-number"),
                Opt(Tok(TokenType.Comma), SubProd("signed-number")),
                Tok(TokenType.Rp)
            )
        );

        // column-constraint ::=
        //      [ CONSTRAINT name ]
        //      ( PRIMARY KEY [ASC | DESC] <conflict-clause> [AUTOINCREMENT] )
        //      |   ( NOT NULL <conflict-clause> )
        //      |   ( NULL <conflict-clause> )
        //              ^^^ this line isn't in the official grammar, but SQLite seems to accept it.
        //      |   ( UNIQUE <conflict-clause> )
        //      |   ( CHECK "(" <expr> ")" )
        //      |   ( DEFAULT ( <signed-number> | <literal-value> | "(" <expr> ")" ) )
        //      |   ( COLLATE collation-name )
        //      |   ( <foreign-key-clause> )
        TopProd(
            p = "column-constraint",
            2,
            Opt(1, Tok(TokenType.Constraint), Id("constraint name")),
            Or(
                Prod(
                    $"{p}.primary-key",
                    1,
                    Tok(TokenType.Primary),
                    Tok(TokenType.Key),
                    Opt(Or(Tok(TokenType.Asc), Tok(TokenType.Desc))),
                    SubProd("conflict-clause"),
                    Opt(Tok(TokenType.Autoincr))
                ),
                Prod($"{p}.not-null", 1, Tok(TokenType.Not), Tok(TokenType.Null), SubProd("conflict-clause")),
                Prod($"{p}.null", 1, Tok(TokenType.Null), SubProd("conflict-clause")),
                Prod($"{p}.unique", 1, Tok(TokenType.Unique), SubProd("conflict-clause")),
                Prod($"{p}.check", 1, Tok(TokenType.Check), Tok(TokenType.Lp), SubProd("expr"), Tok(TokenType.Rp)),
                Prod(
                    $"{p}.default",
                    1,
                    Tok(TokenType.Default),
                    Or(
                        Prod($"{p}.number", 1, SubProd("signed-number")),
                        Prod($"{p}.literal", 1, SubProd("literal-value")),
                        Prod($"{p}.expr", 1, Tok(TokenType.Lp), SubProd("expr"), Tok(TokenType.Rp))
                    )
                ),
                Prod($"{p}.collate", 1, Tok(TokenType.Collate), Id("collation name")),
                Prod($"{p}.foreign-key", 1, SubProd("foreign-key-clause")),
                Prod(
                    $"{p}.generated",
                    2,
                    Opt(Tok(TokenType.Generated), Tok(TokenType.Always)),
                    Tok(TokenType.As),
                    Tok(TokenType.Lp),
                    SubProd("expr"),
                    Tok(TokenType.Rp),
                    Opt(Or(Tok("stored"), Tok(TokenType.Virtual)))
                )
            )
        );

        // signed-number ::= [ + | - ] numeric-literal
        TopProd(
            p = "signed-number",
            2,
            Opt(Or(Tok("+"), Tok("-"))),
            Or(Tok(TokenType.Integer), Tok(TokenType.Float), Tok(TokenType.Qnumber))
        );

        // table-constraint ::= [ CONSTRAINT name ]
        //      ( ( PRIMARY KEY | UNIQUE ) "(" <indexed-column> [ , <indexed-column> ]* ")"
        //      <conflict-clause> | CHECK "(" <expr> ")" | FOREIGN KEY "(" column-name [ , column-name ]* ")"
        //      <foreign-key-clause> )
        TopProd(
            p = "table-constraint",
            2,
            Opt(1, Tok(TokenType.Constraint), Id("constraint name")),
            Or(
                Prod(
                    $"{p}.key-or-unique",
                    1,
                    Or(
                        Prod($"{p}.primary-key", 1, Tok(TokenType.Primary), Tok(TokenType.Key)),
                        Prod($"{p}.unique", 1, Tok(TokenType.Unique))
                    ),
                    Tok(TokenType.Lp),
                    Lst($"{p}.column", TokenType.Comma, 1, SubProd("indexed-column")),
                    Tok(TokenType.Rp),
                    SubProd("conflict-clause")
                ),
                Prod($"{p}.check", 1, Tok(TokenType.Check), Tok(TokenType.Lp), SubProd("expr"), Tok(TokenType.Rp)),
                Prod(
                    $"{p}.foreign-key",
                    1,
                    Tok(TokenType.Foreign),
                    Tok(TokenType.Key),
                    Tok(TokenType.Lp),
                    Lst($"{p}.column", TokenType.Comma, 1, Id("column name")),
                    Tok(TokenType.Rp),
                    SubProd("foreign-key-clause")
                )
            )
        );

        // foreign-key-clause ::=
        //      REFERENCES foreign-table
        //      [ "(" column-name [ "," column-name ]* ")" ]
        //      [
        //          (
        //              ON ( DELETE | UPDATE )
        //              ( SET NULL | SET DEFAULT | CASCADE | RESTRICT | NO ACTION )
        //          ) | (
        //              MATCH name
        //          )
        //      ]*
        //      [ [NOT] DEFERRABLE [ INITIALLY DEFERRED | INITIALLY IMMEDIATE ] ]
        TopProd(
            p = "foreign-key-clause",
            1,
            Tok(TokenType.References),
            Id("foreign table name"),
            Opt(Tok(TokenType.Lp), Lst($"{p}.column", TokenType.Comma, 1, Id("column name")), Tok(TokenType.Rp)),
            Lst(
                $"{p}.on-clause",
                null,
                0,
                Or(
                    Prod(
                        $"{p}.on",
                        1,
                        Tok(TokenType.On),
                        Or(Tok(TokenType.Delete), Tok(TokenType.Update)),
                        Or(
                            Prod($"{p}.set", 1, Tok(TokenType.Set), Or(Tok(TokenType.Null), Tok(TokenType.Default))),
                            Prod($"{p}.cascade", 1, Tok(TokenType.Cascade)),
                            Prod($"{p}.restrict", 1, Tok(TokenType.Restrict)),
                            Prod($"{p}.no-action", 1, Tok(TokenType.No), Tok(TokenType.Action))
                        )
                    ),
                    Prod($"{p}.match", 1, Tok(TokenType.Match), Id("match type"))
                )
            ),
            Opt(
                Opt(Tok(TokenType.Not)),
                Tok(TokenType.Deferrable),
                Opt(1, Tok(TokenType.Initially), Or(Tok(TokenType.Deferred), Tok(TokenType.Immediate)))
            )
        );

        // conflict-clause ::= [ ON CONFLICT ( ROLLBACK | ABORT | FAIL | IGNORE | REPLACE ) ]
        TopProd(
            p = "conflict-clause",
            1,
            Opt(
                2,
                Tok(TokenType.On),
                Tok(TokenType.Conflict),
                Or(
                    Tok(TokenType.Rollback),
                    Tok(TokenType.Abort),
                    Tok(TokenType.Fail),
                    Tok(TokenType.Ignore),
                    Tok(TokenType.Replace)
                )
            )
        );

        // create-trigger-stmt ::= CREATE [ TEMP | TEMPORARY ] TRIGGER [ IF NOT EXISTS ]
        //      [database-name "."] trigger-name [BEFORE | AFTER | INSTEAD OF]
        //      ( DELETE | INSERT | UPDATE [OF column-name [ "," column-name ]* ] ) ON table-name
        //      [ FOR EACH ROW ] [ WHEN <expr> ]
        //      BEGIN ( ( <update-stmt> | <insert-stmt> | <delete-stmt> | <select-stmt> ) ";" )+ END
        TopProd(
            p = "create-trigger-stmt",
            3,
            Tok(TokenType.Create),
            Opt(Tok(TokenType.Temp), Tok("temporary")),
            Tok(TokenType.Trigger),
            Opt(1, Tok(TokenType.If), Tok(TokenType.Not), Tok(TokenType.Exists)),
            Opt(Id("database name"), Tok(TokenType.Dot)),
            Id("trigger name"),
            Opt(
                Or(
                    Prod($"{p}.before", 1, Tok(TokenType.Before)),
                    Prod($"{p}.after", 1, Tok(TokenType.After)),
                    Prod($"{p}.instead-of", 1, Tok(TokenType.Instead), Tok(TokenType.Of))
                )
            ),
            Or(
                Prod($"{p}.delete", 1, Tok(TokenType.Delete)),
                Prod($"{p}.insert", 1, Tok(TokenType.Insert)),
                Prod(
                    $"{p}.update",
                    1,
                    Tok(TokenType.Update),
                    Opt(1, Tok(TokenType.Of), Lst($"{p}.column", TokenType.Comma, 1, Id("column name")))
                )
            ),
            Tok(TokenType.On),
            Id("table name"),
            Opt(1, Tok(TokenType.For), Tok(TokenType.Each), Tok(TokenType.Row)),
            Opt(1, Tok(TokenType.When), SubProd("expr")),
            Tok(TokenType.Begin),
            Lst(
                $"{p}.stmt",
                null,
                1,
                Or(SubProd("update-stmt"), SubProd("insert-stmt"), SubProd("delete-stmt"), SubProd("select-stmt")),
                Tok(TokenType.Semi)
            ),
            Tok(TokenType.End)
        );

        // create-view-stmt ::= CREATE [ TEMP | TEMPORARY ] VIEW [ IF NOT EXISTS ]
        //      [database-name "."] view-name [ "(" column-name [ "," column-name ]* ")" ] AS <select-stmt>
        TopProd(
            p = "create-view-stmt",
            3,
            Tok(TokenType.Create),
            Opt(Tok(TokenType.Temp), Tok("temporary")),
            Tok(TokenType.View),
            Opt(1, Tok(TokenType.If), Tok(TokenType.Not), Tok(TokenType.Exists)),
            Opt(Id("database name"), Tok(TokenType.Dot)),
            Id("view name"),
            Opt(1, Tok(TokenType.Lp), Lst($"{p}.column", TokenType.Comma, 1, Id("column name")), Tok(TokenType.Rp)),
            Tok(TokenType.As),
            SubProd("select-stmt")
        );

        // create-virtual-table-stmt ::= CREATE VIRTUAL TABLE [ IF NOT EXISTS ]
        //      [ database-name "." ] table-name
        //      USING module-name [ "(" module-argument [ "," module-argument ]* ")" ]
        TopProd(
            p = "create-virtual-table-stmt",
            2,
            Tok(TokenType.Create),
            Tok(TokenType.Virtual),
            Tok(TokenType.Table),
            Opt(1, Tok(TokenType.If), Tok(TokenType.Not), Tok(TokenType.Exists)),
            Opt(Id("database name"), Tok(TokenType.Dot)),
            Id("table name"),
            Tok(TokenType.Using),
            Id("module name"),
            Opt(1, Tok(TokenType.Lp), Lst($"{p}.arg", TokenType.Comma, 1, SubProd("expr")), Tok(TokenType.Rp))
        );

        // with-clause ::= WITH [ RECURSIVE ] <cte-table-name> AS
        //      [[NOT] MATERIALIZED]
        //      "(" <select-stmt> ")"
        //      [ "," <cte-table-name> AS "(" <select-stmt> ")" ]*
        TopProd(
            p = "with-clause",
            1,
            Tok(TokenType.With),
            Opt(Tok(TokenType.Recursive)),
            Lst(
                $"{p}.cte",
                TokenType.Comma,
                1,
                SubProd("cte-table-name"),
                Tok(TokenType.As),
                Opt(Opt(Tok(TokenType.Not)), Tok(TokenType.Materialized)),
                Tok(TokenType.Lp),
                SubProd("select-stmt"),
                Tok(TokenType.Rp)
            )
        );

        // cte-table-name ::= table-name [ "(" column-name [ "," column-name ]* ")" ]
        TopProd(
            p = "cte-table-name",
            1,
            Id("table name"),
            Opt(1, Tok(TokenType.Lp), Lst($"{p}.column", TokenType.Comma, 1, Id("column name")), Tok(TokenType.Rp))
        );

        // common-table-expression ::= table-name [ "(" column-name [ "," column-name ]* ")" ]
        //      AS "(" <select-stmt> ")"
        TopProd(
            p = "common-table-expression",
            1,
            SubProd("cte-table-name"),
            Tok(TokenType.As),
            Opt(Opt(Tok(TokenType.Not)), Tok(TokenType.Materialized)),
            Tok(TokenType.Lp),
            SubProd("select-stmt"),
            Tok(TokenType.Rp)
        );

        // delete-stmt ::= [ <with-clause> ] DELETE FROM <qualified-table-name> [ WHERE <expr> ]
        //      [<returning-clause>]
        //      [
        //          [ ORDER BY <ordering-term> [ "," <ordering-term> ]* ]
        //          LIMIT <expr> [ ( OFFSET | "," ) <expr> ]
        //      ]
        TopProd(
            p = "delete-stmt",
            2,
            Opt(SubProd("with-clause")),
            Tok(TokenType.Delete),
            Tok(TokenType.From),
            SubProd("qualified-table-name"),
            Opt(1, Tok(TokenType.Where), SubProd("expr")),
            Opt(SubProd("returning-clause")),
            Opt(
                2,
                Opt(
                    1,
                    Tok(TokenType.Order),
                    Tok(TokenType.By),
                    Lst($"{p}.order", TokenType.Comma, 1, SubProd("ordering-term"))
                ),
                Tok(TokenType.Limit),
                SubProd("expr"),
                Opt(1, Or(Tok(TokenType.Offset), Tok(TokenType.Comma)), SubProd("expr"))
            )
        );

        // detach-stmt ::= DETACH [ DATABASE ] database-name
        TopProd(p = "detach-stmt", 1, Tok(TokenType.Detach), Opt(Tok(TokenType.Database)), Id("database name"));

        // drop-index-stmt ::= DROP INDEX [ IF EXISTS ] [ database-name "." ] index-name
        TopProd(
            p = "drop-index-stmt",
            2,
            Tok(TokenType.Drop),
            Tok(TokenType.Index),
            Opt(1, Tok(TokenType.If), Tok(TokenType.Exists)),
            Opt(Id("database name"), Tok(TokenType.Dot)),
            Id("index name")
        );

        // drop-table-stmt ::= DROP TABLE [ IF EXISTS ] [ database-name "." ] table-name
        TopProd(
            p = "drop-table-stmt",
            2,
            Tok(TokenType.Drop),
            Tok(TokenType.Table),
            Opt(1, Tok(TokenType.If), Tok(TokenType.Exists)),
            Opt(Id("database name"), Tok(TokenType.Dot)),
            Id("table name")
        );

        // drop-trigger-stmt ::= DROP TRIGGER [ IF EXISTS ] [ database-name "." ] trigger-name
        TopProd(
            p = "drop-trigger-stmt",
            2,
            Tok(TokenType.Drop),
            Tok(TokenType.Trigger),
            Opt(1, Tok(TokenType.If), Tok(TokenType.Exists)),
            Opt(Id("database name"), Tok(TokenType.Dot)),
            Id("trigger name")
        );

        // drop-view-stmt ::= DROP VIEW [ IF EXISTS ] [ database-name "." ] view-name
        TopProd(
            p = "drop-view-stmt",
            2,
            Tok(TokenType.Drop),
            Tok(TokenType.View),
            Opt(1, Tok(TokenType.If), Tok(TokenType.Exists)),
            Opt(Id("database name"), Tok(TokenType.Dot)),
            Id("view name")
        );

        // The SQLite grammar for expressions does not express operator precedence.  Our expression grammar is
        // modified from the official SQLite grammar to take operator precedence into account.
        //
        // From https://www.sqlite.org/lang_expr.html:
        //      SQLite understands the following binary operators, in order from highest to lowest precedence:
        //          ||   ->   ->>
        //          *    /    %
        //          +    -
        //          <<   >>   &    |
        //          <    <=   >    >=
        //          =    ==   !=   <>    IS   IS NOT   IN   LIKE   GLOB  MATCH   REGEXP
        //          AND
        //          OR
        //      ...
        //      The COLLATE operator is a unary postfix operator that assigns a collating sequence to an
        //      expression. The COLLATE operator has a higher precedence (binds more tightly) than any binary
        //      operator and any unary prefix operator except "~". (COLLATE and "~" are associative so their
        //      binding order does not matter.) ...

        // expr ::= or-expr
        TopProd(p = "expr", 1, SubProd("or-expr"));

        // or-expr ::= and-expr [ OR and-expr ]*
        TopProd(p = "or-expr", 1, Lst($"{p}.term", TokenType.Or, 1, SubProd("and-expr")));

        // and-expr ::= eq-expr [ AND eq-expr ]*
        TopProd(p = "and-expr", 1, Lst($"{p}.term", TokenType.And, 1, SubProd("eq-expr")));

        // eq-expr ::= ineq-expr [ eq-expr-op | eq-expr-is | eq-expr-in | eq-expr-like | eq-expr-between ]*
        TopProd(
            p = "eq-expr",
            1,
            SubProd("ineq-expr"),
            Lst(
                $"{p}.term",
                null,
                0,
                Or(
                    SubProd("eq-expr-op"),
                    SubProd("eq-expr-is"),
                    SubProd("eq-expr-in"),
                    SubProd("eq-expr-like"),
                    SubProd("eq-expr-between")
                )
            )
        );

        // eq-expr-op ::= ( "=" | "==" | "!=" | "<>" ) ineq-expr
        TopProd(p = "eq-expr-op", 1, Or(Tok("="), Tok("=="), Tok("!="), Tok("<>")), SubProd("ineq-expr"));

        // eq-expr-is ::= (IS [NOT] (DISTINCT FROM)? ineq-expr) | ISNULL | NOTNULL | (NOT NULL)
        TopProd(
            p = "eq-expr-is",
            1,
            Or(
                Prod(
                    $"{p}.is-not",
                    1,
                    Tok(TokenType.Is),
                    Opt(Tok(TokenType.Not)),
                    Opt(Tok(TokenType.Distinct), Tok(TokenType.From)),
                    SubProd("ineq-expr")
                ),
                Prod($"{p}.is-null", 1, Tok(TokenType.Isnull)),
                Prod($"{p}.notnull", 1, Tok(TokenType.Notnull)),
                Prod($"{p}.not-null", 1, Tok(TokenType.Not), Tok(TokenType.Null))
            )
        );

        // eq-expr-in ::=
        //     [NOT] IN
        //     (
        //         "(" [ <select-stmt> | <expr> [ "," <expr> ]* ] ")" |
        //         [schema-name "."] table-or-function-name ["(" [<expr> ["," <expr>]*] ")"]
        //     )
        TopProd(
            p = "eq-expr-in",
            2,
            Opt(Tok(TokenType.Not)),
            Tok(TokenType.In),
            Or(
                Prod(
                    $"{p}.select",
                    1,
                    Tok(TokenType.Lp),
                    Opt(Or(SubProd("select-stmt"), Lst($"{p}.value", TokenType.Comma, 1, SubProd("expr")))),
                    Tok(TokenType.Rp)
                ),
                Prod(
                    $"{p}.table",
                    2,
                    Opt(Id("schema-name"), Tok(TokenType.Dot)),
                    Id("table or function name"),
                    Opt(
                        Tok(TokenType.Lp),
                        Lst($"{p}.table-function-arg", TokenType.Comma, 0, SubProd("expr")),
                        Tok(TokenType.Rp)
                    )
                )
            )
        );

        // eq-expr-like ::= [NOT] (LIKE | GLOB | REGEXP | MATCH) <ineq-expr> [ESCAPE <ineq-expr>]
        TopProd(
            p = "eq-expr-like",
            2,
            Opt(Tok(TokenType.Not)),
            Or(Tok("like"), Tok("glob"), Tok("regexp"), Tok("match")),
            SubProd("ineq-expr"),
            Opt(1, Tok(TokenType.Escape), SubProd("ineq-expr"))
        );

        // eq-expr-between ::= [NOT] BETWEEN <ineq-expr> AND <ineq-expr>
        TopProd(
            p = "eq-expr-between",
            2,
            Opt(Tok(TokenType.Not)),
            Tok(TokenType.Between),
            SubProd("ineq-expr"),
            Tok(TokenType.And),
            SubProd("ineq-expr")
        );

        // ineq-expr ::= <bitwise-expr> [ ( "<" | "<=" | ">" | ">=" ) <bitwise-expr> ]*
        TopProd(
            p = "ineq-expr",
            1,
            LstP(".term", Or(Tok("<"), Tok("<="), Tok(">"), Tok(">=")), 1, SubProd("bitwise-expr"))
        );

        // bitwise-expr ::= <add-expr> [ ( "<<" | ">>" | "&" | "|" ) <add-expr> ]*
        TopProd(
            p = "bitwise-expr",
            1,
            LstP(".term", Or(Tok("<<"), Tok(">>"), Tok("&"), Tok("|")), 1, SubProd("add-expr"))
        );

        // add-expr ::= <mult-expr> [ ( "+" | "-" ) <mult-expr> ]*
        TopProd(p = "add-expr", 1, LstP(".term", Or(Tok("+"), Tok("-")), 1, SubProd("mult-expr")));

        // mult-expr ::= <concat-expr> [ ( "*" | "/" | "%" ) <concat-expr> ]*
        TopProd(p = "mult-expr", 1, LstP(".term", Or(Tok("*"), Tok("/"), Tok("%")), 1, SubProd("concat-expr")));

        // concat-expr ::= <unary-expr> [ ("||" | "->" | "->>") <unary-expr> ]*
        TopProd(p = "concat-expr", 1, LstP(".term", Or(Tok("||"), Tok("->"), Tok("->>")), 1, SubProd("unary-expr")));

        // unary-expr ::= [ "-" | "+" | "NOT" | "~" ]* <collate-expr>
        TopProd(p = "unary-operator", 1, Or(Tok("-"), Tok("+"), Tok(TokenType.Not), Tok("~")));

        TopProd(p = "unary-expr", 2, Lst(".operator", null, 0, SubProd("unary-operator")), SubProd("collate-expr"));

        // collate-expr ::= <expr-term> [COLLATE collation-name]
        TopProd(p = "collate-expr", 2, SubProd("expr-term"), Opt(1, Tok(TokenType.Collate), Id("collation name")));

        // expr-term ::=
        //      <literal-value> |
        //      <bind-parameter> |
        //      [ [ database-name "." ] table-name "." ] column-name |
        //      window-function-invocation |
        //      function-name "(" [ [DISTINCT] <expr> [ "," <expr> ]* [ ORDER BY <ordering-term> [ "," <ordering-term> ] ] | "*" ] ")" |
        //      "(" <expr> ["," <expr>]* ")" |
        //      CAST "(" <expr> AS <type-name> ")" |
        //      [ [NOT] EXISTS ] ( <select-stmt> ) |
        //      CASE [ <expr> ] ( WHEN <expr> THEN <expr> )+ [ ELSE <expr> ] END |
        //      <raise-function>
        TopProd(
            p = "expr-term",
            1,
            Or(
                // window-function-invocation -- must be above simple/aggregate function invocation
                Prod($"{p}.window-function-invocation", 1, SubProd("window-function-invocation")),
                // expr ::= function-name "(" [ [DISTINCT] <expr> [ "," <expr> ]* [ ORDER BY <ordering-term> [ "," <ordering-term> ] ] | "*" ] ")"
                // (this is aggregate-function-invocation in the SQLite docs now)
                Prod(
                    $"{p}.function-call",
                    2,
                    Or(Id("function name"), Tok(TokenType.Replace)),
                    Tok(TokenType.Lp),
                    Opt(
                        Or(
                            Prod($"{p}.star", 1, Tok(TokenType.Star)),
                            Prod(
                                $"{p}.args",
                                2,
                                Opt(Tok(TokenType.Distinct)),
                                Lst($"{p}.arg", TokenType.Comma, 1, SubProd("expr")),
                                Opt(
                                    Tok(TokenType.Order),
                                    Tok(TokenType.By),
                                    Lst($"{p}.ordering-term", TokenType.Comma, 1, SubProd("ordering-term"))
                                )
                            )
                        )
                    ),
                    Tok(TokenType.Rp),
                    Opt(SubProd("filter-clause"))
                ),
                // [ [ database-name "." ] table-name "." ] column-name
                Prod(
                    $"{p}.column-name",
                    1,
                    Or(
                        Prod(
                            ".dotted-identifier",
                            2,
                            Or(Id("database, table, or column name"), LitStr("database, table, or column name")),
                            Tok(TokenType.Dot),
                            Or(Id("table or column name"), LitStr("table or column name")),
                            Opt(Tok(TokenType.Dot), Or(Id("column name"), LitStr("column name")))
                        ),
                        Prod(".bare-col-identifier", 1, Id("column name"))
                    )
                ),
                // <bind-parameter>
                Prod($"{p}.variable-name", 1, Id("variable name", allowVar: true)),
                // <literal-value>
                Prod($"{p}.literal-value", 1, SubProd("literal-value")),
                // expr ::= "(" <expr> ["," <expr>]* ")"
                Prod(
                    $"{p}.parentheses",
                    1,
                    Tok(TokenType.Lp),
                    Lst($"{p}.row-value-item", TokenType.Comma, 1, SubProd("expr")),
                    Tok(TokenType.Rp)
                ),
                // expr ::= CAST "(" <expr> AS <type-name> ")"
                Prod(
                    $"{p}.cast",
                    1,
                    Tok(TokenType.Cast),
                    Tok(TokenType.Lp),
                    SubProd("expr"),
                    Tok(TokenType.As),
                    SubProd("type-name"),
                    Tok(TokenType.Rp)
                ),
                // expr ::= [ [NOT] EXISTS ] ( <select-stmt> )
                Prod(
                    $"{p}.exists",
                    2,
                    Opt(Opt(Tok(TokenType.Not)), Tok(TokenType.Exists)),
                    Tok(TokenType.Lp),
                    SubProd("select-stmt"),
                    Tok(TokenType.Rp)
                ),
                // expr ::= CASE [ <expr> ] (WHEN <expr> THEN <expr>)+ [ ELSE <expr> ] END
                Prod(
                    $"{p}.case",
                    1,
                    Tok(TokenType.Case),
                    Opt(SubProd("expr")),
                    Lst(".when", null, 1, Tok(TokenType.When), SubProd("expr"), Tok(TokenType.Then), SubProd("expr")),
                    Opt(1, Tok(TokenType.Else), SubProd("expr")),
                    Tok(TokenType.End)
                ),
                // expr ::= <raise-function>
                Prod($"{p}.raise", 1, SubProd("raise-function"))
            )
        );

        // raise-function ::= RAISE ( IGNORE | (( ROLLBACK | ABORT | FAIL ) "," error-message) )
        TopProd(
            p = "raise-function",
            1,
            Tok(TokenType.Raise),
            Tok(TokenType.Lp),
            Or(
                Prod($"{p}.ignore", 1, Tok(TokenType.Ignore)),
                Prod(
                    $"{p}.rollback-abort-fail",
                    1,
                    Or(Tok(TokenType.Rollback), Tok(TokenType.Abort), Tok(TokenType.Fail)),
                    Tok(TokenType.Comma),
                    LitStr("error message")
                )
            ),
            Tok(TokenType.Rp)
        );

        // literal-value ::= numeric-literal
        // literal-value ::= string-literal
        // literal-value ::= blob-literal
        // literal-value ::= NULL
        // literal-value ::= CURRENT_TIME
        // literal-value ::= CURRENT_DATE
        // literal-value ::= CURRENT_TIMESTAMP
        TopProd(
            p = "literal-value",
            1,
            Or(
                Tok(TokenType.Integer),
                Tok(TokenType.Float),
                Tok(TokenType.Qnumber),
                Tok(TokenType.String),
                Tok(TokenType.Blob),
                Tok(TokenType.Null),
                Tok("current_time"),
                Tok("current_date"),
                Tok("current_timestamp")
            )
        );

        // insert-stmt ::= [ <with-clause> ]
        //      ( INSERT | REPLACE | INSERT OR REPLACE | INSERT OR ROLLBACK |
        //          INSERT OR ABORT | INSERT OR FAIL | INSERT OR IGNORE ) INTO
        //      [ database-name "." ] table-name [ "(" column-name [ "," column-name ]* ")" ]
        //      (
        //          VALUES "(" <expr> [ "," <expr> ]* ")" [ "," "(" <expr> [ "," <expr> ]* ")" ]* [<upsert-clause>] |
        //          <select-stmt> [<upsert-clause>] |
        //          DEFAULT VALUES
        //      )
        //      [<returning-clause>]
        TopProd(
            p = "insert-stmt",
            1,
            Opt(SubProd("with-clause")),
            Or(
                Prod(
                    $"{p}.insert",
                    1,
                    Tok(TokenType.Insert),
                    Opt(
                        1,
                        Tok(TokenType.Or),
                        Or(
                            Tok(TokenType.Replace),
                            Tok(TokenType.Rollback),
                            Tok(TokenType.Abort),
                            Tok(TokenType.Fail),
                            Tok(TokenType.Ignore)
                        )
                    )
                ),
                Prod($"{p}.replace", 1, Tok(TokenType.Replace))
            ),
            Tok(TokenType.Into),
            Opt(Id("database name"), Tok(TokenType.Dot)),
            Id("table name"),
            Opt(Tok(TokenType.Lp), Lst($"{p}.column", TokenType.Comma, 1, Id("column name")), Tok(TokenType.Rp)),
            Or(
                Prod(
                    $"{p}.values",
                    1,
                    Tok(TokenType.Values),
                    Lst(
                        $"{p}.row",
                        TokenType.Comma,
                        1,
                        Tok(TokenType.Lp),
                        Lst($"{p}.value", TokenType.Comma, 1, SubProd("expr")),
                        Tok(TokenType.Rp)
                    ),
                    Opt(SubProd("upsert-clause"))
                ),
                Prod($"{p}.select", 1, SubProd("select-stmt"), Opt(SubProd("upsert-clause"))),
                Prod($"{p}.default-values", 1, Tok(TokenType.Default), Tok(TokenType.Values))
            ),
            Opt(SubProd("returning-clause"))
        );

        // pragma-stmt ::= PRAGMA [ database-name "." ] pragma-name
        //      [ "=" <pragma-value> | "(" <pragma-value> ")" ]
        TopProd(
            p = "pragma-stmt",
            1,
            Tok(TokenType.Pragma),
            Opt(Id("database name"), Tok(TokenType.Dot)),
            Id("pragma name"),
            Opt(
                Or(
                    Prod($"{p}.equals", 1, Tok(TokenType.Eq), SubProd("pragma-value")),
                    Prod($"{p}.paren", 1, Tok(TokenType.Lp), SubProd("pragma-value"), Tok(TokenType.Rp))
                )
            )
        );

        // pragma-value ::= <signed-number>
        // pragma-value ::= name
        // pragma-value ::= string-literal
        TopProd(p = "pragma-value", 1, Or(SubProd("signed-number"), Id("name"), LitStr("string")));

        // reindex-stmt ::= REINDEX [ [ database-name "." ] table-or-index-or-collation-name ]
        TopProd(
            p = "reindex-stmt",
            1,
            Tok(TokenType.Reindex),
            Opt(Opt(Id("database name"), Tok(TokenType.Dot)), Id("table, index, or collation name"))
        );

        // select-stmt ::= [ WITH [ RECURSIVE ] <common-table-expression> [ , <common-table-expression> ]* ]
        // [ SELECT [ DISTINCT | ALL ] <result-column> [ , <result-column> ]*
        // [ FROM [ <table-or-subquery> [ , <table-or-subquery> ]* | <join-clause> ]1 ]
        // [ WHERE <expr> ]
        // [ GROUP BY <expr> [ , <expr> ]* [ HAVING <expr> ] ] | VALUES ( <expr> [ , <expr> ]* ) [ , ( <expr> [ , <expr> ]* ) ]* ]1 [ <compound-operator> [ SELECT [ DISTINCT | ALL ] <result-column> [ , <result-column> ]*
        // [ WINDOW window-name AS <window-defn> [ , window-name AS <window-defn> ]* ]
        // [ ORDER BY <ordering-term> [ , <ordering-term> ]* ]
        // [ LIMIT <expr> [ [ OFFSET | , ]1 <expr> ] ]
        TopProd(
            p = "select-stmt",
            1,
            Opt(
                1,
                Tok(TokenType.With),
                Opt(Tok(TokenType.Recursive)),
                Lst($"{p}.cte", TokenType.Comma, 1, SubProd("common-table-expression"))
            ),
            LstP(
                ".compound-operand",
                SubProd("compound-operator"),
                1,
                Or(
                    Prod(
                        $"{p}.select",
                        1,
                        Tok(TokenType.Select),
                        Opt(Or(Tok(TokenType.Distinct), Tok(TokenType.All))),
                        Lst($"{p}.column", TokenType.Comma, 1, SubProd("result-column")),
                        Opt(
                            1,
                            Tok(TokenType.From),
                            Or(
                                SubProd("join-clause"),
                                Lst($"{p}.table", TokenType.Comma, 1, SubProd("table-or-subquery"))
                            )
                        ),
                        Opt(1, Tok(TokenType.Where), SubProd("expr")),
                        Opt(
                            1,
                            Tok(TokenType.Group),
                            Tok(TokenType.By),
                            Lst($"{p}.group-expr", TokenType.Comma, 1, SubProd("expr"))
                        ),
                        Opt(1, Tok(TokenType.Having), SubProd("expr")),
                        Opt(
                            1,
                            Tok(TokenType.Window),
                            Lst(
                                $"{p}.window",
                                TokenType.Comma,
                                1,
                                Id("window name"),
                                Tok(TokenType.As),
                                SubProd("window-defn")
                            )
                        )
                    ),
                    Prod(
                        $"{p}.values",
                        1,
                        Tok(TokenType.Values),
                        Lst(
                            $"{p}.row",
                            TokenType.Comma,
                            1,
                            Tok(TokenType.Lp),
                            Lst($"{p}.value", TokenType.Comma, 1, SubProd("expr")),
                            Tok(TokenType.Rp)
                        )
                    )
                )
            ),
            Opt(
                1,
                Tok(TokenType.Order),
                Tok(TokenType.By),
                Lst($"{p}.term", TokenType.Comma, 1, SubProd("ordering-term"))
            ),
            Opt(
                1,
                Tok(TokenType.Limit),
                SubProd("expr"),
                Opt(1, Or(Tok(TokenType.Offset), Tok(TokenType.Comma)), SubProd("expr"))
            )
        );

        // join-clause ::= <table-or-subquery> ( <join-operator> <table-or-subquery> <join-constraint> )+
        // note: the official grammar allows only a single join-operator. that seems like a mistake.
        // we've also changed the optional join-operator into an at-least-one, because the select-stmt production
        // already contains a case for a single table-or-subquery, so join-clause is only needed if a join-operator
        // is present.
        TopProd(
            p = "join-clause",
            1,
            SubProd("table-or-subquery"),
            Lst(
                $"{p}.term",
                null,
                1,
                SubProd("join-operator"),
                SubProd("table-or-subquery"),
                SubProd("join-constraint")
            )
        );

        // table-or-subquery ::= [ database-name "." ] table-function-name "(" [ <expr> ["," <expr>]* ")"
        //                                  [ [AS] table-alias ]
        // table-or-subquery ::= [ database-name "." ] table-name [ [ AS ] table-alias ]
        //      [ INDEXED BY index-name | NOT INDEXED ]
        // table-or-subquery ::= "(" ( <table-or-subquery> [ "," <table-or-subquery> ]* | <join-clause> ) ")"
        // table-or-subquery ::= "(" <select-stmt> ")" [ [ AS ] table-alias ]
        // note: the table-function-name production is described in the syntax diagram but not the text BNF.
        TopProd(
            p = "table-or-subquery",
            1,
            Or(
                Prod(
                    $"{p}.table-function-call",
                    3,
                    SubProd(
                        Prod(
                            $"{p}.table-function-name",
                            2,
                            Opt(Id("database name"), Tok(TokenType.Dot)),
                            Id("table function name")
                        )
                    ),
                    Tok(TokenType.Lp),
                    Lst($"{p}.arg", TokenType.Comma, 0, SubProd("expr")),
                    Tok(TokenType.Rp),
                    Opt(Opt(Tok(TokenType.As)), Or(Id("table alias"), LitStr("table alias")))
                ),
                Prod(
                    $"{p}.table",
                    2,
                    Or(Id("database or table name"), LitStr("database or table name")),
                    Opt(Tok(TokenType.Dot), Or(Id("table name"), LitStr("table name"))),
                    Opt(Opt(Tok(TokenType.As)), Or(Id("table alias"), LitStr("table alias"))),
                    Opt(
                        Or(
                            Prod($"{p}.indexed-by", 1, Tok(TokenType.Indexed), Tok(TokenType.By), Id("index name")),
                            Prod($"{p}.not-indexed", 1, Tok(TokenType.Not), Tok(TokenType.Indexed))
                        )
                    )
                ),
                Prod(
                    $"{p}.select",
                    2,
                    Tok(TokenType.Lp),
                    SubProd("select-stmt"),
                    Tok(TokenType.Rp),
                    Opt(Opt(Tok(TokenType.As)), Or(Id("table alias"), LitStr("table alias")))
                ),
                Prod(
                    $"{p}.joins",
                    1,
                    Tok(TokenType.Lp),
                    Or(Lst($"{p}.term", TokenType.Comma, 1, SubProd("table-or-subquery")), SubProd("join-clause")),
                    Tok(TokenType.Rp)
                )
            )
        );

        // result-column ::= *
        // result-column ::= table-name . *
        // result-column ::= <expr> [ [ AS ] column-alias ]
        TopProd(
            p = "result-column",
            1,
            Or(
                Prod($"{p}.star", 1, Tok(TokenType.Star)),
                Prod(
                    $"{p}.table-star",
                    3,
                    Or(Id("table name"), LitStr("table name")),
                    Tok(TokenType.Dot),
                    Tok(TokenType.Star)
                ),
                Prod(
                    $"{p}.expr",
                    1,
                    SubProd("expr"),
                    Opt(Opt(Tok(TokenType.As)), Or(Id("column alias"), LitStr("column alias")))
                )
            )
        );

        // join-operator ::= ,
        // join-operator ::= [ NATURAL ] [ (LEFT | RIGHT | FULL) [ OUTER ] | INNER | CROSS ] JOIN
        TopProd(
            p = "join-operator",
            1,
            Or(
                Prod($"{p}.comma", 1, Tok(TokenType.Comma)),
                Prod(
                    $"{p}.join",
                    3,
                    Opt(Tok("natural")),
                    Opt(
                        Or(
                            Prod($"{p}.left", 1, Or(Tok("left"), Tok("right"), Tok("full")), Opt(Tok("outer"))),
                            Prod($"{p}.inner", 1, Tok("inner")),
                            Prod($"{p}.cross", 1, Tok("cross"))
                        )
                    ),
                    Tok("join")
                )
            )
        );

        // join-constraint ::= [ ON <expr> | USING ( column-name [ , column-name ]* ) ]
        TopProd(
            p = "join-constraint",
            1,
            Opt(
                Or(
                    Prod($"{p}.on", 1, Tok(TokenType.On), SubProd("expr")),
                    Prod(
                        $"{p}.using",
                        1,
                        Tok(TokenType.Using),
                        Tok(TokenType.Lp),
                        Lst($"{p}.column", TokenType.Comma, 1, Id("column name")),
                        Tok(TokenType.Rp)
                    )
                )
            )
        );

        // ordering-term ::= <expr> [ COLLATE collation-name ] [ ASC | DESC ]
        TopProd(
            p = "ordering-term",
            1,
            SubProd("expr"),
            Opt(1, Tok(TokenType.Collate), Id("collation name")),
            Opt(Or(Tok(TokenType.Asc), Tok(TokenType.Desc))),
            Opt(Tok(TokenType.Nulls), Or(Tok(TokenType.First), Tok(TokenType.Last)))
        );

        // compound-operator ::= UNION
        // compound-operator ::= UNION ALL
        // compound-operator ::= INTERSECT
        // compound-operator ::= EXCEPT
        TopProd(
            p = "compound-operator",
            1,
            Or(
                Prod($"{p}.union", 1, Tok(TokenType.Union), Opt(Tok(TokenType.All))),
                Prod($"{p}.intersect", 1, Tok(TokenType.Intersect)),
                Prod($"{p}.except", 1, Tok(TokenType.Except))
            )
        );

        // column-name-list ::= "(" column-name ["," column-name ]* ")"
        TopProd(
            p = "column-name-list",
            1,
            Tok(TokenType.Lp),
            Lst($"{p}.column-name", TokenType.Comma, 1, Id("column name")),
            Tok(TokenType.Rp)
        );

        // update-stmt ::= [ <with-clause> ] UPDATE
        //      [ OR ROLLBACK | OR ABORT | OR REPLACE | OR FAIL | OR IGNORE ] <qualified-table-name>
        //      SET column-name = <expr> [ , (column-name | <column-name-list>) = <expr> ]* [ WHERE <expr> ]
        //      [<returning-clause>]
        //      [
        //          [ ORDER BY <ordering-term> [ , <ordering-term> ]* ]
        //          LIMIT <expr> [ ( OFFSET | , ) <expr> ]
        //      ]
        TopProd(
            p = "update-stmt",
            2,
            Opt(SubProd("with-clause")),
            Tok(TokenType.Update),
            Opt(
                1,
                Tok(TokenType.Or),
                Opt(
                    Tok(TokenType.Rollback),
                    Tok(TokenType.Abort),
                    Tok(TokenType.Replace),
                    Tok(TokenType.Fail),
                    Tok(TokenType.Ignore)
                )
            ),
            SubProd("qualified-table-name"),
            Tok(TokenType.Set),
            Lst(
                $"{p}.assignment",
                TokenType.Comma,
                1,
                Or(Id("column name"), SubProd("column-name-list")),
                Tok(TokenType.Eq),
                SubProd("expr")
            ),
            Opt(
                1,
                Tok(TokenType.From),
                Or(
                    Lst($"{p}.from-table-or-subquery", TokenType.Comma, 1, SubProd("table-or-subquery")),
                    SubProd("join-clause")
                )
            ),
            Opt(1, Tok(TokenType.Where), SubProd("expr")),
            Opt(SubProd("returning-clause")),
            Opt(
                2,
                Opt(
                    Tok(TokenType.Order),
                    Tok(TokenType.By),
                    Lst($"{p}.order-term", TokenType.Comma, 1, SubProd("ordering-term"))
                ),
                Tok(TokenType.Limit),
                SubProd("expr"),
                Opt(1, Or(Tok(TokenType.Offset), Tok(TokenType.Comma)), SubProd("expr"))
            )
        );

        // qualified-table-name ::= [ database-name . ] table-name [ INDEXED BY index-name | NOT INDEXED ]
        TopProd(
            p = "qualified-table-name",
            2,
            Opt(Id("database name"), Tok(TokenType.Dot)),
            Id("table name"),
            Opt(
                Or(
                    Prod($"{p}.indexed-by", 1, Tok(TokenType.Indexed), Tok(TokenType.By), Id("index name")),
                    Prod($"{p}.not-indexed", 1, Tok(TokenType.Not), Tok(TokenType.Indexed))
                )
            )
        );

        // vacuum-stmt ::= VACUUM
        TopProd(p = "vacuum-stmt", 1, Tok(TokenType.Vacuum));

        // filter-clause https://sqlite.org/syntax/filter-clause.html
        TopProd(
            p = "filter-clause",
            1,
            Tok(TokenType.Filter),
            Tok(TokenType.Lp),
            Tok(TokenType.Where),
            SubProd("expr"),
            Tok(TokenType.Rp)
        );

        // frame-spec https://sqlite.org/syntax/frame-spec.html
        TopProd(
            p = "frame-spec",
            1,
            Or(Tok(TokenType.Range), Tok(TokenType.Rows), Tok(TokenType.Groups)),
            Or(
                Prod(
                    $"{p}.between",
                    1,
                    Tok(TokenType.Between),
                    Or(
                        Prod(
                            $"{p}.between-from-unbounded-preceding",
                            1,
                            Tok(TokenType.Unbounded),
                            Tok(TokenType.Preceding)
                        ),
                        Prod(
                            $"{p}.between-from-expr",
                            1,
                            SubProd("expr"),
                            Or(Tok(TokenType.Preceding), Tok(TokenType.Following))
                        ),
                        Prod($"{p}.between-from-current-row", 1, Tok(TokenType.Current), Tok(TokenType.Row))
                    ),
                    Tok(TokenType.And),
                    Or(
                        Prod(
                            $"{p}.between-to-unbounded-following",
                            1,
                            Tok(TokenType.Unbounded),
                            Tok(TokenType.Following)
                        ),
                        Prod(
                            $"{p}.between-to-expr",
                            1,
                            SubProd("expr"),
                            Or(Tok(TokenType.Preceding), Tok(TokenType.Following))
                        ),
                        Prod($"{p}.between-to-current-row", 1, Tok(TokenType.Current), Tok(TokenType.Row))
                    )
                ),
                Prod($"{p}.unbounded-preceding", 1, Tok(TokenType.Unbounded), Tok(TokenType.Preceding)),
                Prod($"{p}.expr-preceding", 1, SubProd("expr"), Tok(TokenType.Preceding)),
                Prod($"{p}.current-row", 1, Tok(TokenType.Current), Tok(TokenType.Row))
            ),
            Opt(
                Tok(TokenType.Exclude),
                Or(
                    Prod($"{p}.exclude-no-others", 1, Tok(TokenType.No), Tok(TokenType.Others)),
                    Prod($"{p}.exclude-current-row", 1, Tok(TokenType.Current), Tok(TokenType.Row)),
                    Prod($"{p}.exclude-group", 1, Tok(TokenType.Group)),
                    Prod($"{p}.exclude-ties", 1, Tok(TokenType.Ties))
                )
            )
        );

        // window-defn https://sqlite.org/syntax/window-defn.html
        TopProd(
            p = "window-defn",
            1,
            Tok(TokenType.Lp),
            Opt(Id("base window name")),
            Opt(
                Tok(TokenType.Partition),
                Tok(TokenType.By),
                Lst($"{p}.partition-expr", TokenType.Comma, 1, SubProd("expr"))
            ),
            Opt(
                Tok(TokenType.Order),
                Tok(TokenType.By),
                Lst($"{p}.order-expr", TokenType.Comma, 1, SubProd("ordering-term"))
            ),
            Opt(SubProd("frame-spec")),
            Tok(TokenType.Rp)
        );

        // window-function-invocation - https://sqlite.org/syntax/window-function-invocation.html
        TopProd(
            p = "window-function-invocation",
            6,
            Id("window function"),
            Tok(TokenType.Lp),
            Opt(Or(Tok(TokenType.Star), Lst($"{p}.argument", TokenType.Comma, 1, SubProd("expr")))),
            Tok(TokenType.Rp),
            Opt(SubProd("filter-clause")),
            Tok(TokenType.Over),
            Or(SubProd("window-defn"), Id("window name"))
        );

        // upsert-clause - https://sqlite.org/syntax/upsert-clause.html
        TopProd(
            p = "upsert-clause",
            1,
            Lst(
                $"{p}.list-item",
                null,
                1,
                Tok(TokenType.On),
                Tok(TokenType.Conflict),
                Opt( // conflict target
                    Tok(TokenType.Lp),
                    Lst($"{p}.conflict-target.column", TokenType.Comma, 1, SubProd("indexed-column")),
                    Tok(TokenType.Rp),
                    Opt(Tok(TokenType.Where), SubProd("expr"))
                ),
                Tok(TokenType.Do),
                Or(
                    Prod($"{p}.do-nothing", 1, Tok(TokenType.Nothing)),
                    Prod(
                        $"{p}.update",
                        1,
                        Tok(TokenType.Update),
                        Tok(TokenType.Set),
                        Lst(
                            $"{p}.update-column",
                            TokenType.Comma,
                            1,
                            Or(Id("column name"), SubProd("column-name-list")),
                            Tok(TokenType.Eq),
                            SubProd("expr")
                        ),
                        Opt(Tok(TokenType.Where), SubProd("expr"))
                    )
                )
            )
        );

        // returning-clause - https://sqlite.org/syntax/returning-clause.html
        TopProd(
            p = "returning-clause",
            1,
            Tok(TokenType.Returning),
            Lst(
                $"{p}.item",
                TokenType.Comma,
                1,
                Or(
                    Prod($"{p}.item.expr", 1, SubProd("expr"), Opt(Opt(Tok(TokenType.As)), Id("column alias"))),
                    Prod($"{p}.item.star", 1, Tok(TokenType.Star))
                )
            )
        );

        SelfCheck();
    }

    private static void TopProd(string name, int numReq, params SpecTerm[] terms)
    {
        _dict[name] = Prod(name, numReq, terms);
    }

    private static SpecProd Prod(string name, int numReq, params SpecTerm[] terms)
    {
        return new SpecProd
        {
            Name = name,
            NumReq = numReq,
            Terms = terms,
        };
    }

    private static IdentifierTerm Id(string desc, bool allowVar = false)
    {
        return new IdentifierTerm { Desc = desc, AllowVariable = allowVar };
    }

    private static KeyTokenTerm Tok(TokenType type)
    {
        return new KeyTokenTerm { Type = type };
    }

    private static StringTokenTerm Tok(string text)
    {
        return new StringTokenTerm { Text = text };
    }

    private static TokenSetTerm Toks(params TokenType[] types)
    {
        return new TokenSetTerm { Types = types };
    }

    private static OptionalTerm Opt(int numReq, params SpecTerm[] terms)
    {
        return new OptionalTerm
        {
            Prod = new SpecProd { NumReq = numReq, Terms = terms },
        };
    }

    private static OptionalTerm Opt(params SpecTerm[] terms)
    {
        return new OptionalTerm
        {
            Prod = new SpecProd { NumReq = terms.Length, Terms = terms },
        };
    }

    private static OrTerm Or(params SpecProd[] prods)
    {
        return new OrTerm { Prods = prods };
    }

    private static OrTerm Or(params SpecTerm[] terms)
    {
        return new OrTerm { Prods = terms.Select(x => new SpecProd { NumReq = 1, Terms = new[] { x } }).ToArray() };
    }

    private static ProdTerm SubProd(string name)
    {
        return new ProdTerm { ProdName = name };
    }

    private static OrTerm SubProd(SpecProd prod)
    {
        return new OrTerm { Prods = new[] { prod } };
    }

    private static ListTerm Lst(string subProdName, TokenType? separator, int min, params SpecTerm[] terms)
    {
        return new ListTerm
        {
            SeparatorProd = separator.HasValue ? Prod(".list-separator", 1, Tok(separator.Value)) : null,
            Min = min,
            ItemProd = Prod(subProdName, terms.Length, terms),
        };
    }

    private static ListTerm LstP(string subProdName, SpecProd separatorProd, int min, params SpecTerm[] terms)
    {
        return new ListTerm
        {
            SeparatorProd = separatorProd,
            Min = min,
            ItemProd = Prod(subProdName, terms.Length, terms),
        };
    }

    private static ListTerm LstP(string subProdName, SpecTerm separatorTerm, int min, params SpecTerm[] terms)
    {
        return new ListTerm
        {
            SeparatorProd = Prod(".list-separator", 1, separatorTerm),
            Min = min,
            ItemProd = Prod(subProdName, terms.Length, terms),
        };
    }

    private static LiteralStringTerm LitStr(string desc)
    {
        return new LiteralStringTerm { Desc = desc };
    }

    private static BreakpointTerm Breakpoint()
    {
        return new BreakpointTerm();
    }

    private static void SelfCheck()
    {
        // make sure all SubProd() calls refer to productions that we have heard of
        foreach (var prod in _dict.Values)
        {
            SelfCheck(prod);
        }
    }

    private static void SelfCheck(SpecProd prod)
    {
        foreach (var term in prod.Terms)
        {
            if (term is ProdTerm)
            {
                var prodTerm = (ProdTerm)term;
                if (!_dict.ContainsKey(prodTerm.ProdName))
                {
                    throw new Exception(
                        $"Internal error. Production {prod.Name} references sub-production "
                            + $"\"{prodTerm.ProdName}\" which does not exist."
                    );
                }
            }
            foreach (var subProd in GetSubProds(term))
            {
                SelfCheck(subProd);
            }
        }
    }

    private static IEnumerable<SpecProd> GetSubProds(SpecTerm term)
    {
        if (term is OptionalTerm)
        {
            var optTerm = (OptionalTerm)term;
            yield return optTerm.Prod;
        }
        else if (term is OrTerm)
        {
            var orTerm = (OrTerm)term;
            foreach (var prod in orTerm.Prods)
            {
                yield return prod;
            }
        }
        else if (term is ListTerm)
        {
            var listTerm = (ListTerm)term;
            yield return listTerm.ItemProd;
            if (listTerm.SeparatorProd != null)
            {
                yield return listTerm.SeparatorProd;
            }
        }
    }
}
