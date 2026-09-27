namespace QlParse;

public sealed class TargetTable
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? OnlyKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> NameParts { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}
