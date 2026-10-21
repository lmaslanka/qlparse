using System.Collections.Frozen;

namespace QlParse;

// SQL Server words used by workstream C (query expressions and DML). They are matched contextually against
// identifier tokens under SqlOptions.SqlServer only and never change how ISO or Postgres input is classified.
internal static partial class Keyword
{
    internal const string DmlTop = "top";
    internal const string DmlApply = "apply";
    internal const string DmlPivot = "pivot";
    internal const string DmlUnpivot = "unpivot";
    internal const string DmlOption = "option";
    internal const string DmlCompute = "compute";
    internal const string DmlBrowse = "browse";
    internal const string DmlContained = "contained";
    internal const string DmlBulk = "bulk";
    internal const string DmlStatistics = "statistics";
    internal const string DmlPartitions = "partitions";
    internal const string DmlXmlNamespaces = "xmlnamespaces";
    internal const string DmlElements = "elements";
    internal const string DmlXsinil = "xsinil";
    internal const string DmlBinary = "binary";
    internal const string DmlBase64 = "base64";
    internal const string DmlHash = "hash";
    internal const string DmlLoop = "loop";
    internal const string DmlRemote = "remote";

    internal static bool IsTsqlRowsetFunction(ReadOnlySpan<char> text) => TsqlDmlSets.RowsetFunctions.Contains(text);

    // Kept in their own type so the set is built on first T-SQL use, never when ISO code touches Keyword.
    internal static class TsqlDmlSets
    {
        // FROM-clause functions whose arguments are not a plain expression list (named arguments, `*`,
        // `LANGUAGE x`, BULK, `;`-separated connection parts) or that take a WITH (...) result schema.
        internal static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> RowsetFunctions = FrozenSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "openjson", "openxml", "openrowset", "openquery", "opendatasource", "containstable", "freetexttable",
            "semantickeyphrasetable", "semanticsimilaritytable", "semanticsimilaritydetailstable", "vector_search")
            .GetAlternateLookup<ReadOnlySpan<char>>();
    }
}
