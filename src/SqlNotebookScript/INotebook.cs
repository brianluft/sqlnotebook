using System.Collections.Generic;

namespace SqlNotebookScript {
    //public interface INotebook {
    //    void Execute(string sql);
    //    void Execute(string sql, IReadOnlyList<object> args);
    //    void Execute(string sql, IReadOnlyDictionary<string, object> args);
    //    SimpleDataTable Query(string sql, int maxRows);
    //    SimpleDataTable Query(string sql, IReadOnlyList<object> args, int maxRows);
    //    SimpleDataTable Query(string sql, IReadOnlyDictionary<string, object> args, int maxRows);
    //    object QueryValue(string sql);
    //    object QueryValue(string sql, IReadOnlyList<object> args);
    //    object QueryValue(string sql, IReadOnlyDictionary<string, object> args);
    //    IReadOnlyList<Token> Tokenize(string input);
    //    NotebookUserData UserData { get; }
    //    bool IsTransactionActive();
    //}

    public sealed class NotebookItemRecord {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
    }

    public sealed class ScriptParameterRecord {
        public string ScriptName { get; set; }
        public List<string> ParamNames { get; set; } = new();
    }

    public sealed class LastErrorRecord {
        public object ErrorNumber { get; set; }
        public object ErrorMessage { get; set; }
        public object ErrorState { get; set; }
    }

    public sealed class NotebookUserData {
        public List<NotebookItemRecord> Items { get; set; } = new();
        public List<ScriptParameterRecord> ScriptParameters { get; set; } = new();
        public LastErrorRecord LastError { get; set; } = new();
    }

    public sealed class Token {
        public TokenType Type;
        public string Text;
        public override string ToString() => $"{Type}: \"{Text}\"";
        public ulong Utf8Start;
        public ulong Utf8Length;
    }

    // This enum is auto-generated by ps1/Update-Deps.ps1. Don't edit between the braces.
    public enum TokenType {
        Semi = 1,
        Explain = 2,
        Query = 3,
        Plan = 4,
        Begin = 5,
        Transaction = 6,
        Deferred = 7,
        Immediate = 8,
        Exclusive = 9,
        Commit = 10,
        End = 11,
        Rollback = 12,
        Savepoint = 13,
        Release = 14,
        To = 15,
        Table = 16,
        Create = 17,
        If = 18,
        Not = 19,
        Exists = 20,
        Temp = 21,
        Lp = 22,
        Rp = 23,
        As = 24,
        Without = 25,
        Comma = 26,
        Abort = 27,
        Action = 28,
        After = 29,
        Analyze = 30,
        Asc = 31,
        Attach = 32,
        Before = 33,
        By = 34,
        Cascade = 35,
        Cast = 36,
        Conflict = 37,
        Database = 38,
        Desc = 39,
        Detach = 40,
        Each = 41,
        Fail = 42,
        Or = 43,
        And = 44,
        Is = 45,
        Match = 46,
        LikeKw = 47,
        Between = 48,
        In = 49,
        Isnull = 50,
        Notnull = 51,
        Ne = 52,
        Eq = 53,
        Gt = 54,
        Le = 55,
        Lt = 56,
        Ge = 57,
        Escape = 58,
        Id = 59,
        Columnkw = 60,
        Do = 61,
        For = 62,
        Ignore = 63,
        Initially = 64,
        Instead = 65,
        No = 66,
        Key = 67,
        Of = 68,
        Offset = 69,
        Pragma = 70,
        Raise = 71,
        Recursive = 72,
        Replace = 73,
        Restrict = 74,
        Row = 75,
        Rows = 76,
        Trigger = 77,
        Vacuum = 78,
        View = 79,
        Virtual = 80,
        With = 81,
        Nulls = 82,
        First = 83,
        Last = 84,
        Current = 85,
        Following = 86,
        Partition = 87,
        Preceding = 88,
        Range = 89,
        Unbounded = 90,
        Exclude = 91,
        Groups = 92,
        Others = 93,
        Ties = 94,
        Generated = 95,
        Always = 96,
        Materialized = 97,
        Reindex = 98,
        Rename = 99,
        CtimeKw = 100,
        Any = 101,
        Bitand = 102,
        Bitor = 103,
        Lshift = 104,
        Rshift = 105,
        Plus = 106,
        Minus = 107,
        Star = 108,
        Slash = 109,
        Rem = 110,
        Concat = 111,
        Collate = 112,
        Bitnot = 113,
        On = 114,
        Indexed = 115,
        String = 116,
        JoinKw = 117,
        Constraint = 118,
        Default = 119,
        Null = 120,
        Primary = 121,
        Unique = 122,
        Check = 123,
        References = 124,
        Autoincr = 125,
        Insert = 126,
        Delete = 127,
        Update = 128,
        Set = 129,
        Deferrable = 130,
        Foreign = 131,
        Drop = 132,
        Union = 133,
        All = 134,
        Except = 135,
        Intersect = 136,
        Select = 137,
        Values = 138,
        Distinct = 139,
        Dot = 140,
        From = 141,
        Join = 142,
        Using = 143,
        Order = 144,
        Group = 145,
        Having = 146,
        Limit = 147,
        Where = 148,
        Returning = 149,
        Into = 150,
        Nothing = 151,
        Float = 152,
        Blob = 153,
        Integer = 154,
        Variable = 155,
        Case = 156,
        When = 157,
        Then = 158,
        Else = 159,
        Index = 160,
        Alter = 161,
        Add = 162,
        Window = 163,
        Over = 164,
        Filter = 165,
        Column = 166,
        AggFunction = 167,
        AggColumn = 168,
        Truefalse = 169,
        Isnot = 170,
        Function = 171,
        Uminus = 172,
        Uplus = 173,
        Truth = 174,
        Register = 175,
        Vector = 176,
        SelectColumn = 177,
        IfNullRow = 178,
        Asterisk = 179,
        Span = 180,
        Error = 181,
        Space = 182,
        Illegal = 183,
    }

    // we want to add an additional token that SQLite doesn't use but we want to avoid changing the enum
    // so just force the value
    public static class EofTokenType {
        public static readonly TokenType Value = TokenType.Illegal + 1;
    }
}
