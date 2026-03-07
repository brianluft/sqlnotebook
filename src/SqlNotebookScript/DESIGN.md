# SqlNotebookScript Design

SqlNotebookScript is the scripting engine and database abstraction layer for SQL Notebook. It extends standard SQLite SQL with procedural programming constructs (variables, loops, conditionals, error handling) and provides import/export engines for CSV, Excel, text, and external databases.

## Architecture Overview

```
                      ScriptParser
                          |
              +-----------+-----------+
              |                       |
         TokenQueue              SqliteParser
        (from native             (grammar-based
         tokenizer)               validation)
              |                       |
              +--------> AST <--------+
                          |
                     ScriptRunner
                          |
              +-----------+-----------+
              |           |           |
         Notebook    ScriptEnv   ScriptOutput
        (SQLite DB)  (variables,  (results,
                      control     tables,
                      flow)       text)
```

## Directory Structure

- **Core/** - Database engine facade and native interop
  - **AdoModules/** - ADO.NET providers for external databases (SQL Server, PostgreSQL, MySQL, DuckDB, SQLite)
  - **GenericModules/** - Virtual table module providers for table-valued functions
  - **SqliteInterop/** - P/Invoke declarations for the native `sqlite3.dll`
- **Interpreter/** - Script parsing and execution
  - **Ast/** - Abstract syntax tree node definitions (28+ statement types)
- **DataTables/** - Result set representations (memory-backed and disk-backed)
- **ScalarFunctions/** - Custom SQL scalar functions (date, math, system, array, regex, download)
- **TableFunctions/** - Custom SQL table-valued functions (list_files, read_file, list_xls_worksheets)
- **Macros/** - SQL statement transformation macros
- **Utils/** - Shared utilities (CSV, Excel, date/time, argument validation, blob handling)

## Core Database Layer (`Core/`)

### Notebook (`Core/Notebook.cs`)

The central class. Wraps a SQLite database with P/Invoke calls to the native `sqlite3.dll`. Provides the primary API for all database operations.

Key responsibilities:
- **Lifecycle**: `Notebook.New()` creates a new in-memory notebook; `Notebook.Open(filePath)` opens an existing `.sqlnb` file. The notebook is `IDisposable`.
- **Static init**: `Notebook.InitSqlite()` must be called once at startup. It loads the native DLL and SQLite extensions (`crypto.dll`, `fuzzy.dll`, `stats.dll`), and registers built-in functions (UUID, series).
- **Query execution**: `Query(sql, args)` returns a `SimpleDataTable`; `Execute(sql, args)` runs without results; `QueryValue(sql, args)` returns a single scalar.
- **Prepared statements**: `Prepare(sql)` returns a `PreparedStatement` for repeated execution with parameter binding.
- **Tokenization**: `Tokenize(sql)` calls the native `SxGetToken` export to tokenize SQL input for the parser.
- **Persistence**: `Save()` and `SaveAs(filePath)` serialize the notebook. The file format is a SQLite database with the `.sqlnb` extension.
- **UserData**: The `UserData` property (`NotebookUserData`) holds the notebook's pages and scripts as a serialized structure within the database.
- **Thread safety**: Uses a static lock for all database operations.

### PreparedStatement (`Core/PreparedStatement.cs`)

Wraps a native SQLite prepared statement handle. Supports positional and named parameter binding, result iteration via row callbacks, and automatic parameter name extraction.

### ADO Module Providers (`Core/AdoModules/`)

Enable importing data from external databases by registering SQLite virtual tables backed by ADO.NET connections:

| Provider | Class | ADO.NET Driver |
|----------|-------|----------------|
| SQL Server | `SqlServerAdoModuleProvider` | `Microsoft.Data.SqlClient` |
| PostgreSQL | `PostgreSqlAdoModuleProvider` | `Npgsql` |
| MySQL | `MySqlAdoModuleProvider` | `MySql.Data` |
| DuckDB | `DuckDBAdoModuleProvider` | `DuckDB.NET` |
| SQLite | `SQLiteAdoModuleProvider` | `Microsoft.Data.Sqlite` |

Each provider registers a virtual table module (e.g., `CREATE VIRTUAL TABLE t USING virtsqlserver(...)`) that maps queries to the external database.

### Generic Module Provider (`Core/GenericModules/`)

Enables custom table-valued functions to be exposed as SQLite virtual tables. Used to register `list_files`, `read_file`, and `list_xls_worksheets`.

## Scripting Language

### Custom Statements

The scripting language extends SQLite SQL with these procedural constructs:

| Statement | Description |
|-----------|-------------|
| `DECLARE @var [AS type] [= expr]` | Declare a local variable |
| `DECLARE PARAMETER @var [= default]` | Declare a script parameter |
| `SET @var = expr` | Assign a value to a variable |
| `IF expr BEGIN ... END [ELSE BEGIN ... END]` | Conditional execution |
| `WHILE expr BEGIN ... END` | Conditional loop |
| `FOR @i = start TO end [STEP step] BEGIN ... END` | Numeric iteration |
| `FOREACH (@col1, ...) IN (SELECT ...) BEGIN ... END` | Iterate over query results |
| `BREAK` | Exit innermost loop |
| `CONTINUE` | Skip to next loop iteration |
| `RETURN [value]` | Return from script |
| `EXECUTE script_name [@param = value, ...]` | Call another script |
| `PRINT expr` | Output text |
| `BEGIN TRY ... END TRY BEGIN CATCH ... END CATCH` | Exception handling |
| `THROW [message]` | Raise an error |
| `RETHROW` | Re-raise a caught error |
| `IMPORT CSV/TXT/XLS/DATABASE ...` | Import data from external sources |
| `EXPORT CSV/TXT ...` | Export data to files |
| `CREATE SCRIPT name AS 'sql'` | Create a stored script |
| `DROP SCRIPT/PAGE name` | Remove a notebook item |
| `SAVE ['filename']` | Persist the notebook to disk |

### Parser (`Interpreter/ScriptParser.cs`)

Recursive descent parser that converts SQL text into an AST.

**Parsing flow:**
1. Input SQL is tokenized using the native SQLite tokenizer (`Notebook.Tokenize`), producing a `TokenQueue`.
2. `MacroProcessor` applies any registered macros to transform the token stream.
3. `ScriptParser.Parse()` inspects the first token(s) to determine statement type (DECLARE, SET, IF, WHILE, etc.).
4. Custom statements are parsed directly by `ScriptParser`.
5. Standard SQL statements are delegated to `SqliteParser`, which validates them against the full SQLite grammar defined in `SqliteGrammar.cs`.
6. The result is an `Ast.Script` containing a `Block` of `Stmt` nodes.

### SQLite Grammar (`Interpreter/SqliteGrammar.cs`)

Contains the complete SQLite BNF grammar as production rules using combinator classes (`Or`, `Opt`, `Tok`, `SubProd`). The `Matcher` class performs recursive descent matching with backtracking. This is used to validate that standard SQL statements conform to SQLite syntax before execution.

### Script Runner (`Interpreter/ScriptRunner.cs`)

Executes an `Ast.Script` by dispatching each statement to the appropriate handler based on its type. Maintains a `ScriptEnv` (execution environment) with:

- **Variables**: Case-insensitive dictionary of variable names to values.
- **Control flow flags**: `DidReturn`, `DidBreak`, `DidContinue`.
- **Output accumulation**: `ScriptOutput` collects data tables, text output, and scalar results.
- **Sub-script execution**: `EXECUTE` statements resolve script names from the notebook's stored scripts and recursively invoke `ScriptRunner`.

### AST Nodes (`Interpreter/Ast/`)

28+ statement node types, all inheriting from `Ast.Stmt`. Key nodes include:
- `DeclareStmt`, `SetStmt` - Variable operations
- `IfStmt`, `WhileStmt`, `ForStmt`, `ForeachStmt` - Control flow
- `SqlStmt` - Standard SQL (contains both raw text and a `SqliteSyntaxProduction` parse tree)
- `ImportCsvStmt`, `ImportXlsStmt`, `ImportDatabaseStmt`, `ExportCsvStmt` - Data I/O
- `ExecuteStmt` - Script invocation
- `TryCatchStmt`, `ThrowStmt` - Error handling

## Built-in Functions

### Scalar Functions (`ScalarFunctions/`)

Registered via reflection during `Notebook.InitSqlite()`. Each class provides one or more SQLite scalar functions:

- **DateFunctions**: `dateadd`, `datediff`, `datepart`, `datename`, `date_trunc`, `datefromparts`, `datetimefromparts`, `day`, `month`, `year`, `eomonth`, `getdate`, `getutcdate`, `now`, `isdate`, `to_date`, `to_datetime`
- **MathFunctions**: `round`, `sign`, `abs`, `ceiling`, `floor`
- **SystemFunctions**: `newid`, `host_name`, `user_name`, `read_file_text`, `isnumeric`, `choose`, `error_message`
- **ArrayFunctions**: `array`, `array_get`, `array_set`, `array_insert`, `array_remove`, `array_slice`, `array_concat`, `array_length`
- **RegexpFunction**: Regular expression matching
- **DownloadFunction**: HTTP GET requests returning file content

### Table-Valued Functions (`TableFunctions/`)

Exposed as virtual tables via `GenericModuleProvider`:
- `list_files(root_path, recursive)` - File system enumeration with metadata
- `read_file(path)` - Read file contents into rows
- `list_xls_worksheets(file_path)` - List Excel worksheet names

## Import/Export Engines

### Import Statement Runners

Each import type has a dedicated runner class in `Interpreter/`:

- **ImportCsvStmtRunner**: Parses CSV/TSV files with configurable delimiter, encoding, header handling, type conversion, and blank value treatment.
- **ImportTxtStmtRunner**: Line-by-line text import with line number and text columns.
- **ImportXlsStmtRunner**: Excel file import using `ExcelDataReader`, supporting multiple worksheets and column type mapping.
- **ImportDatabaseStmtRunner**: Connects to external databases via ADO.NET, supports schema/table/query selection with virtual table linking or data copy.

### Export Statement Runners

- **ExportCsvStmtRunner**: Exports query results, table data, or script output to CSV with configurable quoting, delimiter, and encoding.
- **ExportTxtStmtRunner**: Exports query results as formatted text.

## Result Sets (`DataTables/`)

- **SimpleDataTable** (abstract base): Column names and row access.
- **MemorySimpleDataTable**: Entire result set stored in memory. Used for small results.
- **DiskSimpleDataTable**: Large results spilled to temporary disk storage for memory efficiency.
- **SimpleDataTableBuilder**: Incrementally constructs result sets during query execution.

## NuGet Dependencies

| Package | Purpose |
|---------|---------|
| DuckDB.NET.Data.Full | DuckDB database access |
| ExcelDataReader | Excel file (.xls/.xlsx) parsing |
| HtmlAgilityPack | HTML parsing (for documentation) |
| Microsoft.Data.SqlClient | SQL Server ADO.NET provider |
| Microsoft.Data.Sqlite | SQLite ADO.NET provider |
| MySql.Data | MySQL ADO.NET provider |
| Npgsql | PostgreSQL ADO.NET provider |
| SharpZipLib | ZIP archive handling |

## Key Design Decisions

- **SQLite as the runtime**: All data lives in SQLite. External databases are accessed through virtual tables, presenting a unified SQL interface.
- **Native tokenizer**: The parser reuses SQLite's own tokenizer (via P/Invoke to `SxGetToken`) for token-level compatibility with SQLite SQL.
- **Grammar validation**: Standard SQL is validated against the full SQLite grammar before execution, catching syntax errors early with helpful messages.
- **Macro system**: `MacroProcessor` allows transparent SQL transformations (e.g., rewriting queries) without modifying the grammar itself.
- **Disk-backed results**: Large result sets automatically spill to disk to prevent out-of-memory conditions.
- **Extensibility by reflection**: Custom functions, macros, and ADO modules are discovered and registered via reflection at initialization.
