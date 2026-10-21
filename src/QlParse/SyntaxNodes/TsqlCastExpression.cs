namespace QlParse;

// `TRY_CAST(expression AS type)`, `PARSE(string AS type [USING culture])` and `TRY_PARSE(...)`.
public sealed class TsqlCastExpression : Expression
{
    public required SyntaxToken FunctionName { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required DataType Type { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public Expression? Culture { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
