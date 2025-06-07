- [x] Add FOREACH loop construct. We have WHILE and FOR already.
    - [x] Production: 'FOREACH' '(' <variables-list> ')' 'IN' <table-name> <block>
    - [x] Example: `FOREACH (@a, @b, @c) IN my_table BEGIN PRINT @a; END`
    
    ## Implementation Tasks (Grammar Production):
    
    ### 1. Create AST Node
    - [x] Create `src/SqlNotebookScript/Interpreter/Ast/ForeachStmt.cs`
        - [x] Inherit from `Stmt` 
        - [x] Add `List<string> VariableNames` property for the variable list
        - [x] Add `IdentifierOrExpr TableExpr` property for the table name/expression (improved to use IdentifierOrExpr)
        - [x] Add `Block Block` property for the code block to execute
        - [x] Implement `GetChildren()` to return `new Node[] { TableExpr, Block }`

    ### 2. Update Parser
    - [x] Update `src/SqlNotebookScript/Interpreter/ScriptParser.cs`
        - [x] Add `"foreach"` case to the switch statement in `ParseStmt()` method (~line 40-70)
        - [x] Implement `ParseForeachStmt(TokenQueue q)` method:
            - [x] Parse `"foreach"` keyword
            - [x] Parse `"("` 
            - [x] Parse comma-separated list of variable names using `ParseVariableName()`
            - [x] Parse `")"` 
            - [x] Parse `"in"` keyword
            - [x] Parse table name/expression using `ParseIdentifierOrExpr()` (improved)
            - [x] Parse block using `ParseBlock()`
            - [x] Call `ConsumeSemicolon()`

    ### 3. Update Runner
    - [x] Update `src/SqlNotebookScript/Interpreter/ScriptRunner.cs`
        - [x] Add `[typeof(Ast.ForeachStmt)] = (s, e) => ExecuteForeachStmt((Ast.ForeachStmt)s, e)` to the `_stmtRunners` dictionary (~line 20-35)
        - [x] Implement `ExecuteForeachStmt(Ast.ForeachStmt stmt, ScriptEnv env)` method:
            - [x] Evaluate the table expression to get table name
            - [x] Query the table to get all rows and column names
            - [x] Auto-declare variables (improved: no need for manual DECLARE statements)
            - [x] For each row:
                - [x] Set each variable to the corresponding column value 
                - [x] Execute the block using `ExecuteBlock(stmt.Block, env)`
                - [x] Handle `env.DidReturn`, `env.DidBreak`, `env.DidContinue` like other loops
            - [x] Handle edge cases (empty table, non-existent table, etc.)

    ### 4. Write Tests
    - [x] Create test files in `src/Tests/scripts/`:
        - [x] `FOREACH_basic.sql` - Basic functionality test
        - [x] `FOREACH_empty_table.sql` - Empty table test  
        - [x] `FOREACH_single_column.sql` - Single variable test
        - [x] `FOREACH_break_continue.sql` - BREAK/CONTINUE test
        - [x] `FOREACH_nested.sql` - Nested loops test
        - [x] Follow the pattern: SQL commands, then `--output--`, then expected output
        - [x] Use `PRINT` for scalar values, `SELECT` for tables

    ### 5. Write Documentation  
    - [x] Create `doc/foreach-stmt.html`
        - [x] Follow the exact structure of `doc/while-stmt.html` and `doc/for-stmt.html`
        - [x] Include syntax diagram (can create using pikchr like other statements)
        - [x] Document parameters: variable-list, table-name, statement
        - [x] Include practical examples
        - [x] Mention BREAK/CONTINUE support
        - [x] Create `doc/art/foreach-stmt-syntax.pikchr` for the syntax diagram

    ### 6. Build and Test
    - [x] Run `scripts/build.sh` to build after each major change
    - [x] Verify all tests pass (215/215 tests passing)
    - [x] Test manually with various table structures
    - [x] Test error conditions and edge cases

    ## Technical Notes:
    - Variables will be assigned values in the order they appear in the variable list
    - If fewer variables than columns: extra columns ignored
    - If more variables than columns: extra variables set to NULL
    - Table expression can be a table name, view name, or subquery
    - Support BREAK/CONTINUE like other loop constructs
    - Handle SQL injection protection through parameterized queries