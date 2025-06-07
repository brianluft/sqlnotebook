- [ ] Add FOREACH loop construct. We have WHILE and FOR already.
    - [ ] Production: 'FOREACH' '(' <variables-list> ')' 'IN' <table-name> <block>
    - [ ] Example: `FOREACH (@a, @b, @c) IN my_table BEGIN SELECT @a, @b, @c; END`
    
    ## Implementation Tasks (Grammar Production):
    
    ### 1. Create AST Node
    - [ ] Create `src/SqlNotebookScript/Interpreter/Ast/ForeachStmt.cs`
        - [ ] Inherit from `Stmt` 
        - [ ] Add `List<string> VariableNames` property for the variable list
        - [ ] Add `Expr TableExpr` property for the table name/expression
        - [ ] Add `Block Block` property for the code block to execute
        - [ ] Implement `GetChildren()` to return `new Node[] { TableExpr, Block }`

    ### 2. Update Parser
    - [ ] Update `src/SqlNotebookScript/Interpreter/ScriptParser.cs`
        - [ ] Add `"foreach"` case to the switch statement in `ParseStmt()` method (~line 40-70)
        - [ ] Implement `ParseForeachStmt(TokenQueue q)` method:
            - [ ] Parse `"foreach"` keyword
            - [ ] Parse `"("` 
            - [ ] Parse comma-separated list of variable names using `ParseVariableName()`
            - [ ] Parse `")"` 
            - [ ] Parse `"in"` keyword
            - [ ] Parse table name/expression using `ParseExpr()`
            - [ ] Parse block using `ParseBlock()`
            - [ ] Call `ConsumeSemicolon()`

    ### 3. Update Runner
    - [ ] Update `src/SqlNotebookScript/Interpreter/ScriptRunner.cs`
        - [ ] Add `[typeof(Ast.ForeachStmt)] = (s, e) => ExecuteForeachStmt((Ast.ForeachStmt)s, e)` to the `_stmtRunners` dictionary (~line 20-35)
        - [ ] Implement `ExecuteForeachStmt(Ast.ForeachStmt stmt, ScriptEnv env)` method:
            - [ ] Evaluate the table expression to get table name
            - [ ] Query the table to get all rows and column names
            - [ ] Validate that number of variables matches number of columns (or handle mismatch appropriately)
            - [ ] For each row:
                - [ ] Set each variable to the corresponding column value 
                - [ ] Execute the block using `ExecuteBlock(stmt.Block, env)`
                - [ ] Handle `env.DidReturn`, `env.DidBreak`, `env.DidContinue` like other loops
            - [ ] Handle edge cases (empty table, non-existent table, etc.)

    ### 4. Write Tests
    - [ ] Create test files in `src/Tests/scripts/`:
        - [ ] `FOREACH_basic.sql` - Basic functionality test
        - [ ] `FOREACH_empty_table.sql` - Empty table test  
        - [ ] `FOREACH_single_column.sql` - Single variable test
        - [ ] `FOREACH_break_continue.sql` - BREAK/CONTINUE test
        - [ ] `FOREACH_nested.sql` - Nested loops test
        - [ ] Follow the pattern: SQL commands, then `--output--`, then expected output
        - [ ] Use `PRINT` for scalar values, `SELECT` for tables

    ### 5. Write Documentation  
    - [ ] Create `doc/foreach-stmt.html`
        - [ ] Follow the exact structure of `doc/while-stmt.html` and `doc/for-stmt.html`
        - [ ] Include syntax diagram (can create using pikchr like other statements)
        - [ ] Document parameters: variable-list, table-name, statement
        - [ ] Include practical examples
        - [ ] Mention BREAK/CONTINUE support
        - [ ] Create `doc/art/foreach-stmt-syntax.pikchr` for the syntax diagram

    ### 6. Build and Test
    - [ ] Run `scripts/build.sh` to build after each major change
    - [ ] Verify all tests pass
    - [ ] Test manually with various table structures
    - [ ] Test error conditions and edge cases

    ## Technical Notes:
    - Variables will be assigned values in the order they appear in the variable list
    - If fewer variables than columns: extra columns ignored
    - If more variables than columns: extra variables set to NULL
    - Table expression can be a table name, view name, or subquery
    - Support BREAK/CONTINUE like other loop constructs
    - Handle SQL injection protection through parameterized queries