namespace QlParse;

public sealed class NiladicFunctionExpression : Expression
{
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public SyntaxToken? Precision { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}
