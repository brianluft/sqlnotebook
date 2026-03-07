# SQL Notebook - Design Overview

SQL Notebook is a Windows desktop application for casual data exploration in SQL. It provides a Jupyter-style notebook interface powered by an extended SQLite engine. Users can import data from CSV, Excel, SQL Server, PostgreSQL, MySQL, and DuckDB, then explore it using standard SQL plus procedural scripting extensions.

## What SQL Notebook Does

- **Notebook interface**: Pages containing text blocks and SQL query blocks with inline results, similar to Jupyter notebooks
- **SQL scripting**: Extends SQLite SQL with variables, loops, conditionals, error handling, and stored scripts
- **Data import**: CSV, TSV, Excel (.xls/.xlsx), and external databases (SQL Server, PostgreSQL, MySQL, DuckDB, SQLite)
- **Data export**: CSV and formatted text output
- **Interactive console**: Ad-hoc SQL execution with scrolling result history
- **CLI tool**: Batch execution of scripts from notebook files
- **50+ built-in functions**: Date/time, math, arrays, crypto, fuzzy matching, statistics, file I/O, HTTP downloads

## Technology Stack

| Layer | Technology |
|-------|-----------|
| GUI | .NET 9.0 Windows Forms, ScintillaNET (code editor), DockPanelSuite (docking layout) |
| Scripting engine | C# (.NET 9.0), custom recursive descent parser and AST interpreter |
| Database engine | SQLite 3.52.0 (compiled from source as native DLL) |
| Native extensions | C/C++ DLLs from sqlean library (crypto, fuzzy matching, statistics) |
| External databases | ADO.NET providers via virtual tables (SqlClient, Npgsql, MySql.Data, DuckDB.NET) |
| Installer | WiX Toolset 3.14 (MSI), plus portable ZIP |
| Build | MSBuild 17.x (VS 2022), bash scripts, PowerShell automation |
| CI | GitHub Actions (parallel x64/ARM64 builds, x64 tests) |

## Repository Structure

```
sqlnotebook/
  src/                          # All source code
    SqlNotebook.sln             # Visual Studio solution
    SqlNotebook/                # GUI application (Windows Forms)
    SqlNotebookScript/          # Scripting engine and database layer
    SqlNotebookCmd/             # Command-line interface
    SqlNotebookDb/              # Native SQLite DLL (C)
    crypto/                     # Crypto extension DLL (C, from sqlean)
    fuzzy/                      # Fuzzy matching extension DLL (C, from sqlean)
    stats/                      # Statistics extension DLL (C, from sqlean)
    Tests/                      # Test suite
    chocolatey/                 # Chocolatey package definition
    SqlNotebook.wxs             # WiX MSI installer definition
  ext/                          # Third-party source dependencies
    sqlite/                     # SQLite source and documentation
    sqlean/                     # sqlean extension source
    dockpanelsuite/             # DockPanel UI framework (modified from upstream)
    Windows-API-Code-Pack/      # Windows shell interop
    fatcow/                     # Icon library (PNG)
  doc/                          # HTML help documentation source
  web/                          # Website (sqlnotebook.com)
  scripts/                      # Bash build/run scripts
  ps1/                          # PowerShell automation scripts
  project/                      # Task tracking
```

## Project Dependency Graph

```
                    SqlNotebook (GUI)
                   /       |        \
                  /        |         \
   DockPanelSuite    SqlNotebookScript    Windows-API-Code-Pack
                           |
                      SqlNotebookDb
                     (sqlite3.dll)
                      /    |    \
                crypto  fuzzy  stats
                (.dll)  (.dll) (.dll)

   SqlNotebookCmd -----> SqlNotebookScript

   Tests -----> SqlNotebook + SqlNotebookScript + SqlNotebookCmd
```

**SqlNotebookScript** is the core library. It owns the database engine, script parser/interpreter, import/export engines, and all built-in functions. Both the GUI and CLI depend on it.

**SqlNotebookDb** compiles SQLite from source into `sqlite3.dll`. Three companion DLLs (`crypto.dll`, `fuzzy.dll`, `stats.dll`) are compiled from the sqlean library and loaded at runtime as SQLite extensions.

## Project Descriptions

### SqlNotebook (GUI) - [src/SqlNotebook/DESIGN.md](src/SqlNotebook/DESIGN.md)

Windows Forms application with a docking panel layout (Visual Studio-style). Contains:
- **MainForm**: Menu bar, docking panels, notebook lifecycle management
- **ExplorerControl**: Tree view of pages, scripts, tables, views (left panel)
- **ConsoleControl**: Interactive SQL console (bottom panel)
- **PageControl**: Notebook pages with text and query blocks (center)
- **QueryDocumentControl**: Full-screen SQL editor with results grid (center)
- **Import wizards**: Multi-step dialogs for CSV, Excel, and database imports
- **NotebookManager**: Coordinates between UI and the scripting engine

### SqlNotebookScript (Engine) - [src/SqlNotebookScript/DESIGN.md](src/SqlNotebookScript/DESIGN.md)

The scripting engine and database abstraction layer. Contains:
- **Notebook class**: Wraps SQLite via P/Invoke; manages the `.sqlnb` file format
- **ScriptParser**: Recursive descent parser producing an AST from SQL + procedural extensions
- **ScriptRunner**: AST interpreter with variable scoping, control flow, and sub-script execution
- **SqliteGrammar/SqliteParser**: Complete SQLite BNF grammar for SQL validation
- **Import/Export runners**: CSV, Excel, text, and database import/export engines
- **Built-in functions**: 50+ scalar and table-valued functions (date, math, array, crypto, etc.)
- **ADO module providers**: Virtual table modules for SQL Server, PostgreSQL, MySQL, DuckDB, SQLite

