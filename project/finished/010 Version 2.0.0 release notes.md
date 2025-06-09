### 🔄 FOREACH Loop Statement
Iterate through table rows with automatic variable assignment:
```sql
FOREACH (@id, @name, @email) IN customers 
BEGIN
    PRINT CONCAT('Customer: ', @name, ' (', @email, ')');
END
```
- Variables are automatically declared and assigned values from each row
- Supports BREAK and CONTINUE statements like other loops
- Works with tables, views, and subqueries

### 💻 Command-Line Interface
Execute SQL Notebook scripts from the command line without opening the GUI:
```bash
SqlNotebookCmd "MyNotebook.sqlnb" "MyScript"
```
- Perfect for automation, batch processing, and CI/CD pipelines
- Outputs results in CSV format
- Returns appropriate exit codes for success/failure

### 📝 Dynamic Script Management
Create and delete scripts programmatically:
```sql
-- Create a new script
CREATE SCRIPT DataCleanup AS '
    DELETE FROM temp_table WHERE processed = 1;
    PRINT ''Cleanup completed'';
';

-- Execute the script
EXECUTE DataCleanup;

-- Remove the script when no longer needed
DROP SCRIPT DataCleanup;
```
- `CREATE SCRIPT` to add new scripts dynamically
- `DROP SCRIPT` and `DROP PAGE` to remove scripts and pages
- Useful for generating reports, temporary procedures, and workflow automation

### 💾 Save Command
Save your notebook programmatically from within scripts:
```sql
-- Save to current file
SAVE;

-- Save to a specific file
SAVE 'MyBackup.sqlnb';
```
- Particularly useful in SqlNotebookCmd for persisting changes
- Must be used outside of transactions (use "None (auto-commit)" mode in GUI)

### 🦆 DuckDB Integration
Import data from DuckDB files with full UI support:
- Drag and drop `.duckdb` files directly into SQL Notebook
- Available in Import menu → "From file..."
- Support for both copying data and live database connections
```sql
-- Copy data from DuckDB
IMPORT DATABASE 'duckdb'
CONNECTION 'Data Source=C:\data\analytics.duckdb'
TABLE sales_data;

-- Create live connection
IMPORT DATABASE 'duckdb'
CONNECTION 'Data Source=C:\data\analytics.duckdb'
TABLE sales_data
OPTIONS (LINK: 1);
```

### 🗄️ Enhanced SQLite Support
Expanded SQLite file compatibility:
- Open SQLite databases (`.db`, `.sqlite`, `.sqlite3`) directly via File → Open
- Import SQLite tables using the same interface as other databases
- Seamless integration with your existing SQLite workflows
