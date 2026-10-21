namespace QlParse;

// Postgres-only words used to close gaps across the Postgres workstreams. They are matched
// contextually against Identifier tokens and are never reserved by Keyword.Classify.
internal static partial class Keyword
{
    internal const string GapSnapshot = "snapshot";
}
