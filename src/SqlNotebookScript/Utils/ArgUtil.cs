using System;

namespace SqlNotebookScript.Utils;

public enum DatePart
{
    Year,
    Quarter,
    Month,
    DayOfYear,
    Day,
    Week,
    DayOfWeek,
    Hour,
    Minute,
    Second,
    Millisecond,
}

public static class ArgUtil
{
    public static long GetIntArg(object arg, string paramName, string functionName)
    {
        if (arg == null || arg is DBNull)
        {
            throw new Exception($"{functionName.ToUpper()}: The \"{paramName}\" argument is required.");
        }
        else if (arg is int || arg is long)
        {
            return Convert.ToInt64(arg);
        }
        else
        {
            throw new Exception(
                $"{functionName.ToUpper()}: The \"{paramName}\" argument must be an INTEGER "
                    + $" value, but type {GetTypeName(arg.GetType())} was provided."
            );
        }
    }

    public static int GetInt32Arg(object arg, string paramName, string functionName)
    {
        if (arg == null || arg is DBNull)
        {
            throw new Exception($"{functionName.ToUpper()}: The \"{paramName}\" argument is required.");
        }
        else if (arg is int || arg is long)
        {
            var value64 = Convert.ToInt64(arg);
            if (value64 < int.MinValue || value64 > int.MaxValue)
            {
                throw new Exception(
                    $"{functionName.ToUpper()}: The \"{paramName}\" argument must be between "
                        + $"{int.MinValue} and {int.MaxValue}."
                );
            }
            else
            {
                return (int)value64;
            }
        }
        else
        {
            throw new Exception(
                $"{functionName.ToUpper()}: The \"{paramName}\" argument must be an INTEGER "
                    + $" value, but type {GetTypeName(arg.GetType())} was provided."
            );
        }
    }

    public static double GetFloatArg(object arg, string paramName, string functionName)
    {
        if (arg == null || arg is DBNull)
        {
            throw new Exception($"{functionName.ToUpper()}: The \"{paramName}\" argument is required.");
        }
        else if (arg is int || arg is long || arg is float || arg is double)
        {
            return Convert.ToDouble(arg);
        }
        else
        {
            throw new Exception(
                $"{functionName.ToUpper()}: The \"{paramName}\" argument must be an INTEGER "
                    + $"or FLOAT value, but type {GetTypeName(arg.GetType())} was provided."
            );
        }
    }

    public static string GetStrArg(object arg, string paramName, string functionName)
    {
        if (arg == null || arg is DBNull)
        {
            throw new Exception($"{functionName.ToUpper()}: The \"{paramName}\" argument is required.");
        }
        else if (arg is string)
        {
            return (string)arg;
        }
        else
        {
            throw new Exception(
                $"{functionName.ToUpper()}: The \"{paramName}\" argument must be a TEXT value, "
                    + $"but type {GetTypeName(arg.GetType())} was provided."
            );
        }
    }

    public static byte[] GetBlobArg(object arg, string paramName, string functionName)
    {
        if (arg == null || arg is DBNull)
        {
            throw new Exception($"{functionName.ToUpper()}: The \"{paramName}\" argument is required.");
        }
        else if (arg is byte[])
        {
            return (byte[])arg;
        }
        else
        {
            throw new Exception(
                $"{functionName.ToUpper()}: The \"{paramName}\" argument must be a BLOB value, "
                    + $"but type {GetTypeName(arg.GetType())} was provided."
            );
        }
    }

    public static DateTime GetDateArg(object arg, string paramName, string functionName)
    {
        var text = GetStrArg(arg, paramName, functionName);

        // Bug fix:
        // This previously used DateTime.TryParse().
        //
        // If we use DateTime.TryParse() and the text happens to have a DateTimeOffset-style UTC offset, then the date
        // will actually get _converted_ to the computer's local time zone! Whoops! If we parse as a DateTimeOffset
        // first and then use .DateTime, we'll effectively strip off the UTC offset without changing the date or
        // time.

        if (DateTimeOffset.TryParse(text, out var date))
        {
            return date.DateTime;
        }
        else
        {
            throw new Exception($"{functionName.ToUpper()}: The \"{paramName}\" argument must be a valid date string.");
        }
    }

    public static DatePart GetDatePartArg(object arg, string paramName, string functionName)
    {
        var text = GetStrArg(arg, paramName, functionName);
        switch (text.ToLower())
        {
            case "year":
            case "yyyy":
            case "yy":
                return DatePart.Year;
            case "quarter":
            case "qq":
            case "q":
                return DatePart.Quarter;
            case "month":
            case "mm":
            case "m":
                return DatePart.Month;
            case "dayofyear":
            case "dy":
            case "y":
                return DatePart.DayOfYear;
            case "day":
            case "dd":
            case "d":
                return DatePart.Day;
            case "week":
            case "wk":
            case "ww":
                return DatePart.Week;
            case "weekday":
            case "dw":
                return DatePart.DayOfWeek;
            case "hour":
            case "hh":
                return DatePart.Hour;
            case "minute":
            case "mi":
            case "n":
                return DatePart.Minute;
            case "second":
            case "ss":
            case "s":
                return DatePart.Second;
            case "millisecond":
            case "ms":
                return DatePart.Millisecond;
            default:
                throw new Exception(
                    $"{functionName.ToUpper()}: The \"{paramName}\" argument must be one of the recognized "
                        + "date part strings, such as 'year' or 'day'."
                );
        }
    }

    public static string GetTypeName(Type type)
    {
        if (type == typeof(int) || type == typeof(long))
        {
            return "INTEGER";
        }
        else if (type == typeof(float) || type == typeof(double))
        {
            return "FLOAT";
        }
        else if (type == typeof(string))
        {
            return "TEXT";
        }
        else if (type == typeof(DBNull))
        {
            return "NULL";
        }
        else if (type == typeof(byte[]))
        {
            return "BLOB";
        }
        else
        {
            return $"UNKNOWN ({type.Name})";
        }
    }
}
