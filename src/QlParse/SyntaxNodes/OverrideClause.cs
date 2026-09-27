namespace QlParse;

public sealed class OverrideClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OverridingKeyword { get; init; }
    public required SyntaxToken Kind { get; init; }
    public required SyntaxToken ValueKeyword { get; init; }
}
