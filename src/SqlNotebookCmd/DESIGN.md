# SqlNotebookCmd Design

SqlNotebookCmd is a minimal command-line interface for executing scripts stored in SQL Notebook files. It allows automation and batch processing without the GUI.

## Usage

```
SqlNotebookCmd <notebook-file> <script-name>
SqlNotebookCmd --help
```

**Arguments:**
- `<notebook-file>` - Path to a `.sqlnb` notebook file
- `<script-name>` - Name of a script within the notebook (case-insensitive)

**Exit codes:**
- `0` - Success
- `1` - Error (file not found, script not found, empty script, execution error)

## Architecture

The entire application is a single file (`Program.cs`). It is a thin wrapper around SqlNotebookScript:

```
Command-line args
       |
  Notebook.Open(filePath)
       |
  Find script in notebook.UserData.Items
       |
  ScriptParser.Parse(scriptSql)
       |
  ScriptRunner.Execute(parsedScript)
       |
  Format and output ScriptOutput to stdout
```

## Output Format

Results are written to stdout:
- **Scalar result**: Formatted as a single value (doubles with up to 4 decimal places, blobs as hex, DBNull as "null")
- **Text output**: Each `PRINT` line output verbatim
- **Data tables**: CSV format with header row, comma-separated values. Multiple tables separated by blank lines.

Errors are written to stderr.

## Dependencies

The only dependency is a project reference to `SqlNotebookScript`. All database connectivity, parsing, and execution capabilities come from that library.

## Key Design Decisions

- **Single-file simplicity**: No argument parsing library, no dependency injection. The CLI does one thing: run a named script from a notebook file.
- **CSV output**: Results use a simple CSV format for easy piping to other tools.
- **No interactive mode**: The CLI is designed for batch execution, not interactive use. The GUI's console serves that purpose.
