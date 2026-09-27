namespace QlParse;

public sealed class AbsentOnNullExpression : Expression
{
    public required SyntaxToken AbsentKeyword { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required SyntaxToken NullKeyword { get; init; }
}
