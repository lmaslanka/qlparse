namespace QlParse;

public sealed class CopyOption
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public IReadOnlyList<SyntaxToken> NameTail { get; init; } = [];
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Value { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken> Values { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}
