namespace QlParse;

public sealed class TrimExpression : Expression
{
    public required SyntaxToken TrimKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public SyntaxToken? Specification { get; init; }
    public Expression? Characters { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public required Expression Source { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
