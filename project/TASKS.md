- [ ] Create a way to execute a script inside a notebook file from the command line, without opening the GUI.
    - [X] Create a new project `SqlNotebookCmd` as a console application. Add to `src/SqlNotebook.sln`. Add references from `SqlNotebook` to `SqlNotebookCmd`, from `Tests` to `SqlNotebookCmd`, and from `SqlNotebookCmd` to `SqlNotebookScript`.
    - [X] Read `src/Tests/Program.cs` and `src/Tests/ScriptTest.cs` for an example of how to use a notebook from a separate console project. `SqlNotebookCmd` will be similar, but user-facing.
    - [X] Usage: `SqlNotebookCmd "C:\path\to\notebook.sqlnb" "MyScriptName"`
    - [X] Provide usage output on `--help` or if the arguments aren't correct.
    - [X] On success, the output is a series of tables. Write them each in to stdout in CSV format with a blank line between adjacent tables, no blank line at the very end of the output. Exit 0.
    - [X] On failure, show the error in stderr and exit 1.
    - [X] Notebook is not saved, any changes are discarded. We will tackle this problem later in way orthogonal to this feature.
    - [X] We have a test notebook in `src/Tests/files/cli_test.sqlnb`.
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
    - [X] Add `src/Tests/SqlNotebookCmdTest.cs`. Locate the `SqlNotebookCmd.exe` in the same directory as `Tests.exe`, run it, collect the output, verify it.
    - [X] Write a documentation page in `doc/`. Look at `doc/windows-7.html` for a formatting example.
    - [X] Our documentation mentions that we open the notebook in read-only mode. Let's _not_ do that. We plan to offer a "SAVE" command in the future that will actually write to the file. Prepare for that by opening the notebook normally (not read only) and removing that mention from the documentation.
    - [X] Update our main homepage's marketing copy in `web/index.html` with a brief mention of this feature.
