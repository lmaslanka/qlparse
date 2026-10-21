namespace QlParse;

public sealed class StorageParameter
{
    public required SourceSpan Span { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public SyntaxToken? Sign { get; init; }
    public SyntaxToken? Value { get; init; }
}
