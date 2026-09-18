namespace QlParse;

internal static partial class Keyword
{
    public static SyntaxKind Classify(ReadOnlySpan<char> text)
    {
        var length = text.Length;
        if (length == Keyword.CurrentTimestamp.Length)
        {
            return EqualsKeyword(text, Keyword.CurrentTimestamp)
                ? SyntaxKind.CurrentTimestampKeyword
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

            return EqualsKeyword(text, Keyword.SessionUser)
                ? SyntaxKind.SessionUserKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.SystemUser.Length)
        {
            return EqualsKeyword(text, Keyword.SystemUser)
                ? SyntaxKind.SystemUserKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.Corresponding.Length)
        {
            return EqualsKeyword(text, Keyword.Corresponding)
                ? SyntaxKind.CorrespondingKeyword
                : SyntaxKind.Identifier;
        }

        if (length == Keyword.Distinct.Length)
        {
            if (EqualsKeyword(text, Keyword.Distinct))
            {
                return SyntaxKind.DistinctKeyword;
            }

            if (EqualsKeyword(text, Keyword.Overlaps))
            {
                return SyntaxKind.OverlapsKeyword;
            }

            if (EqualsKeyword(text, Keyword.Interval))
            {
                return SyntaxKind.IntervalKeyword;
            }

            return EqualsKeyword(text, Keyword.Position) ? SyntaxKind.PositionKeyword : SyntaxKind.Identifier;
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

            return EqualsKeyword(text, Keyword.Translate) ? SyntaxKind.TranslateKeyword : SyntaxKind.Identifier;
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

            return EqualsKeyword(text, Keyword.Convert) ? SyntaxKind.ConvertKeyword : SyntaxKind.Identifier;
        }

        if (length == Keyword.Select.Length)
        {
            if (EqualsKeyword(text, Keyword.Select))
            {
                return SyntaxKind.SelectKeyword;
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

            return EqualsKeyword(text, Keyword.Update) ? SyntaxKind.UpdateKeyword : SyntaxKind.Identifier;
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

            return EqualsKeyword(text, Keyword.For) ? SyntaxKind.ForKeyword : SyntaxKind.Identifier;
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

            return EqualsKeyword(text, Keyword.Union) ? SyntaxKind.UnionKeyword : SyntaxKind.Identifier;
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

            return EqualsKeyword(text, Keyword.Only) ? SyntaxKind.OnlyKeyword : SyntaxKind.Identifier;
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

            return EqualsKeyword(text, Keyword.Of) ? SyntaxKind.OfKeyword : SyntaxKind.Identifier;
        }

        return SyntaxKind.Identifier;
    }

    private static bool EqualsKeyword(ReadOnlySpan<char> text, string keyword) =>
        text.Equals(keyword.AsSpan(), StringComparison.OrdinalIgnoreCase);
}
