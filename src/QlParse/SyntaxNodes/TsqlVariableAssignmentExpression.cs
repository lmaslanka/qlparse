namespace QlParse;

// `@variable = expression` (or a compound operator such as `+=`) in a SELECT list.
public sealed class TsqlVariableAssignmentExpression : Expression
{
    public required SyntaxToken Variable { get; init; }
    public required SyntaxToken Operator { get; init; }
    public required Expression Value { get; init; }
}
