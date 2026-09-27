namespace QlParse;

public sealed class RevokeOptionClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Kind { get; init; }
    public required SyntaxToken OptionKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
}
