using System.Collections.Frozen;

namespace QlParse;

// SQL Server procedural, module, trigger, transaction and session words (workstream E). They stay plain
// identifiers in the lexer and are matched by text only under SqlOptions.SqlServer, so none of them is
// reserved in ISO or Postgres mode.
internal static partial class Keyword
{
    internal const string ProcAggregate = "aggregate";
    internal const string ProcAppend = "append";
    internal const string ProcAt = "at";
    internal const string ProcBreak = "break";
    internal const string ProcCatch = "catch";
    internal const string ProcDataSource = "data_source";
    internal const string ProcDatabase = "database";
    internal const string ProcDelay = "delay";
    internal const string ProcDisable = "disable";
    internal const string ProcDistributed = "distributed";
    internal const string ProcEnable = "enable";
    internal const string ProcEvent = "event";
    internal const string ProcFanIn = "fan_in";
    internal const string ProcIdentityInsert = "identity_insert";
    internal const string ProcLogin = "login";
    internal const string ProcMark = "mark";
    internal const string ProcNotification = "notification";
    internal const string ProcOff = "off";
    internal const string ProcPrint = "print";
    internal const string ProcProc = "proc";
    internal const string ProcQueue = "queue";
    internal const string ProcRaiserror = "raiserror";
    internal const string ProcReadonly = "readonly";
    internal const string ProcRecompile = "recompile";
    internal const string ProcReplication = "replication";
    internal const string ProcSave = "save";
    internal const string ProcServer = "server";
    internal const string ProcService = "service";
    internal const string ProcSnapshot = "snapshot";
    internal const string ProcStatistics = "statistics";
    internal const string ProcTimeout = "timeout";
    internal const string ProcTran = "tran";
    internal const string ProcTry = "try";
    internal const string ProcUndefined = "undefined";
    internal const string ProcUse = "use";
    internal const string ProcWaitfor = "waitfor";

    // SET options that take ON/OFF or a single value (`SET NOCOUNT ON`, `SET DATEFIRST 7`).
    internal static bool IsTsqlSessionOption(ReadOnlySpan<char> text) => TsqlProcSets.SessionOptions.Contains(text);

    // Words allowed between CURSOR and FOR in a T-SQL cursor declaration.
    internal static bool IsTsqlCursorOption(ReadOnlySpan<char> text) => TsqlProcSets.CursorOptions.Contains(text);

    // Unreserved words that begin a T-SQL statement, so an optional trailing name never swallows them.
    internal static bool IsTsqlUnreservedVerb(ReadOnlySpan<char> text) => TsqlProcSets.UnreservedVerbs.Contains(text);

    // Built on first T-SQL use, never when ISO code touches Keyword.
    internal static class TsqlProcSets
    {
        internal static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> SessionOptions = FrozenSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "ansi_defaults", "ansi_null_dflt_off", "ansi_null_dflt_on", "ansi_nulls", "ansi_padding", "ansi_warnings",
            "arithabort", "arithignore", "concat_null_yields_null", "context_info", "cursor_close_on_commit",
            "datefirst", "dateformat", "deadlock_priority", "fips_flagger", "fmtonly", "forceplan",
            "implicit_transactions", "language", "lock_timeout", "no_browsetable", "nocount", "noexec",
            "numeric_roundabort", "parseonly", "query_governor_cost_limit", "quoted_identifier",
            "remote_proc_transactions", "rowcount", "showplan_all", "showplan_text", "showplan_xml", "textsize",
            "xact_abort")
            .GetAlternateLookup<ReadOnlySpan<char>>();

        internal static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> CursorOptions = FrozenSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "local", "global", "forward_only", "scroll", "static", "keyset", "dynamic", "fast_forward", "read_only",
            "scroll_locks", "optimistic", "type_warning")
            .GetAlternateLookup<ReadOnlySpan<char>>();

        internal static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> UnreservedVerbs = FrozenSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "throw", "enable", "disable", "move", "send", "receive", "get")
            .GetAlternateLookup<ReadOnlySpan<char>>();
    }
}
