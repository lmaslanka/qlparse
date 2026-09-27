namespace QlParse;

public sealed class TransactionMode
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? IsolationKeyword { get; init; }
    public SyntaxToken? LevelKeyword { get; init; }
    public SyntaxToken? Access { get; init; }
    public SyntaxToken? Level { get; init; }
    public SyntaxToken? ReadKeyword { get; init; }
    public SyntaxToken? DiagnosticsKeyword { get; init; }
    public SyntaxToken? SizeKeyword { get; init; }
    public Expression? Size { get; init; }
}
