using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using SqlNotebookScript.Core;
using SqlNotebookScript.Utils;

namespace SqlNotebookScript.Interpreter;

public sealed class ImportCsvStmtRunner
{
    private readonly Notebook _notebook;
    private readonly ScriptEnv _env;
    private readonly ScriptRunner _runner;
    private readonly Ast.ImportCsvStmt _stmt;

    // option values
    private readonly long _skipLines = 0;
    private readonly long? _takeLines = null;
    private readonly bool _headerRow = true;
    private readonly bool _truncateExistingTable = false;
    private readonly bool _temporaryTable = false;
    private readonly char _separator = ',';
    private readonly Encoding _fileEncoding = null; // or null for automatic
    private readonly IfConversionFails _ifConversionFails = IfConversionFails.ImportAsText;
    private readonly BlankValuesOption _blankValuesMethod = BlankValuesOption.Null;

    // must be run from the SQLite thread
    public static void Run(Notebook notebook, ScriptEnv env, ScriptRunner runner, Ast.ImportCsvStmt stmt)
    {
        var importer = new ImportCsvStmtRunner(notebook, env, runner, stmt);
        SqlUtil.WithTransaction(notebook, importer.Import);
    }

    private ImportCsvStmtRunner(Notebook notebook, ScriptEnv env, ScriptRunner runner, Ast.ImportCsvStmt stmt)
    {
        _notebook = notebook;
        _env = env;
        _runner = runner;
        _stmt = stmt;

        foreach (var option in _stmt.OptionsList.GetOptionKeys())
        {
            switch (option)
            {
                case "SKIP_LINES":
                    _skipLines = _stmt.OptionsList.GetOptionLong(option, _runner, _env, 0, minValue: 0);
                    break;

                case "TAKE_LINES":
                    _takeLines = _stmt.OptionsList.GetOptionLong(option, _runner, _env, -1, minValue: -1);
                    if (_takeLines == -1)
                    {
                        _takeLines = null;
                    }
                    break;

                case "HEADER_ROW":
                    _headerRow = _stmt.OptionsList.GetOptionBool(option, _runner, _env, true);
                    break;

                case "SEPARATOR":
                {
                    var separator = _stmt.OptionsList.GetOption(option, _runner, _env, ",");
                    if (separator.Length != 1)
                    {
                        throw new Exception("IMPORT CSV: The separator must be a single character.");
                    }
                    _separator = separator[0];
                    break;
                }

                case "TRUNCATE_EXISTING_TABLE":
                    _truncateExistingTable = _stmt.OptionsList.GetOptionBool(option, _runner, _env, false);
                    break;

                case "TEMPORARY_TABLE":
                    _temporaryTable = _stmt.OptionsList.GetOptionBool(option, _runner, _env, false);
                    break;

                case "FILE_ENCODING":
                    _fileEncoding = _stmt.OptionsList.GetOptionEncoding(option, _runner, _env);
                    break;

                case "IF_CONVERSION_FAILS":
                    _ifConversionFails = (IfConversionFails)
                        _stmt.OptionsList.GetOptionLong(option, _runner, _env, 1, minValue: 1, maxValue: 3);
                    break;

                case "BLANK_VALUES":
                    _blankValuesMethod = (BlankValuesOption)
                        _stmt.OptionsList.GetOptionLong(option, _runner, _env, 2, minValue: 1, maxValue: 3);
                    break;

                default:
                    throw new Exception($"\"{option}\" is not a recognized option name.");
            }
        }
    }

    private void Import()
    {
        var filePath = GetFilePath();
        using var stream = File.OpenRead(filePath);
        using var bufferedStream = new BufferedStream(stream);
        using var parser = NewParser(bufferedStream, _separator.ToString());
        var parserBuffer = new TextFieldParserBuffer(parser);

        // skip the specified number of initial file lines
        for (int i = 0; i < _skipLines && !parserBuffer.EndOfData; i++)
        {
            parserBuffer.SkipLine();
        }

        SqlUtil.Import(
            ReadColumnNames(parserBuffer),
            GetRows(parserBuffer),
            _stmt.ImportTable,
            _temporaryTable,
            _truncateExistingTable,
            false, // stopAtFirstBlankRow
            _ifConversionFails,
            _blankValuesMethod,
            _notebook,
            _runner,
            _env
        );
    }

