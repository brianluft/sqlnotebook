using System;
using System.Collections.Generic;
using SqlNotebookScript.Core;

namespace SqlNotebookScript.Interpreter;

public sealed class MacroProcessorException : Exception
{
    public MacroProcessorException(string message)
        : base(message) { }
}

public sealed class MacroProcessor
{
    private readonly Notebook _notebook;
    private readonly List<CustomMacro> _macros = new List<CustomMacro>();

    public MacroProcessor(Notebook notebook)
    {
        _notebook = notebook;
        LoadMacros();
    }

    public void PreprocessStmt(Ast.SqlStmt input)
    {
        foreach (var macro in _macros)
        {
            while (macro.Apply(input)) { }
        }
    }

    private void LoadMacros()
    {
        var types = typeof(CustomMacro).Assembly.GetExportedTypes();
        foreach (var type in types)
        {
            if (type.IsAbstract)
            {
                continue;
            }
            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType == typeof(CustomMacro))
                {
                    var macro = (CustomMacro)Activator.CreateInstance(type);
                    macro.Notebook = _notebook;
                    _macros.Add(macro);
                    break;
                }
                baseType = baseType.BaseType;
            }
        }
    }
}
