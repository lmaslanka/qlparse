namespace QlParse;

public sealed class EmptyGroupingSetExpression : Expression
{
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