    private IEnumerable<object[]> GetRows(TextFieldParserBuffer parser)
    {
        for (int i = 0; (!_takeLines.HasValue || i < _takeLines.Value) && !parser.EndOfData; i++)
        {
            yield return parser.ReadFields();
        }
    }

    private List<string> ReadColumnNames(TextFieldParserBuffer parser)
    {
        var srcColNames = new List<string>();

        if (_headerRow)
        {
            // read the column header row
            var cells = parser.ReadFields();
            if (cells == null)
            {
                // end of file; there is nothing here.
                if (_skipLines == 0)
                {
                    throw new Exception("No column header row was found because the file is empty.");
                }
                else
                {
                    throw new Exception("No column header row was found because all rows were skipped.");
                }
            }

            var columnNumber = 0;
            foreach (var c in cells)
            {
                columnNumber++;
                var cell = c;

                // fill in blank column names
                if (string.IsNullOrWhiteSpace(cell))
                {
                    cell = $"column{columnNumber}";
                }

                // add a numeric suffix to each column name if necessary to make them all unique
                var testName = cell;
                var testNum = 1;
                while (srcColNames.Contains(testName))
                {
                    testNum++;
                    testName = $"{cell}_{testNum}";
                }
                srcColNames.Add(testName);
            }
        }
        else
        {
            // no header row so use "column1", "column2", etc. based on the first row of data
            var fields = parser.PeekFields();
            if (fields == null || !fields.Any())
            {
                // treat empty file as a single column with no rows
                srcColNames.Add("column1");
            }
            else
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    srcColNames.Add($"column{i + 1}");
                }
            }
        }

        return srcColNames;
    }

    private string GetFilePath()
    {
        var filePath = _runner.EvaluateExpr<string>(_stmt.FilenameExpr, _env);
        if (File.Exists(filePath))
        {
            return filePath;
        }
        else
        {
            throw new Exception($"The specified CSV file was not found: \"{filePath}\"");
        }
    }

    private TextFieldParser NewParser(Stream stream, string separator)
    {
        // Detect the presence of a UTF-8 BOM and arrange to handle it if this is an automatic or UTF-8 encoding.
        var isUtf8 = (_fileEncoding?.CodePage ?? 0) switch
        {
            0 => true,
            65001 => true,
            _ => false,
        };

        TextFieldParser textFieldParser =
            isUtf8 ? new(stream, Encoding.UTF8, true)
            : _fileEncoding != null ? new(stream, _fileEncoding, false)
            : new(stream);
        textFieldParser.HasFieldsEnclosedInQuotes = true;
        if (separator == "")
        {
            separator = ",";
        }
        textFieldParser.SetDelimiters(separator);
        return textFieldParser;
    }

    // allow a line to be read, un-read, and then read again
    private sealed class TextFieldParserBuffer
    {
        private readonly Stack<string[]> _unreadStack = new();
        private readonly TextFieldParser _parser;

        public TextFieldParserBuffer(TextFieldParser parser)
        {
            _parser = parser;
        }

        public bool EndOfData => !_unreadStack.Any() && _parser.EndOfData;

        public void SkipLine()
        {
            if (_unreadStack.Any())
            {
                _unreadStack.Pop();
            }
            else if (!_parser.EndOfData)
            {
                _parser.ReadLine();
            }
        }

        public string[] ReadFields()
        { // or null
            if (_unreadStack.Any())
            {
                return _unreadStack.Pop();
            }
            else if (_parser.EndOfData)
            {
                return null;
            }
            else
            {
                return _parser.ReadFields();
            }
        }

        public void UnreadFields(string[] fields)
        {
            if (fields != null)
            {
                _unreadStack.Push(fields);
            }
        }

        public string[] PeekFields()
        { // or null
            var fields = ReadFields();
            UnreadFields(fields);
            return fields;
        }
    }
}
