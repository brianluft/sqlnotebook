using System;
using System.Globalization;

namespace SqlNotebookScript.Utils;

public static class DateTimeUtil
{
    public static string FormatDate(DateTime dt) => dt.ToString("yyyy-MM-dd");

    public static string FormatDateTime(DateTime dt) => dt.ToString("yyyy-MM-dd HH:mm:ss.fff");

    public static int GetQuarter(DateTime dt) => GetQuarter(dt.Month);

    public static int GetQuarter(int month) =>
        month <= 3 ? 1
        : month <= 6 ? 2
        : month <= 9 ? 3
        : 4;

    public static int GetDatePart(DateTime date, DatePart datePart)
    {
        var cal = CultureInfo.CurrentCulture.Calendar;
        var dtf = CultureInfo.CurrentCulture.DateTimeFormat;
        return datePart switch
        {
            DatePart.Year => cal.GetYear(date),
            DatePart.Quarter => GetQuarter(date),
            DatePart.Month => cal.GetMonth(date),
            DatePart.DayOfYear => cal.GetDayOfYear(date),
            DatePart.Day => cal.GetDayOfMonth(date),
            DatePart.Week => cal.GetWeekOfYear(date, dtf.CalendarWeekRule, dtf.FirstDayOfWeek),
            DatePart.DayOfWeek => DayOfWeekToInt(cal.GetDayOfWeek(date)),
            DatePart.Hour => cal.GetHour(date),
            DatePart.Minute => cal.GetMinute(date),
            DatePart.Second => cal.GetSecond(date),
            DatePart.Millisecond => (int)cal.GetMilliseconds(date),
            _ => throw new Exception("Internal error: unrecognized DatePart."),
        };
    }

    public static int DayOfWeekToInt(DayOfWeek dow)
    {
        switch (dow)
        {
            case DayOfWeek.Monday:
                return 1;
            case DayOfWeek.Tuesday:
                return 2;
            case DayOfWeek.Wednesday:
                return 3;
            case DayOfWeek.Thursday:
                return 4;
            case DayOfWeek.Friday:
                return 5;
            case DayOfWeek.Saturday:
                return 6;
            case DayOfWeek.Sunday:
                return 7;
            default:
                throw new Exception("Internal error: unrecognized DayOfWeek.");
        }
    }

    public static DayOfWeek IntToDayOfWeek(int dow)
    {
        switch (dow)
        {
            case 1:
                return DayOfWeek.Monday;
            case 2:
                return DayOfWeek.Tuesday;
            case 3:
                return DayOfWeek.Wednesday;
            case 4:
                return DayOfWeek.Thursday;
            case 5:
                return DayOfWeek.Friday;
            case 6:
                return DayOfWeek.Saturday;
            case 7:
                return DayOfWeek.Sunday;
            default:
                throw new Exception("Internal error: unrecognized day of week number.");
        }
    }

    public static DateTime TruncateDate(DatePart datePart, DateTime date)
    {
        switch (datePart)
        {
            case DatePart.Year:
                return new(date.Year, 1, 1, 0, 0, 0);
            case DatePart.Quarter:
                return new(date.Year, (GetQuarter(date) - 1) * 3 + 1, 1, 0, 0, 0);
            case DatePart.Month:
                return new(date.Year, date.Month, 1, 0, 0, 0);
            case DatePart.DayOfYear:
            case DatePart.Day:
            case DatePart.DayOfWeek:
                return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
            case DatePart.Week:
                while (date.DayOfWeek != CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek)
                {
                    date = date.AddDays(-1);
                }
                return new(date.Year, date.Month, date.Day, 0, 0, 0);
            case DatePart.Hour:
                return new(date.Year, date.Month, date.Day, date.Hour, 0, 0);
            case DatePart.Minute:
                return new(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0);
            case DatePart.Second:
                return new(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second);
            default:
                return date;
        }
    }
}
