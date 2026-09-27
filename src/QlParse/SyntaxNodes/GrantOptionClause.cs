namespace QlParse;

public sealed class GrantOptionClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required SyntaxToken Kind { get; init; }
    public required SyntaxToken OptionKeyword { get; init; }
}
