namespace QlParse;

public sealed class TypeCastOption
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken CastKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken Source { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required SyntaxToken Target { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required SyntaxToken Function { get; init; }
}