### SqlNotebookCmd (CLI) - [src/SqlNotebookCmd/DESIGN.md](src/SqlNotebookCmd/DESIGN.md)

Single-file command-line tool. Runs a named script from a `.sqlnb` file and outputs results to stdout in CSV format. Designed for automation and batch processing.

### SqlNotebookDb (Native) - [src/SqlNotebookDb/DESIGN.md](src/SqlNotebookDb/DESIGN.md)

Compiles SQLite 3.52.0 from source with custom compile-time options (FTS5, math functions, JSON, STAT4). Also covers the three sqlean extension DLLs (crypto, fuzzy, stats) that provide cryptographic hashing, string distance algorithms, and statistical aggregates.

### Tests - [src/Tests/DESIGN.md](src/Tests/DESIGN.md)

Custom test framework with 224+ file-based SQL test scripts. Each `.sql` file contains a script and its expected output. Tests are auto-generated from the script files by a PowerShell script. Covers the full scripting language, all import/export formats, built-in functions, and backward compatibility.

## Notebook File Format (.sqlnb)

A `.sqlnb` file is a SQLite database containing:
- **User tables and views**: Created by the user through SQL or imports
- **Notebook metadata**: Stored in internal `_sqlnotebook_*` tables
- **UserData**: Serialized structure holding pages (with text/query blocks) and scripts (SQL code + parameters)

The file is self-contained and portable. Opening a notebook creates a working copy; saving writes back to the original file.

## Scripting Language

SQL Notebook extends standard SQLite SQL with procedural constructs:

```sql
-- Variables
DECLARE @count = 0;
SET @count = (SELECT COUNT(*) FROM my_table);

-- Control flow
IF @count > 100 BEGIN
    PRINT 'Large dataset';
END ELSE BEGIN
    PRINT 'Small dataset';
END

-- Loops
FOREACH (@name, @age) IN (SELECT name, age FROM people) BEGIN
    IF @age >= 18 BEGIN
        INSERT INTO adults VALUES (@name, @age);
    END
END

-- Error handling
BEGIN TRY
    EXECUTE my_risky_script;
END TRY
BEGIN CATCH
    PRINT error_message();
END CATCH

-- Data import
IMPORT CSV 'data.csv' INTO my_table (name TEXT, value REAL);

-- Sub-script execution
EXECUTE transform_data @threshold = 50;
```

See [src/SqlNotebookScript/DESIGN.md](src/SqlNotebookScript/DESIGN.md) for the full statement reference and parser architecture.

## Build System

### Development Build

```bash
scripts/build.sh
```

1. Detect CPU architecture (x64 or ARM64)
2. Find MSBuild 17.x via `vswhere`
3. Auto-generate test methods from `Tests/scripts/*.sql`
4. Restore NuGet packages
5. Build native DLLs: `sqlite3.dll`, `crypto.dll`, `fuzzy.dll`, `stats.dll`
6. Generate help documentation from `doc/` HTML files
7. Build the .NET projects (Debug, self-contained)
8. Run the test suite

### Release Build

Two-phase process:

**Phase 1** (`scripts/publish.sh` -> `ps1/Start-Release.ps1`): Build Release binaries, copy VC++ redistributables, clean debug artifacts. Runs in GitHub Actions for both x64 and ARM64.

**Phase 2** (`ps1/Finish-Release.ps1`): Code sign executables, generate portable ZIP, build MSI installer via WiX, code sign MSI. Runs locally with HSM credentials.

### Dependency Management

`ps1/Update-Deps.ps1` downloads and extracts:
- SQLite amalgamation, source, and documentation (with SHA256 verification)
- sqlean source (auto-generates `.vcxproj` files from template)

### Other Scripts

| Script | Purpose |
|--------|---------|
| `scripts/run.sh` | Run the debug build |
| `scripts/clean.sh` | Remove all build artifacts |
| `scripts/format.sh` | Run CSharpier (C#) and HTML Tidy (docs) |
| `ps1/Update-Docs.ps1` | Generate help docs for the app and website |
| `ps1/Update-Tests.ps1` | Regenerate `ScriptTest.g.cs` from test script files |

## CI/CD (GitHub Actions)

Defined in `.github/workflows/sqlnotebook.yml`:
- **Build job** (matrix: x64, ARM64): Runs Phase 1 release build, uploads artifacts
- **Test job** (x64 only): Downloads dependencies, runs test suite

## External Dependencies

| Dependency | Type | License | Purpose |
|------------|------|---------|---------|
| SQLite 3.52.0 | Native source | Public Domain | Database engine |
| sqlean 0.27.2 | Native source | MIT | Crypto, fuzzy, stats SQL extensions |
| DockPanelSuite | Managed source | MIT | Docking panel UI framework |
| Windows-API-Code-Pack | Managed source | Microsoft | Windows shell integration |
| fatcow icons | Assets | CC-BY-3.0 | Application icons |
| ScintillaNET | NuGet | MIT | Code editor control |
| DuckDB.NET | NuGet | MIT | DuckDB database driver |
| Microsoft.Data.SqlClient | NuGet | MIT | SQL Server driver |
| Npgsql | NuGet | PostgreSQL | PostgreSQL driver |
| MySql.Data | NuGet | GPL | MySQL driver |
| ExcelDataReader | NuGet | MIT | Excel file parsing |
| HtmlAgilityPack | NuGet | MIT | HTML parsing for help system |
| SharpZipLib | NuGet | MIT | ZIP archive handling |
| WiX Toolset 3.14 | Build tool | MS-RL | MSI installer generation |
