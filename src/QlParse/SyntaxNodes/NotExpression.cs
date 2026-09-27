namespace QlParse;

public sealed class NotExpression : Expression
{
    public required SyntaxToken NotKeyword { get; init; }
    public required Expression Expression { get; init; }
}
