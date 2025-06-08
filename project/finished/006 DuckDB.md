- [x] Add DuckDB import support.
    - [x] Add `<PackageReference Include="DuckDB.NET.Data.Full" Version="1.3.0" />` (Full version includes native components)
    - [x] Language update
        - [x] We have an `IMPORT DATABASE` command already. Read and update `doc/import-database-stmt.html`. Add `duckdb` as a new "vendor" where the connection string is the path to the file.
        - [x] The implementation is in `src\SqlNotebookScript\Interpreter\ImportDatabaseStmtRunner.cs`.
    - [x] UI updates
        - [x] "Import" menu > "From file...": Update open file dialog filter to include *.duckdb.
        - [x] When .duckdb file is selected, transition to `DatabaseImportTablesForm`. This is what MySQL/PostgreSQL/SQL Server import uses, but they start off in `DatabaseConnectionForm` whereas for DuckDB we start in the open file dialog. DuckDB connects like CSV but it imports like SQL Server.
        - [x] Currently when the user drag-and-drops a .csv file to our window, we open the Import CSV form with that file. Make the same happen here when dragging in a .duckdb file.
    - [x] Tests
        - [x] A test file is available in `src/Tests/files/example.duckdb`. You may create more test files with `duckdb` CLI if you need.
        - [x] Exercise `IMPORT DATABASE` for DuckDB using real files in `src/Tests/files`. See `src\Tests\scripts\IMPORT TEXT.sql` for an example.
    - [x] I changed my mind about the connection string. We made it simply the path to the .duckdb file, but our documentation defines it to be a proper ADO.NET connection string. Let's change it to be the ADO.NET connection string, so the user will have to type `Data Source=C:\Path\To\file.duckdb` instead of simply `C:\Path\To\file.duckdb`. We want to mimic the other SQL providers more closely, we ended up needing to special-case DuckDB to do the filename-only connection string.
    - [x] Add a test for the `LINK: 1` option, which creates a live database connection instead of copying data. The default is `LINK: 0` and you've only tested that. Link mode involves implementing an `AdoModuleProvider` (see example `SqlServerAdoModuleProvider`) and registering it in `Notebook.Init()`. Read `doc/import-database-stmt.html` for context. The following query should work and you should be able to query it.
        ```
        IMPORT DATABASE 'duckdb'
        CONNECTION 'Data Source=<FILES>\example.duckdb'
        TABLE employees
        OPTIONS (LINK: 1)
        ```
