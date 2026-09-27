namespace QlParse;

public sealed class SubstringExpression : Expression
{
    public required SyntaxToken SubstringKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Source { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required Expression Start { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public Expression? Length { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
