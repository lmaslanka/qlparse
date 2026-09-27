namespace QlParse;

public sealed class UsingTransformExpression : Expression
{
    public required SyntaxToken FunctionKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken UsingKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
