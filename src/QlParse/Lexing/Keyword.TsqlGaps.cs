using System.Collections.Frozen;

namespace QlParse;

// SQL Server gap closing (workstream H). Only the T-SQL reserved keywords are reserved in T-SQL, so an
// ISO keyword kind outside that list (DATE, TIME, POSITION, FILTER, WINDOW, OFFSET, ...) lexes as an
// ordinary identifier under SqlOptions.SqlServer, and the T-SQL grammar matches those words by text.
internal static partial class Keyword
{
    internal const string GapsClassifier = "classifier";
    internal const string GapsBinding = "binding";
    internal const string GapsBroker = "broker";
    internal const string GapsPriority = "priority";
    internal const string GapsLibrary = "library";
    internal const string GapsDependents = "dependents";
    internal const string GapsRemove = "remove";
    internal const string GapsChanges = "changes";
    internal const string GapsVersion = "version";
    internal const string GapsProvider = "provider";
    internal const string GapsStop = "stop";
    internal const string GapsIncremental = "incremental";
    internal const string GapsChangeTracking = "change_tracking";

    internal static bool IsTsqlLegacyTableHint(ReadOnlySpan<char> text) => TsqlGapsSets.LegacyTableHints.Contains(text);

    // Kept in their own type so the set is built on first T-SQL use, never when ISO code touches Keyword.
    internal static class TsqlGapsSets
    {
        // The table hints SQL Server accepts without WITH (`FROM t (NOLOCK)`), plus the equally common HOLDLOCK.
        internal static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> LegacyTableHints = FrozenSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "nolock", "readuncommitted", "updlock", "repeatableread", "serializable", "readcommitted", "tablock",
            "tablockx", "paglock", "rowlock", "nowait", "readpast", "xlock", "snapshot", "noexpand", "holdlock")
            .GetAlternateLookup<ReadOnlySpan<char>>();
    }

    // The kind a word classified as `kind` takes in T-SQL: itself when the word is a T-SQL reserved
    // keyword, otherwise Identifier.
    internal static SyntaxKind TsqlKind(SyntaxKind kind) => kind switch
    {
        SyntaxKind.SelectKeyword or SyntaxKind.FromKeyword or SyntaxKind.WhereKeyword or SyntaxKind.AndKeyword
            or SyntaxKind.OrKeyword or SyntaxKind.AsKeyword or SyntaxKind.InnerKeyword or SyntaxKind.JoinKeyword
            or SyntaxKind.LeftKeyword or SyntaxKind.RightKeyword or SyntaxKind.FullKeyword or SyntaxKind.CrossKeyword
            or SyntaxKind.OuterKeyword or SyntaxKind.OnKeyword or SyntaxKind.BetweenKeyword or SyntaxKind.InKeyword
            or SyntaxKind.IsKeyword or SyntaxKind.NullKeyword or SyntaxKind.GroupKeyword or SyntaxKind.ByKeyword
            or SyntaxKind.OrderKeyword or SyntaxKind.FetchKeyword or SyntaxKind.HavingKeyword or SyntaxKind.NotKeyword
            or SyntaxKind.AscKeyword or SyntaxKind.DescKeyword or SyntaxKind.CaseKeyword or SyntaxKind.WhenKeyword
            or SyntaxKind.ThenKeyword or SyntaxKind.ElseKeyword or SyntaxKind.EndKeyword or SyntaxKind.ExistsKeyword
            or SyntaxKind.AllKeyword or SyntaxKind.AnyKeyword or SyntaxKind.SomeKeyword or SyntaxKind.UnionKeyword
            or SyntaxKind.ExceptKeyword or SyntaxKind.IntersectKeyword or SyntaxKind.WithKeyword or SyntaxKind.OverKeyword
            or SyntaxKind.TablesampleKeyword or SyntaxKind.DistinctKeyword or SyntaxKind.LikeKeyword
            or SyntaxKind.NullIfKeyword or SyntaxKind.CoalesceKeyword or SyntaxKind.SetKeyword or SyntaxKind.EscapeKeyword
            or SyntaxKind.ValuesKeyword or SyntaxKind.CollateKeyword or SyntaxKind.UniqueKeyword or SyntaxKind.ForKeyword
            or SyntaxKind.UserKeyword or SyntaxKind.CurrentDateKeyword or SyntaxKind.CurrentTimeKeyword
            or SyntaxKind.CurrentTimestampKeyword or SyntaxKind.CurrentUserKeyword or SyntaxKind.SessionUserKeyword
            or SyntaxKind.SystemUserKeyword or SyntaxKind.ConvertKeyword or SyntaxKind.ReadKeyword
            or SyntaxKind.UpdateKeyword or SyntaxKind.OfKeyword => kind,
        _ => SyntaxKind.Identifier,
    };
}
