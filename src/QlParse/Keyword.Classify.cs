namespace QlParse;

internal static partial class Keyword
{
    public static SyntaxKind Classify(ReadOnlySpan<char> text)
    {
        var length = text.Length;
        if (length == Keyword.CurrentTimestamp.Length)
        {
            if (EqualsKeyword(text, Keyword.CurrentTimestamp))
            {
                return SyntaxKind.CurrentTimestampKeyword;
            }

            return EqualsKeyword(text, Keyword.CharacterLength)
                ? SyntaxKind.CharacterLengthKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.CurrentDate.Length)
        {
            if (EqualsKeyword(text, Keyword.CurrentDate))
            {
                return SyntaxKind.CurrentDateKeyword;
            }

            if (EqualsKeyword(text, Keyword.CurrentTime))
            {
                return SyntaxKind.CurrentTimeKeyword;
            }

            if (EqualsKeyword(text, Keyword.CurrentUser))
            {
                return SyntaxKind.CurrentUserKeyword;
            }

            if (EqualsKeyword(text, Keyword.SessionUser))
            {
                return SyntaxKind.SessionUserKeyword;
            }

            if (EqualsKeyword(text, Keyword.OctetLength))
            {
                return SyntaxKind.OctetLengthKeyword;
            }

            if (EqualsKeyword(text, Keyword.WidthBucket))
            {
                return SyntaxKind.WidthBucketKeyword;
            }

            if (EqualsKeyword(text, Keyword.Specifictype))
            {
                return SyntaxKind.SpecifictypeKeyword;
            }

            if (EqualsKeyword(text, Keyword.CurrentRole))
            {
                return SyntaxKind.CurrentRoleKeyword;
            }

            return EqualsKeyword(text, Keyword.CurrentPath)
                ? SyntaxKind.CurrentPathKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.SystemUser.Length)
        {
            if (EqualsKeyword(text, Keyword.SystemUser))
            {
                return SyntaxKind.SystemUserKeyword;
            }

            if (EqualsKeyword(text, Keyword.CharLength))
            {
                return SyntaxKind.CharLengthKeyword;
            }

            if (EqualsKeyword(text, Keyword.Cardinality))
            {
                return SyntaxKind.CardinalityKeyword;
            }

            return EqualsKeyword(text, Keyword.Tablesample)
                ? SyntaxKind.TablesampleKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.BitLength.Length)
        {
            return EqualsKeyword(text, Keyword.BitLength)
                ? SyntaxKind.BitLengthKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.Corresponding.Length)
        {
            return EqualsKeyword(text, Keyword.Corresponding)
                ? SyntaxKind.CorrespondingKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.LocalTimestamp.Length)
        {
            if (EqualsKeyword(text, Keyword.LocalTimestamp))
            {
                return SyntaxKind.LocalTimestampKeyword;
            }

            return EqualsKeyword(text, Keyword.CurrentSchema)
                ? SyntaxKind.CurrentSchemaKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.CurrentCatalog.Length)
        {
            return EqualsKeyword(text, Keyword.CurrentCatalog)
                ? SyntaxKind.CurrentCatalogKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.Distinct.Length)
        {
            if (EqualsKeyword(text, Keyword.Distinct))
            {
                return SyntaxKind.DistinctKeyword;
            }

            if (EqualsKeyword(text, Keyword.Coalesce))
            {
                return SyntaxKind.CoalesceKeyword;
            }

            if (EqualsKeyword(text, Keyword.Overlaps))
            {
                return SyntaxKind.OverlapsKeyword;
            }

            if (EqualsKeyword(text, Keyword.Interval))
            {
                return SyntaxKind.IntervalKeyword;
            }

            if (EqualsKeyword(text, Keyword.Position))
            {
                return SyntaxKind.PositionKeyword;
            }

            if (EqualsKeyword(text, Keyword.Multiset))
            {
                return SyntaxKind.MultisetKeyword;
            }

            return EqualsKeyword(text, Keyword.Grouping)
                ? SyntaxKind.GroupingKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.Intersect.Length)
        {
            if (EqualsKeyword(text, Keyword.Intersect))
            {
                return SyntaxKind.IntersectKeyword;
            }

            if (EqualsKeyword(text, Keyword.Recursive))
            {
                return SyntaxKind.RecursiveKeyword;
            }

            if (EqualsKeyword(text, Keyword.Timestamp))
            {
                return SyntaxKind.TimestampKeyword;
            }

            if (EqualsKeyword(text, Keyword.Substring))
            {
                return SyntaxKind.SubstringKeyword;
            }

            if (EqualsKeyword(text, Keyword.Translate))
            {
                return SyntaxKind.TranslateKeyword;
            }

            if (EqualsKeyword(text, Keyword.Normalize))
            {
                return SyntaxKind.NormalizeKeyword;
            }

            return EqualsKeyword(text, Keyword.LocalTime)
                ? SyntaxKind.LocalTimeKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.Between.Length)
        {
            if (EqualsKeyword(text, Keyword.Between))
            {
                return SyntaxKind.BetweenKeyword;
            }

            if (EqualsKeyword(text, Keyword.Unknown))
            {
                return SyntaxKind.UnknownKeyword;
            }

            if (EqualsKeyword(text, Keyword.Collate))
            {
                return SyntaxKind.CollateKeyword;
            }

            if (EqualsKeyword(text, Keyword.Partial))
            {
                return SyntaxKind.PartialKeyword;
            }

            if (EqualsKeyword(text, Keyword.Natural))
            {
                return SyntaxKind.NaturalKeyword;
            }

            if (EqualsKeyword(text, Keyword.Extract))
            {
                return SyntaxKind.ExtractKeyword;
            }

            if (EqualsKeyword(text, Keyword.Convert))
            {
                return SyntaxKind.ConvertKeyword;
            }

            if (EqualsKeyword(text, Keyword.Overlay))
            {
                return SyntaxKind.OverlayKeyword;
            }

            if (EqualsKeyword(text, Keyword.Ceiling))
            {
                return SyntaxKind.CeilingKeyword;
            }

            if (EqualsKeyword(text, Keyword.Element))
            {
                return SyntaxKind.ElementKeyword;
            }

            if (EqualsKeyword(text, Keyword.Similar))
            {
                return SyntaxKind.SimilarKeyword;
            }

            return EqualsKeyword(text, Keyword.Lateral)
                ? SyntaxKind.LateralKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.Select.Length)
        {
            if (EqualsKeyword(text, Keyword.Select))
            {
                return SyntaxKind.SelectKeyword;
            }

            if (EqualsKeyword(text, Keyword.NullIf))
            {
                return SyntaxKind.NullIfKeyword;
            }

            if (EqualsKeyword(text, Keyword.Having))
            {
                return SyntaxKind.HavingKeyword;
            }

            if (EqualsKeyword(text, Keyword.Offset))
            {
                return SyntaxKind.OffsetKeyword;
            }

            if (EqualsKeyword(text, Keyword.Exists))
            {
                return SyntaxKind.ExistsKeyword;
            }

            if (EqualsKeyword(text, Keyword.Unique))
            {
                return SyntaxKind.UniqueKeyword;
            }

            if (EqualsKeyword(text, Keyword.Filter))
            {
                return SyntaxKind.FilterKeyword;
            }

            if (EqualsKeyword(text, Keyword.Escape))
            {
                return SyntaxKind.EscapeKeyword;
            }

            if (EqualsKeyword(text, Keyword.Values))
            {
                return SyntaxKind.ValuesKeyword;
            }

            if (EqualsKeyword(text, Keyword.Except))
            {
                return SyntaxKind.ExceptKeyword;
            }

            if (EqualsKeyword(text, Keyword.Update))
            {
                return SyntaxKind.UpdateKeyword;
            }

            if (EqualsKeyword(text, Keyword.Unnest))
            {
                return SyntaxKind.UnnestKeyword;
            }

            if (EqualsKeyword(text, Keyword.Absent))
            {
                return SyntaxKind.AbsentKeyword;
            }

            if (EqualsKeyword(text, Keyword.Search))
            {
                return SyntaxKind.SearchKeyword;
            }

            return EqualsKeyword(text, Keyword.Window)
                ? SyntaxKind.WindowKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.And.Length)
        {
            if (EqualsKeyword(text, Keyword.And))
            {
                return SyntaxKind.AndKeyword;
            }

            if (EqualsKeyword(text, Keyword.Asc))
            {
                return SyntaxKind.AscKeyword;
            }

            if (EqualsKeyword(text, Keyword.Not))
            {
                return SyntaxKind.NotKeyword;
            }

            if (EqualsKeyword(text, Keyword.End))
            {
                return SyntaxKind.EndKeyword;
            }

            if (EqualsKeyword(text, Keyword.All))
            {
                return SyntaxKind.AllKeyword;
            }

            if (EqualsKeyword(text, Keyword.Any))
            {
                return SyntaxKind.AnyKeyword;
            }

            if (EqualsKeyword(text, Keyword.For))
            {
                return SyntaxKind.ForKeyword;
            }

            if (EqualsKeyword(text, Keyword.Exp))
            {
                return SyntaxKind.ExpKeyword;
            }

            if (EqualsKeyword(text, Keyword.Mod))
            {
                return SyntaxKind.ModKeyword;
            }

            if (EqualsKeyword(text, Keyword.Abs))
            {
                return SyntaxKind.AbsKeyword;
            }

            return EqualsKeyword(text, Keyword.Set)
                ? SyntaxKind.SetKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.Where.Length)
        {
            if (EqualsKeyword(text, Keyword.Where))
            {
                return SyntaxKind.WhereKeyword;
            }

            if (EqualsKeyword(text, Keyword.Array))
            {
                return SyntaxKind.ArrayKeyword;
            }

            if (EqualsKeyword(text, Keyword.False))
            {
                return SyntaxKind.FalseKeyword;
            }

            if (EqualsKeyword(text, Keyword.Match))
            {
                return SyntaxKind.MatchKeyword;
            }

            if (EqualsKeyword(text, Keyword.Treat))
            {
                return SyntaxKind.TreatKeyword;
            }

            if (EqualsKeyword(text, Keyword.Deref))
            {
                return SyntaxKind.DerefKeyword;
            }

            if (EqualsKeyword(text, Keyword.Inner))
            {
                return SyntaxKind.InnerKeyword;
            }

            if (EqualsKeyword(text, Keyword.Right))
            {
                return SyntaxKind.RightKeyword;
            }

            if (EqualsKeyword(text, Keyword.Cross))
            {
                return SyntaxKind.CrossKeyword;
            }

            if (EqualsKeyword(text, Keyword.Outer))
            {
                return SyntaxKind.OuterKeyword;
            }

            if (EqualsKeyword(text, Keyword.Using))
            {
                return SyntaxKind.UsingKeyword;
            }

            if (EqualsKeyword(text, Keyword.Group))
            {
                return SyntaxKind.GroupKeyword;
            }

            if (EqualsKeyword(text, Keyword.Order))
            {
                return SyntaxKind.OrderKeyword;
            }

            if (EqualsKeyword(text, Keyword.Limit))
            {
                return SyntaxKind.LimitKeyword;
            }

            if (EqualsKeyword(text, Keyword.Union))
            {
                return SyntaxKind.UnionKeyword;
            }

            if (EqualsKeyword(text, Keyword.Upper))
            {
                return SyntaxKind.UpperKeyword;
            }

            if (EqualsKeyword(text, Keyword.Lower))
            {
                return SyntaxKind.LowerKeyword;
            }

            if (EqualsKeyword(text, Keyword.Floor))
            {
                return SyntaxKind.FloorKeyword;
            }

            if (EqualsKeyword(text, Keyword.Power))
            {
                return SyntaxKind.PowerKeyword;
            }

            if (EqualsKeyword(text, Keyword.Cycle))
            {
                return SyntaxKind.CycleKeyword;
            }

            return EqualsKeyword(text, Keyword.Fetch)
                ? SyntaxKind.FetchKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.From.Length)
        {
            if (EqualsKeyword(text, Keyword.From))
            {
                return SyntaxKind.FromKeyword;
            }

            if (EqualsKeyword(text, Keyword.Join))
            {
                return SyntaxKind.JoinKeyword;
            }

            if (EqualsKeyword(text, Keyword.Left))
            {
                return SyntaxKind.LeftKeyword;
            }

            if (EqualsKeyword(text, Keyword.Like))
            {
                return SyntaxKind.LikeKeyword;
            }

            if (EqualsKeyword(text, Keyword.Null))
            {
                return SyntaxKind.NullKeyword;
            }

            if (EqualsKeyword(text, Keyword.Full))
            {
                return SyntaxKind.FullKeyword;
            }

            if (EqualsKeyword(text, Keyword.Desc))
            {
                return SyntaxKind.DescKeyword;
            }

            if (EqualsKeyword(text, Keyword.Case))
            {
                return SyntaxKind.CaseKeyword;
            }

            if (EqualsKeyword(text, Keyword.Cast))
            {
                return SyntaxKind.CastKeyword;
            }

            if (EqualsKeyword(text, Keyword.True))
            {
                return SyntaxKind.TrueKeyword;
            }

            if (EqualsKeyword(text, Keyword.When))
            {
                return SyntaxKind.WhenKeyword;
            }

            if (EqualsKeyword(text, Keyword.Then))
            {
                return SyntaxKind.ThenKeyword;
            }

            if (EqualsKeyword(text, Keyword.Else))
            {
                return SyntaxKind.ElseKeyword;
            }

            if (EqualsKeyword(text, Keyword.Some))
            {
                return SyntaxKind.SomeKeyword;
            }

            if (EqualsKeyword(text, Keyword.With))
            {
                return SyntaxKind.WithKeyword;
            }

            if (EqualsKeyword(text, Keyword.Date))
            {
                return SyntaxKind.DateKeyword;
            }

            if (EqualsKeyword(text, Keyword.Time))
            {
                return SyntaxKind.TimeKeyword;
            }

            if (EqualsKeyword(text, Keyword.Trim))
            {
                return SyntaxKind.TrimKeyword;
            }

            if (EqualsKeyword(text, Keyword.User))
            {
                return SyntaxKind.UserKeyword;
            }

            if (EqualsKeyword(text, Keyword.Read))
            {
                return SyntaxKind.ReadKeyword;
            }

            if (EqualsKeyword(text, Keyword.Only))
            {
                return SyntaxKind.OnlyKeyword;
            }

            if (EqualsKeyword(text, Keyword.Ceil))
            {
                return SyntaxKind.CeilKeyword;
            }

            if (EqualsKeyword(text, Keyword.Sqrt))
            {
                return SyntaxKind.SqrtKeyword;
            }

            return EqualsKeyword(text, Keyword.Over)
                ? SyntaxKind.OverKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.Or.Length)
        {
            if (EqualsKeyword(text, Keyword.Or))
            {
                return SyntaxKind.OrKeyword;
            }

            if (EqualsKeyword(text, Keyword.As))
            {
                return SyntaxKind.AsKeyword;
            }

            if (EqualsKeyword(text, Keyword.In))
            {
                return SyntaxKind.InKeyword;
            }

            if (EqualsKeyword(text, Keyword.Is))
            {
                return SyntaxKind.IsKeyword;
            }

            if (EqualsKeyword(text, Keyword.On))
            {
                return SyntaxKind.OnKeyword;
            }

            if (EqualsKeyword(text, Keyword.By))
            {
                return SyntaxKind.ByKeyword;
            }

            if (EqualsKeyword(text, Keyword.Of))
            {
                return SyntaxKind.OfKeyword;
            }

            return EqualsKeyword(text, Keyword.Ln)
                ? SyntaxKind.LnKeyword
                : SyntaxKind.Identifier;
        }

        return SyntaxKind.Identifier;
    }

    private static bool EqualsKeyword(ReadOnlySpan<char> text, string keyword) =>
        text.Equals(keyword.AsSpan(), StringComparison.OrdinalIgnoreCase);
}
