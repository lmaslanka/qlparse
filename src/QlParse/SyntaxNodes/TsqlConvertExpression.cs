namespace QlParse;

// `CONVERT(type, expression [, style])` and `TRY_CONVERT(...)`: the target type comes first.
public sealed class TsqlConvertExpression : Expression
{
    public required SyntaxToken FunctionName { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required DataType Type { get; init; }
    public required SyntaxToken Comma { get; init; }
    public required Expression Expression { get; init; }
    public SyntaxToken? StyleComma { get; init; }
    public Expression? Style { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
