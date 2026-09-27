namespace QlParse;

public sealed class IdentityColumn
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken GeneratedKeyword { get; init; }
    public SyntaxToken? AlwaysKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required SyntaxToken IdentityKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SequenceOption> Options { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}
