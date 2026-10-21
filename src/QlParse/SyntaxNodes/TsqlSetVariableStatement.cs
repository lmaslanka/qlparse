namespace QlParse;

// SET @v {= | += | -= | ...} expr, or SET @cursor = CURSOR ... FOR select. Exactly one of Value and Cursor is set.
public sealed class TsqlSetVariableStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken Variable { get; init; }
    public required SyntaxToken OperatorToken { get; init; }
    public Expression? Value { get; init; }
    public TsqlCursorDefinition? Cursor { get; init; }
}
