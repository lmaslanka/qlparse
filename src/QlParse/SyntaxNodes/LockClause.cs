namespace QlParse;

public sealed class LockClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public SyntaxToken? ReadKeyword { get; init; }
    public SyntaxToken? OnlyKeyword { get; init; }
    public SyntaxToken? UpdateKeyword { get; init; }
    public SyntaxToken? ShareKeyword { get; init; }
    public SyntaxToken? OfKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
}
