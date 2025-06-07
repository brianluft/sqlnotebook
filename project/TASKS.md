- [ ] Create a way to execute a script inside a notebook file from the command line, without opening the GUI.
    - [X] Create a new project `SqlNotebookCmd` as a console application. Add to `src/SqlNotebook.sln`. Add references from `SqlNotebook` to `SqlNotebookCmd`, from `Tests` to `SqlNotebookCmd`, and from `SqlNotebookCmd` to `SqlNotebookScript`.
    - [ ] Read `src/Tests/Program.cs` and `src/Tests/ScriptTest.cs` for an example of how to use a notebook from a separate console project. `SqlNotebookCmd` will be similar, but user-facing.
    - [ ] Usage: `SqlNotebookCmd "C:\path\to\notebook.sqlnb" "MyScriptName"`
    - [ ] Provide usage output on `--help` or if the arguments aren't correct.
    - [ ] On success, the output is a series of tables. Write them each in to stdout in CSV format with a blank line between adjacent tables, no blank line at the very end of the output. Exit 0.
    - [ ] On failure, show the error in stderr and exit 1.
    - [ ] Notebook is not saved, any changes are discarded. We will tackle this problem later in way orthogonal to this feature.
    - [ ] We have a test notebook in `src/Tests/files/cli_test.sqlnb`.
        - It contains a script `Script1` with the following queries:
            ```
            SELECT 1 AS id, 'Hello' AS name
            UNION ALL
            SELECT 2 AS id, 'World' AS name

            SELECT 3 AS foo, 4 AS bar
            ```
        - Expected output from the CLI:
            ```
            id,name
            1,Hello
            2,World

            foo,bar
            3,4
            ```
    - [ ] Add `src/Tests/SqlNotebookCmdTest.cs`. Locate the `SqlNotebookCmd.exe` in the same directory as `Tests.exe`, run it, collect the output, verify it.
