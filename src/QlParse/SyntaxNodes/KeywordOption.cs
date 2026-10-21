namespace QlParse;

public sealed class KeywordOption
{
    public required SourceSpan Span { get; init; }
    public required IReadOnlyList<SyntaxToken> Keywords { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public OptionValue? Value { get; init; }
    public IReadOnlyList<SyntaxToken> Roles { get; init; } = [];
}
