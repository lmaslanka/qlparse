namespace QlParse;

public sealed class GeneratedColumn
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken GeneratedKeyword { get; init; }
    public SyntaxToken? AlwaysKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public Expression? Expression { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? RowKeyword { get; init; }
    public SyntaxToken? Bound { get; init; }
}
