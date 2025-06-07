using System;
using System.Collections.Generic;
using System.Linq;
using SqlNotebookScript.Core;
using SqlNotebookScript.Utils;

namespace SqlNotebookScript.Interpreter;

public sealed class ScriptRunner
{
    private readonly Notebook _notebook;
    private readonly IReadOnlyDictionary<Type, Action<Ast.Stmt, ScriptEnv>> _stmtRunners;
    private readonly IReadOnlyDictionary<string, string> _scripts; // lowercase script name -> script code

    public ScriptRunner(Notebook notebook, IReadOnlyDictionary<string, string> scripts)
    {
        _notebook = notebook;
        _scripts = scripts;
        _stmtRunners = new Dictionary<Type, Action<Ast.Stmt, ScriptEnv>>
        {
            [typeof(Ast.SqlStmt)] = (s, e) => ExecuteSqlStmt((Ast.SqlStmt)s, e),
            [typeof(Ast.DeclareStmt)] = (s, e) => ExecuteDeclareStmt((Ast.DeclareStmt)s, e),
            [typeof(Ast.SetStmt)] = (s, e) => ExecuteSetStmt((Ast.SetStmt)s, e),
            [typeof(Ast.IfStmt)] = (s, e) => ExecuteIfStmt((Ast.IfStmt)s, e),
            [typeof(Ast.WhileStmt)] = (s, e) => ExecuteWhileStmt((Ast.WhileStmt)s, e),
            [typeof(Ast.ForStmt)] = (s, e) => ExecuteForStmt((Ast.ForStmt)s, e),
            [typeof(Ast.ForeachStmt)] = (s, e) => ExecuteForeachStmt((Ast.ForeachStmt)s, e),
            [typeof(Ast.BlockStmt)] = (s, e) => ExecuteBlockStmt((Ast.BlockStmt)s, e),
            [typeof(Ast.BreakStmt)] = (s, e) => ExecuteBreakStmt((Ast.BreakStmt)s, e),
            [typeof(Ast.ContinueStmt)] = (s, e) => ExecuteContinueStmt((Ast.ContinueStmt)s, e),
            [typeof(Ast.PrintStmt)] = (s, e) => ExecutePrintStmt((Ast.PrintStmt)s, e),
            [typeof(Ast.ExecuteStmt)] = (s, e) => ExecuteExecuteStmt((Ast.ExecuteStmt)s, e),
            [typeof(Ast.ReturnStmt)] = (s, e) => ExecuteReturnStmt((Ast.ReturnStmt)s, e),
            [typeof(Ast.ThrowStmt)] = (s, e) => ExecuteThrowStmt((Ast.ThrowStmt)s, e),
            [typeof(Ast.SaveStmt)] = (s, e) => ExecuteSaveStmt((Ast.SaveStmt)s, e),
            [typeof(Ast.RethrowStmt)] = (s, e) => ExecuteRethrowStmt((Ast.RethrowStmt)s, e),
            [typeof(Ast.TryCatchStmt)] = (s, e) => ExecuteTryCatchStmt((Ast.TryCatchStmt)s, e),
            [typeof(Ast.ImportCsvStmt)] = (s, e) => ExecuteImportCsvStmt((Ast.ImportCsvStmt)s, e),
            [typeof(Ast.ImportTxtStmt)] = (s, e) => ExecuteImportTxtStmt((Ast.ImportTxtStmt)s, e),
            [typeof(Ast.ExportTxtStmt)] = (s, e) => ExecuteExportTxtStmt((Ast.ExportTxtStmt)s, e),
            [typeof(Ast.ImportXlsStmt)] = (s, e) => ExecuteImportXlsStmt((Ast.ImportXlsStmt)s, e),
            [typeof(Ast.ImportDatabaseStmt)] = (s, e) => ExecuteImportDatabaseStmt((Ast.ImportDatabaseStmt)s, e),
            [typeof(Ast.ExportCsvStmt)] = (s, e) => ExecuteExportCsvStmt((Ast.ExportCsvStmt)s, e),
            [typeof(Ast.DropScriptStmt)] = (s, e) => ExecuteDropScriptStmt((Ast.DropScriptStmt)s, e),
            [typeof(Ast.DropPageStmt)] = (s, e) => ExecuteDropPageStmt((Ast.DropPageStmt)s, e),
            [typeof(Ast.CreateScriptStmt)] = (s, e) => ExecuteCreateScriptStmt((Ast.CreateScriptStmt)s, e),
        };
    }

