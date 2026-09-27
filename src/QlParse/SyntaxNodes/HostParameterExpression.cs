namespace QlParse;

public sealed class HostParameterExpression : Expression
{
    public required SyntaxToken QuestionMark { get; init; }
}
