namespace QlParse;

// SQL Server expression words (workstream B): function names whose grammar differs from a plain call,
// the AT TIME ZONE suffix and the VECTOR element types. All are matched by text under SqlOptions.SqlServer
// only and stay ordinary identifiers.
internal static partial class Keyword
{
    internal const string TsqlExprAt = "at";
    internal const string TsqlExprTryConvert = "try_convert";
    internal const string TsqlExprTryCast = "try_cast";
    internal const string TsqlExprParse = "parse";
    internal const string TsqlExprTryParse = "try_parse";
    internal const string TsqlExprFreetext = "freetext";
    internal const string TsqlExprGenerateEmbeddings = "ai_generate_embeddings";
    internal const string TsqlExprGenerateChunks = "ai_generate_chunks";
    internal const string TsqlExprUse = "use";
    internal const string TsqlExprModel = "model";
    internal const string TsqlExprParameters = "parameters";
    internal const string TsqlExprFloat16 = "float16";
    internal const string TsqlExprFloat32 = "float32";
}
