namespace QlParse;

// Postgres DDL words (workstream D), matched contextually so they stay non-reserved.
// Generic words carry a Ddl prefix so they cannot clash with constants other dialect streams add.
internal static partial class Keyword
{
    internal const string Attach = "attach";
    internal const string Compression = "compression";
    internal const string Concurrently = "concurrently";
    internal const string Detach = "detach";
    internal const string Finalize = "finalize";
    internal const string Inherits = "inherits";
    internal const string Logged = "logged";
    internal const string Modulus = "modulus";
    internal const string Oids = "oids";
    internal const string Refresh = "refresh";
    internal const string Reindex = "reindex";
    internal const string Unlogged = "unlogged";
    internal const string DdlAlso = "also";
    internal const string DdlCache = "cache";
    internal const string DdlCluster = "cluster";
    internal const string DdlEnable = "enable";
    internal const string DdlExpression = "expression";
    internal const string DdlInclude = "include";
    internal const string DdlInherit = "inherit";
    internal const string DdlLocation = "location";
    internal const string DdlMaterialized = "materialized";
    internal const string DdlNothing = "nothing";
    internal const string DdlNowait = "nowait";
    internal const string DdlOwned = "owned";
    internal const string DdlOwner = "owner";
    internal const string DdlReplace = "replace";
    internal const string DdlReplica = "replica";
    internal const string DdlReset = "reset";
    internal const string DdlRule = "rule";
    internal const string DdlStatistics = "statistics";
    internal const string DdlStorage = "storage";
    internal const string DdlTablespace = "tablespace";
    internal const string DdlTemp = "temp";
    internal const string DdlValid = "valid";
    internal const string DdlValidate = "validate";
}
