namespace QlParse;

// Contextual words of the Postgres schema-object statements (stream E). None of them is reserved:
// the parser matches them by text only, so ISO mode keeps treating them as plain identifiers.
internal static partial class Keyword
{
    internal const string PgAccess = "access";
    internal const string PgAggregate = "aggregate";
    internal const string PgAttribute = "attribute";
    internal const string PgClass = "class";
    internal const string PgConfiguration = "configuration";
    internal const string PgConversion = "conversion";
    internal const string PgDepends = "depends";
    internal const string PgDictionary = "dictionary";
    internal const string PgEnable = "enable";
    internal const string PgEnum = "enum";
    internal const string PgEvent = "event";
    internal const string PgExtension = "extension";
    internal const string PgFamily = "family";
    internal const string PgImplicit = "implicit";
    internal const string PgImport = "import";
    internal const string PgInherits = "inherits";
    internal const string PgInline = "inline";
    internal const string PgMapping = "mapping";
    internal const string PgOwner = "owner";
    internal const string PgParser = "parser";
    internal const string PgProcedural = "procedural";
    internal const string PgPublication = "publication";
    internal const string PgRefresh = "refresh";
    internal const string PgReplace = "replace";
    internal const string PgReplica = "replica";
    internal const string PgReset = "reset";
    internal const string PgServer = "server";
    internal const string PgStatistics = "statistics";
    internal const string PgStorage = "storage";
    internal const string PgSubscription = "subscription";
    internal const string PgTag = "tag";
    internal const string PgTemplate = "template";
    internal const string PgText = "text";
    internal const string PgTrusted = "trusted";
    internal const string PgValidator = "validator";
    internal const string PgVersion = "version";
}
