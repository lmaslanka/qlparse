namespace QlParse;

// Postgres-only words used by workstream C (query expressions and DML). They are matched
// contextually against Identifier tokens and are never reserved by Keyword.Classify.
internal static partial class Keyword
{
    internal const string Conflict = "conflict";
    internal const string Nothing = "nothing";
    internal const string Materialized = "materialized";
    internal const string Copy = "copy";
    internal const string Stdin = "stdin";
    internal const string Stdout = "stdout";
    internal const string Program = "program";
    internal const string Csv = "csv";
    internal const string Header = "header";
    internal const string Delimiter = "delimiter";
    internal const string Quote = "quote";
    internal const string Force = "force";
    internal const string Oids = "oids";
    internal const string Freeze = "freeze";
    internal const string Nowait = "nowait";
    internal const string Locked = "locked";
    internal const string Temp = "temp";
    internal const string Unlogged = "unlogged";
}
