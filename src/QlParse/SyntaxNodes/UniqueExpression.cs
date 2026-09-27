namespace QlParse;

public sealed class UniqueExpression : Expression
{
    public required SyntaxToken UniqueKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
