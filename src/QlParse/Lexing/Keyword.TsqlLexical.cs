using System.Collections.Frozen;

namespace QlParse;

// SQL Server lexical words (workstream A). Pseudo-column names are matched by the lexer after `$`; the
// reserved-word set is only consulted at implicit-alias positions under SqlOptions.SqlServer, so these
// words never become reserved in ISO or Postgres.
internal static partial class Keyword
{
    internal const string TsqlGo = "go";
    internal const string TsqlThrow = "throw";
    internal const string TsqlPartition = "$partition";

    internal static bool IsTsqlPseudoColumn(ReadOnlySpan<char> text) => TsqlLexicalSets.PseudoColumns.Contains(text);

    internal static bool IsTsqlReserved(ReadOnlySpan<char> text) => TsqlLexicalSets.ReservedWords.Contains(text);

    // Kept in their own type so the sets are built on first T-SQL use, never when ISO code touches Keyword.
    internal static class TsqlLexicalSets
    {
        internal static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> PseudoColumns = FrozenSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "$identity", "$rowguid", "$action", TsqlPartition, "$node_id", "$edge_id", "$from_id", "$to_id")
            .GetAlternateLookup<ReadOnlySpan<char>>();

        // The T-SQL reserved keywords. An unquoted reserved word can never be an implicit alias, which is
        // what lets a statement start right after the previous one without a `;`.
        internal static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> ReservedWords = FrozenSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "add", "all", "alter", "and", "any", "as", "asc", "authorization", "backup", "begin", "between",
            "break", "browse", "bulk", "by", "cascade", "case", "check", "checkpoint", "close", "clustered",
            "coalesce", "collate", "column", "commit", "compute", "constraint", "contains", "containstable",
            "continue", "convert", "create", "cross", "current", "current_date", "current_time",
            "current_timestamp", "current_user", "cursor", "database", "dbcc", "deallocate", "declare",
            "default", "delete", "deny", "desc", "disk", "distinct", "distributed", "double", "drop", "dump",
            "else", "end", "errlvl", "escape", "except", "exec", "execute", "exists", "exit", "external",
            "fetch", "file", "fillfactor", "for", "foreign", "freetext", "freetexttable", "from", "full",
            "function", "goto", "grant", "group", "having", "holdlock", "identity", "identity_insert",
            "identitycol", "if", "in", "index", "inner", "insert", "intersect", "into", "is", "join", "key",
            "kill", "left", "like", "lineno", "load", "merge", "national", "nocheck", "nonclustered", "not",
            "null", "nullif", "of", "off", "offsets", "on", "open", "opendatasource", "openquery", "openrowset",
            "openxml", "option", "or", "order", "outer", "over", "percent", "pivot", "plan", "precision",
            "primary", "print", "proc", "procedure", "public", "raiserror", "read", "readtext", "reconfigure",
            "references", "replication", "restore", "restrict", "return", "revert", "revoke", "right",
            "rollback", "rowcount", "rowguidcol", "rule", "save", "schema", "securityaudit", "select",
            "semantickeyphrasetable", "semanticsimilaritydetailstable", "semanticsimilaritytable",
            "session_user", "set", "setuser", "shutdown", "some", "statistics", "system_user", "table",
            "tablesample", "textsize", "then", "to", "top", "tran", "transaction", "trigger", "truncate",
            "try_convert", "tsequal", "union", "unique", "unpivot", "update", "updatetext", "use", "user",
            "values", "varying", "view", "waitfor", "when", "where", "while", "with", "writetext")
            .GetAlternateLookup<ReadOnlySpan<char>>();
    }
}