    private string GetScriptCode(string name)
    {
        string code;
        if (_scripts.TryGetValue(name.ToLower(), out code))
        {
            return code ?? "";
        }
        else
        {
            // Check if the script was created during execution and is in UserData.Items
            var scriptRecord =
                _notebook.UserData.Items.FirstOrDefault(x =>
                    x is ScriptNotebookItemRecord && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)
                ) as ScriptNotebookItemRecord;

            if (scriptRecord != null)
            {
                return scriptRecord.Sql ?? "";
            }
            else
            {
                throw new ScriptException($"There is no script named \"{name}\".");
            }
        }
    }

    // must be run from the SQLite thread
    public ScriptOutput Execute(Ast.Script script, IReadOnlyDictionary<string, object> args)
    {
        return Execute(script, new ScriptEnv(), args);
    }

    public ScriptOutput Execute(Ast.Script script, ScriptEnv env, IReadOnlyDictionary<string, object> args)
    {
        foreach (var arg in args)
        {
            var lowercaseKey = arg.Key.ToLower();
            env.Vars[lowercaseKey] = arg.Value;
        }
        Execute(script, env);
        return env.Output;
    }

    public void Execute(Ast.Script script, ScriptEnv env)
    {
        try
        {
            ExecuteBlock(script.Block, env);

            if (env.DidBreak)
            {
                throw new ScriptException($"Attempted to BREAK outside of a WHILE loop.");
            }
            else if (env.DidContinue)
            {
                throw new ScriptException($"Attempted to CONTINUE outside of a WHILE loop.");
            }
        }
        catch (Exception ex)
        {
            var snippet = ex is SqliteException s ? s.Snippet : null;
            throw new UncaughtErrorScriptException(ex.GetExceptionMessage(), ex) { Snippet = snippet };
        }
    }

    private void ExecuteBlock(Ast.Block block, ScriptEnv env)
    {
        foreach (var stmt in block.Statements)
        {
            ExecuteStmt(stmt, env);
            if (env.DidReturn || env.DidBreak || env.DidContinue)
            {
                return;
            }
        }
    }

    private void ExecuteStmt(Ast.Stmt stmt, ScriptEnv env)
    {
        var runner = _stmtRunners[stmt.GetType()];
        runner(stmt, env);
    }

    private void ExecuteSqlStmt(Ast.SqlStmt stmt, ScriptEnv env)
    {
        foreach (var beforeStmt in stmt.RunBefore)
        {
            ExecuteStmt(beforeStmt, env);
        }

        try
        {
            var dt = _notebook.Query(stmt.Sql, env.Vars, env.OnRow);
            if (dt.Columns.Any())
            {
                env.Output.DataTables.Add(dt);
            }
            else
            {
                dt.Dispose();
            }
        }
        finally
        {
            foreach (var afterStmt in stmt.RunAfter)
            {
                ExecuteStmt(afterStmt, env);
            }
        }
    }

    private void ExecuteDeclareStmt(Ast.DeclareStmt stmt, ScriptEnv env)
    {
        var name = stmt.VariableName.ToLower();
        var declarationExists = env.Vars.ContainsKey(name);
        if (stmt.IsParameter)
        {
            if (env.ParNames.Contains(name))
            {
                throw new ScriptException($"Duplicate DECLARE for parameter \"{stmt.VariableName}\".");
            }
            else
            {
                env.ParNames.Add(name);
            }

            if (declarationExists)
            {
                // do nothing; the parameter value was specified by the caller
            }
            else if (stmt.InitialValue != null)
            {
                // the caller did not specify a value, but there is an initial value in the DECLARE statement.
                env.Vars[name] = EvaluateExpr(stmt.InitialValue, env);
            }
            else
            {
                throw new ScriptException($"An argument value was not provided for parameter \"{stmt.VariableName}\".");
            }
        }
        else if (declarationExists)
        {
            throw new ScriptException($"Duplicate DECLARE for variable \"{stmt.VariableName}\".");
        }
        else
        {
            if (stmt.InitialValue != null)
            {
                env.Vars[name] = EvaluateExpr(stmt.InitialValue, env);
            }
            else
            {
                env.Vars[name] = DBNull.Value;
            }
        }
    }

    private void ExecuteSetStmt(Ast.SetStmt stmt, ScriptEnv env)
    {
        var name = stmt.VariableName.ToLower();
        if (env.Vars.ContainsKey(name))
        {
            env.Vars[name] = EvaluateExpr(stmt.InitialValue, env);
        }
        else
        {
            throw new ScriptException($"Attempted to SET the undeclared variable \"{stmt.VariableName}\".");
        }
    }

    private void ExecuteIfStmt(Ast.IfStmt stmt, ScriptEnv env)
    {
        var condition = EvaluateExpr<long>(stmt.Condition, env);
        if (condition == 0)
        {
            if (stmt.ElseBlock != null)
            {
                ExecuteBlock(stmt.ElseBlock, env);
            }
        }
        else if (condition == 1)
        {
            ExecuteBlock(stmt.Block, env);
        }
        else
        {
            throw new ScriptException(
                $"Evaluation of IF condition expression \"{stmt.Condition.Sql}\" "
                    + $"produced a value of {condition} instead of the expected 0 or 1."
            );
        }
    }

    private void ExecuteWhileStmt(Ast.WhileStmt stmt, ScriptEnv env)
    {
        long condition;
        do
        {
            condition = EvaluateExpr<long>(stmt.Condition, env);
            if (condition == 1)
            {
                ExecuteBlock(stmt.Block, env);
                if (env.DidReturn)
                {
                    return;
                }
                else if (env.DidBreak)
                {
                    env.DidBreak = false;
                    break;
                }
                else if (env.DidContinue)
                {
                    env.DidContinue = false;
                }
            }
            else if (condition != 0)
            {
                throw new ScriptException(
                    $"Evaluation of WHILE condition expression \"{stmt.Condition.Sql}\" "
                        + $"produced a value of {condition} instead of the expected 0 or 1."
                );
            }
        } while (condition == 1);
    }

    private void ExecuteForStmt(Ast.ForStmt stmt, ScriptEnv env)
    {
        var firstNumber = EvaluateExpr<long>(stmt.FirstNumberExpr, env);
        var lastNumber = EvaluateExpr<long>(stmt.LastNumberExpr, env);

        long step = firstNumber <= lastNumber ? 1 : -1;
        if (stmt.StepExpr != null)
        {
            step = EvaluateExpr<long>(stmt.StepExpr, env);
        }

        if (step == 0)
        {
            throw new ScriptException("The STEP value in a FOR statement must not be zero.");
        }
        else if (step < 0 && firstNumber < lastNumber)
        {
            throw new ScriptException(
                "The STEP value in a FOR statement must be positive if \"first-number\" < \"last-number\"."
            );
        }
        else if (step > 0 && firstNumber > lastNumber)
        {
            throw new ScriptException(
                "The STEP value in a FOR statement must be negative if \"first-number\" > \"last-number\"."
            );
        }

        var upward = step > 0;
        long counter = firstNumber;

        while ((upward && counter <= lastNumber) || (!upward && counter >= lastNumber))
        {
            env.Vars[stmt.VariableName] = counter;

            ExecuteBlock(stmt.Block, env);
            if (env.DidReturn)
            {
                return;
            }
            else if (env.DidBreak)
            {
                env.DidBreak = false;
                break;
            }
            else if (env.DidContinue)
            {
                env.DidContinue = false;
            }

            counter += step;
        }
    }

    private void ExecuteForeachStmt(Ast.ForeachStmt stmt, ScriptEnv env)
    {
        // Evaluate the table expression to get table name
        var tableName = EvaluateIdentifierOrExpr(stmt.TableExpr, env);

        // Query the table to get all rows and column names
        var query = $"SELECT * FROM {tableName.DoubleQuote()}";
        using var dt = _notebook.Query(query, env.Vars);

        var columnCount = dt.Columns.Count;
        var variableCount = stmt.VariableNames.Count;

        // For each row in the table
        foreach (var row in dt.Rows)
        {
            // Set each variable to the corresponding column value
            for (int i = 0; i < variableCount; i++)
            {
                var variableName = stmt.VariableNames[i].ToLower();
                if (i < columnCount)
                {
                    // Set to the column value
                    env.Vars[variableName] = row[i];
                }
                else
                {
                    // More variables than columns, set extra variables to NULL
                    env.Vars[variableName] = DBNull.Value;
                }
            }

            // Execute the block
            ExecuteBlock(stmt.Block, env);

            // Handle control flow like other loops
            if (env.DidReturn)
            {
                return;
            }
            else if (env.DidBreak)
            {
                env.DidBreak = false;
                break;
            }
            else if (env.DidContinue)
            {
                env.DidContinue = false;
            }
        }
    }

    private void ExecuteBlockStmt(Ast.BlockStmt stmt, ScriptEnv env)
    {
        ExecuteBlock(stmt.Block, env);
    }

    private void ExecuteBreakStmt(Ast.BreakStmt stmt, ScriptEnv env)
    {
        env.DidBreak = true;
    }

    private void ExecuteContinueStmt(Ast.ContinueStmt stmt, ScriptEnv env)
    {
        env.DidContinue = true;
    }

    private void ExecutePrintStmt(Ast.PrintStmt stmt, ScriptEnv env)
    {
        var value = EvaluateExpr(stmt.Value, env);

        if (value is byte[] byteArray)
        {
            env.Output.TextOutput.Add(BlobUtil.ToString(byteArray));
            return;
        }

        env.Output.TextOutput.Add(value.ToString());
    }

    public ScriptOutput ExecuteSubScript(string scriptName, List<Ast.ArgumentPair> arguments, ScriptEnv env)
    {
        var parser = new ScriptParser(_notebook);
        var runner = new ScriptRunner(_notebook, _scripts);
        var script = parser.Parse(GetScriptCode(scriptName));
        var subEnv = new ScriptEnv { OnRow = env.OnRow };
        foreach (var arg in arguments)
        {
            if (arg.Value != null)
            {
                subEnv.Vars[arg.Name.ToLower()] = EvaluateExpr(arg.Value, env);
            }
        }
        runner.Execute(script, subEnv);
        return subEnv.Output;
    }

    private void ExecuteExecuteStmt(Ast.ExecuteStmt stmt, ScriptEnv env)
    {
        var subOutput = ExecuteSubScript(stmt.ScriptName, stmt.Arguments, env);
        env.Output.Append(subOutput);
        var returnCode = subOutput.ScalarResult;
        if (stmt.ReturnVariableName != null)
        {
            var name = stmt.ReturnVariableName.ToLower();
            env.Vars[name] = returnCode ?? DBNull.Value;
        }
    }

    private void ExecuteReturnStmt(Ast.ReturnStmt stmt, ScriptEnv env)
    {
        if (stmt.Value != null)
        {
            env.Output.ScalarResult = EvaluateExpr(stmt.Value, env);
        }
        env.DidReturn = true;
    }

    private void ExecuteThrowStmt(Ast.ThrowStmt stmt, ScriptEnv env)
    {
        if (stmt.HasErrorValues)
        {
            var errorMessage = EvaluateExpr(stmt.Message, env);
            Throw(errorMessage.ToString());
        }
        else
        {
            Throw(Notebook.ErrorMessage);
        }
    }

    private void ExecuteSaveStmt(Ast.SaveStmt stmt, ScriptEnv env)
    {
        try
        {
            if (stmt.FilenameExpr != null)
            {
                // Save with specified filename
                var filename = EvaluateIdentifierOrExpr(stmt.FilenameExpr, env);
                _notebook.SaveAs(filename);
            }
            else
            {
                // Save to existing filename
                if (_notebook.OriginalFilePath == null)
                {
                    throw new Exception("No filename was specified and the notebook is untitled.");
                }
                _notebook.Save();
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"SAVE: {ex.GetExceptionMessage()}", ex);
        }
    }

    private static void Throw(string errorMessage)
    {
        throw new ScriptException(errorMessage);
    }

    private void ExecuteRethrowStmt(Ast.RethrowStmt stmt, ScriptEnv env)
    {
        Throw(Notebook.ErrorMessage);
    }

    private void ExecuteTryCatchStmt(Ast.TryCatchStmt stmt, ScriptEnv env)
    {
        try
        {
            ExecuteBlock(stmt.TryBlock, env);
        }
        catch (Exception ex)
        {
            Notebook.ErrorMessage = ex.GetExceptionMessage();
            ExecuteBlock(stmt.CatchBlock, env);
        }
    }

    private void ExecuteImportCsvStmt(Ast.ImportCsvStmt stmt, ScriptEnv env)
    {
        ImportCsvStmtRunner.Run(_notebook, env, this, stmt);
    }

    private void ExecuteImportXlsStmt(Ast.ImportXlsStmt stmt, ScriptEnv env)
    {
        ImportXlsStmtRunner.Run(_notebook, env, this, stmt);
    }

    private void ExecuteImportTxtStmt(Ast.ImportTxtStmt stmt, ScriptEnv env)
    {
        ImportTxtStmtRunner.Run(_notebook, env, this, stmt);
    }

    private void ExecuteExportTxtStmt(Ast.ExportTxtStmt stmt, ScriptEnv env)
    {
        ExportTxtStmtRunner.Run(_notebook, env, this, stmt);
    }

    private void ExecuteImportDatabaseStmt(Ast.ImportDatabaseStmt stmt, ScriptEnv env)
    {
        Notebook.WithCancellationToken(cancel => ImportDatabaseStmtRunner.Run(_notebook, env, this, stmt, cancel));
    }

    private void ExecuteExportCsvStmt(Ast.ExportCsvStmt stmt, ScriptEnv env)
    {
        Notebook.WithCancellationToken(cancel => ExportCsvStmtRunner.Run(_notebook, env, this, stmt, cancel));
    }

    private void ExecuteDropScriptStmt(Ast.DropScriptStmt stmt, ScriptEnv env)
    {
        var scriptName = EvaluateIdentifierOrExpr(stmt.ScriptName, env);

        // Find the script in the user data
        var itemRecord = _notebook.UserData.Items.FirstOrDefault(x =>
            x is ScriptNotebookItemRecord && string.Equals(x.Name, scriptName, StringComparison.OrdinalIgnoreCase)
        );

        if (itemRecord == null)
        {
            throw new ScriptException($"There is no script named \"{scriptName}\".");
        }

        // Remove from user data
        _notebook.UserData.Items.Remove(itemRecord);
        if (itemRecord is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void ExecuteDropPageStmt(Ast.DropPageStmt stmt, ScriptEnv env)
    {
        var pageName = EvaluateIdentifierOrExpr(stmt.PageName, env);

        // Find the page in the user data
        var itemRecord = _notebook.UserData.Items.FirstOrDefault(x =>
            x is PageNotebookItemRecord && string.Equals(x.Name, pageName, StringComparison.OrdinalIgnoreCase)
        );

        if (itemRecord == null)
        {
            throw new ScriptException($"There is no page named \"{pageName}\".");
        }

        // Remove from user data
        _notebook.UserData.Items.Remove(itemRecord);
        if (itemRecord is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void ExecuteCreateScriptStmt(Ast.CreateScriptStmt stmt, ScriptEnv env)
    {
        var scriptName = EvaluateIdentifierOrExpr(stmt.ScriptName, env);
        var sqlCommands = EvaluateIdentifierOrExpr(stmt.SqlCommands, env);

        // Check if script already exists (case insensitive)
        var existingScript = _notebook.UserData.Items.FirstOrDefault(x =>
            x is ScriptNotebookItemRecord && string.Equals(x.Name, scriptName, StringComparison.OrdinalIgnoreCase)
        );

        if (existingScript != null)
        {
            throw new ScriptException($"A script named \"{scriptName}\" already exists.");
        }

        // Create the new script
        var newScript = new ScriptNotebookItemRecord
        {
            Name = scriptName,
            Sql = sqlCommands,
            Parameters = new(),
        };

        // Add to user data
        _notebook.UserData.Items.Add(newScript);
    }

    public T EvaluateExpr<T>(Ast.Expr expr, ScriptEnv env)
    {
        var value = EvaluateExpr(expr, env);
        if (typeof(T).IsAssignableFrom(value.GetType()))
        {
            return (T)value;
        }
        else
        {
            throw new ScriptException(
                $"Evaluation of expression \"{expr.Sql}\" produced a value of type "
                    + $"\"{value.GetType().Name}\" instead of the expected \"{typeof(T).Name}\"."
            );
        }
    }

    public object EvaluateExpr(Ast.Expr expr, ScriptEnv env)
    {
        using var dt = _notebook.Query($"SELECT ({expr.Sql})", env.Vars);
        if (dt.Columns.Count == 1 && dt.Rows.Count == 1)
        {
            return dt.Rows[0][0];
        }
        else
        {
            throw new ScriptException($"Evaluation of expression \"{expr.Sql}\" did not produce a value.");
        }
    }

    public string EvaluateIdentifierOrExpr(Ast.IdentifierOrExpr idOrExpr, ScriptEnv env)
    {
        if (idOrExpr.Expr != null)
        {
            return EvaluateExpr<string>(idOrExpr.Expr, env);
        }
        else
        {
            return idOrExpr.Identifier;
        }
    }
}
