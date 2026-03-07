# SqlNotebook Design

SqlNotebook is the Windows Forms GUI application that provides a Jupyter-style notebook interface for interactive SQL data exploration. It is the main user-facing project in the solution.

## Architecture Overview

```
+---------------------------------------------------------------+
|  MainForm                                                     |
|  +----------+  +--------------------------------------+       |
|  | Explorer |  |          Document Area               |       |
|  | Control  |  |  +------------+ +------------------+ |       |
|  | (tree of |  |  | PageControl| | QueryDocControl  | |       |
|  |  pages,  |  |  | (blocks)  | | (SQL editor +    | |       |
|  |  scripts,|  |  |           | |  results grid)   | |       |
|  |  tables, |  |  +------------+ +------------------+ |       |
|  |  views)  |  |  +------------------+                |       |
|  |          |  |  | TableDocControl  |                |       |
|  |          |  |  | (table viewer)   |                |       |
|  +----------+  +--------------------------------------+       |
|  +----------------------------------------------------+       |
|  | ConsoleControl (interactive query console)          |       |
|  +----------------------------------------------------+       |
+---------------------------------------------------------------+
         |                    |
    NotebookManager      UserOptions
         |
    SqlNotebookScript.Notebook
```

## Directory Structure

- **Root** - Main form, entry point, core controls, and utilities
- **Pages/** - Page/block UI system (notebook pages with text and query blocks)
- **Import/** - Data import wizards and infrastructure
  - **Csv/** - CSV/TSV import wizard
  - **Xls/** - Excel import wizard
  - **Database/** - External database import (SQL Server, PostgreSQL, MySQL, DuckDB, SQLite)

## Entry Point (`Program.cs`)

Application startup sequence:
1. Set High DPI mode (`SystemAware`)
2. Enable visual styles and register code page encoding provider
3. Call `Notebook.InitSqlite()` to initialize the native SQLite engine
4. Open a notebook from command-line argument, or create a new one via `Notebook.New()`
5. Launch `MainForm` with the notebook instance
6. On exit: clean up temporary files and shut down SQLite

## Main Form (`MainForm.cs`)

Inherits from `ZForm` (a custom `Form` base class). Uses `WeifenLuo.WinFormsUI.Docking` (DockPanel) for a Visual Studio-style docking layout.

**Layout:**
- **Menu bar**: File, Edit, View, Help menus plus a search textbox (Ctrl+H) and update notification
- **Left dock**: `ExplorerControl` - tree view of notebook items (Pages, Scripts, Tables, Views) with schema browser
- **Center**: Document area with tabbed docking for `PageControl`, `QueryDocumentControl`, `TableDocumentControl`
- **Bottom** (auto-hide): `ConsoleControl` - interactive SQL console with output history

**Key responsibilities:**
- Owns the `NotebookManager` instance
- Handles menu commands (New, Open, Save, Import, Export, etc.)
- Coordinates document opening/closing and dirty state tracking
- Manages the docking panel layout and theme (`VS2012Light`)

## NotebookManager (`NotebookManager.cs`)

Central coordinator between the UI and the `SqlNotebookScript.Notebook` engine. Provides:

- **Item management**: Create, rename, delete notebook items (pages, scripts, tables, views)
- **Script execution**: `ExecuteScript()` wraps `ScriptParser` + `ScriptRunner` to parse and run SQL, returning `ScriptOutput`
- **Transaction tracking**: Monitors `_isTransactionOpen` via a `Slot<bool>` to disable Save/Import during open transactions
- **Schema snapshots**: `DatabaseSchema` captures current table/column structure for UI display
- **Events**: Publishes `NotebookChange`, `NotebookItemOpenRequest`, `NotebookItemRename`, `NotebookItemDelete` events

## Document Controls

All dockable document types implement `IDocumentControl`:

### PageControl (`Pages/PageControl.cs`)

A notebook page containing an ordered list of blocks in a `FlowLayoutPanel`:

- **TextBlockControl**: Rich text content block
- **QueryBlockControl**: SQL code block with inline results display. Uses GDI+ custom painting for result rendering. Configurable: show/hide SQL, show/hide results, max display rows.
- **DividerBlockControl**: Clickable divider between blocks for inserting new blocks

Pages support edit mode (rearrange, add, delete blocks) and view mode (run all queries, display results).

### QueryDocumentControl (`QueryDocumentControl.cs`)

Full-screen SQL editor for scripts. Contains a `QueryEmbeddedControl` which combines:
- `SqlTextControl` (Scintilla-based SQL editor) at the top
- Tabbed results area (DataGridView) at the bottom
- Execute with F5

### TableDocumentControl (`TableDocumentControl.cs`)

Read-only table viewer. Displays table contents in a `DataGridView` when opened. Implements `IDocumentControlOpenNotification` to load data lazily on first display.

## SQL Editor (`SqlTextControl.cs`)

Wraps `ScintillaNET` (Scintilla text editor component) with SQL Notebook-specific configuration:
- Syntax highlighting using SQLite tokenizer for accurate keyword/string/number coloring
- F5 key binding for query execution
- Line numbers, word wrap, and configurable fonts

## Explorer (`ExplorerControl.cs`)

Left-panel tree view organized into sections:
- **Pages** - Notebook pages (double-click opens `PageControl`)
- **Scripts** - Stored SQL scripts (double-click opens `QueryDocumentControl`)
- **Tables** - Database tables with expandable column lists
- **Views** - Database views with expandable column lists

Supports context menus for rename, delete, and import operations. Refreshes on `NotebookChange` events.

## Console (`ConsoleControl.cs`)

Bottom-panel interactive SQL execution console:
- Text input area for ad-hoc queries
- Scrolling output history showing results as DataGridViews
- Limits display to 10,000 rows per query to prevent memory exhaustion

## Import System (`Import/`)

### Entry Point: FileImporter (`Import/FileImporter.cs`)

Dispatches file imports based on extension:
- `.csv`, `.tsv`, `.txt` -> `ImportCsvForm`
- `.xls`, `.xlsx` -> `ImportXlsForm`
- `.sqlite3`, `.sqlite`, `.db` -> `SQLiteImportSession` -> `DatabaseImportTablesForm`
- `.duckdb` -> `DuckDBImportSession` -> `DatabaseImportTablesForm`

Menu-driven database imports dispatch to `DatabaseConnectionForm` -> `DatabaseImportTablesForm`.

### CSV Import (`Import/Csv/ImportCsvForm.cs`)

Multi-step wizard:
1. **Options**: Delimiter, encoding, header row handling
2. **Columns**: Column name mapping and type selection (with automatic type detection via `TypeDetection.cs`)
3. **Preview**: Shows the generated `IMPORT CSV` SQL statement

### Excel Import (`Import/Xls/ImportXlsForm.cs`)

Two-step wizard:
1. Select worksheets to import
2. Column mapping and type selection

### Database Import (`Import/Database/`)

Generic framework for importing from external databases:
- `IImportSession` interface with implementations for each database vendor
- `DatabaseConnectionForm`: Build connection strings
- `DatabaseImportTablesForm`: Select tables to import (with rename support)
- `DatabaseImportScriptForm`: Choose between linking (virtual table) and copying
- `DatabaseImportCustomQueryForm`: Import via custom SQL query

### Type Detection (`Import/TypeDetection.cs`)

Automatic column type inference from sample data. Checks in priority order: Integer -> Real -> DateTime/Date -> Text. Uses a flags-based approach to handle ambiguous columns.

## Export (`ExportForm.cs`)

Modal dialog for exporting notebook items:
- Select a Script, Table, or View
- Export as SQL text or CSV data
- Uses standard `SaveFileDialog` for file selection

## Help System (`HelpSearcher.cs`)

Full-text search over embedded HTML documentation:
1. On first search, loads all HTML doc files from the `doc/` application subdirectory
2. Creates an in-memory SQLite database with an FTS5 virtual table
3. Indexes document titles and content
4. `HelpSearchResultsForm` displays ranked results

The help doc content is generated at build time by `ps1/Update-Docs.ps1` from the `doc/` directory and SQLite documentation.

## UI Utilities

- **Ui.cs**: DPI-aware layout helper. Defines "x-width" and "x-height" units scaled relative to font metrics for consistent sizing across display densities.
- **Bind.cs**: Two-way data binding between WinForms controls and `Slot<T>` reactive properties.
- **DataGridViewUtil.cs**: Factory for double-buffered `DataGridView` instances (prevents flicker).
- **MenuRenderer.cs**: Custom `ToolStripRenderer` for consistent menu appearance.
- **UserOptions.cs**: Persisted user preferences (fonts, colors) stored in application settings.
- **Slot<T>**: Reactive property wrapper (defined in SqlNotebookScript.Utils) used for `_isDirty`, `_isTransactionOpen`, and data binding.
- **WaitForm.cs**: Modal progress dialog for async operations with optional cancellation support.

## Key External Dependencies

| Dependency | Purpose |
|------------|---------|
| WeifenLuo.WinFormsUI.Docking | Dockable panel framework (VS2012Light theme) |
| ScintillaNET (fernandreu) | Syntax-highlighted code editor control |
| Windows-API-Code-Pack | Native Windows file/folder dialogs |
| DuckDB.NET, Microsoft.Data.SqlClient, Npgsql, MySql.Data | Database import drivers |
| HtmlAgilityPack | HTML parsing for help documentation |
| Microsoft.Data.Sqlite | SQLite ADO.NET access |

## Key Design Decisions

- **DockPanel over MDI**: Provides flexible, resizable, auto-hideable panels that users can customize, similar to Visual Studio.
- **Page blocks as UI model**: Pages are `FlowLayoutPanel` containers of editable blocks, giving a Jupyter-like notebook experience within WinForms.
- **Custom GDI+ rendering**: Query blocks render results using `OnPaint` with GDI+ for efficient display without embedding heavyweight controls.
- **Scintilla for code editing**: Wraps the mature Scintilla C++ editor for syntax highlighting, using the native SQLite tokenizer for accurate coloring.
- **Event-driven coordination**: `NotebookManager` publishes events that `MainForm`, `ExplorerControl`, and document controls subscribe to, keeping components decoupled.
- **Async with WaitForm**: Long operations (import, save, open) run on background threads with `WaitForm` providing progress feedback and cancellation.
