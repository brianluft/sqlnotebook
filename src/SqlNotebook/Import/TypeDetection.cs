using System;
using System.Collections.Generic;
using System.Globalization;
using SqlNotebookScript.DataTables;

namespace SqlNotebook.Import;

public static class TypeDetection
{
    [Flags]
    private enum TypeFlag
    {
        Text = 1,
        Integer = 2,
        Real = 4,
        Date = 8,
        DateTime = 16,
    }

    public static IReadOnlyList<string> DetectTypes(SimpleDataTable table)
    {
        var types = new TypeFlag[table.Columns.Count];
        for (var i = 0; i < table.Columns.Count; i++)
        {
            types[i] = TypeFlag.Text | TypeFlag.Integer | TypeFlag.Real | TypeFlag.Date | TypeFlag.DateTime;
        }

        foreach (var row in table.Rows)
        {
            for (var i = 0; i < table.Columns.Count; i++)
            {
                var value = Convert.ToString(row[i]);
                if (string.IsNullOrWhiteSpace(value))
                {
                    // ok for all types as a null
                    continue;
                }
                var type = TypeFlag.Text;
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    type |= TypeFlag.Integer;
                }
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    type |= TypeFlag.Real;
                }
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
                {
                    type |= TypeFlag.DateTime;
                    if (dateTime == dateTime.Date)
                    {
                        type |= TypeFlag.Date;
                    }
                }
                types[i] &= type;
            }
        }

        var chosenTypes = new string[table.Columns.Count];
        if (table.Rows.Count == 0)
        {
            // special case: if there are no rows, use TEXT for all.
            for (var i = 0; i < table.Columns.Count; i++)
            {
                chosenTypes[i] = "TEXT";
            }
        }
        else
        {
            for (var i = 0; i < table.Columns.Count; i++)
            {
                var mask = types[i];
                if (mask.HasFlag(TypeFlag.Integer))
                {
                    chosenTypes[i] = "INTEGER";
                }
                else if (mask.HasFlag(TypeFlag.Real))
                {
                    chosenTypes[i] = "REAL";
                }
                else if (mask.HasFlag(TypeFlag.Date))
                {
                    chosenTypes[i] = "DATE";
                }
                else if (mask.HasFlag(TypeFlag.DateTime))
                {
                    chosenTypes[i] = "DATETIME";
                }
                else
                {
                    chosenTypes[i] = "TEXT";
                }
            }
        }

        return chosenTypes;
    }
}
