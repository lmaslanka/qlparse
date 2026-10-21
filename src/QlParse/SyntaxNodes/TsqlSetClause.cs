namespace QlParse;

// One T-SQL SET item: `column {= | += | ...} value`, `column = DEFAULT`, `@v {= | += ...} value`, the combined
// `@v = column = value`, or a mutator call `column.WRITE(...)` / `udt.Method(...)` (Column only, no operator).
public sealed class TsqlSetClause
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Variable { get; init; }
    public SyntaxToken? VariableOperator { get; init; }
    public Expression? Column { get; init; }
    public SyntaxToken? Operator { get; init; }
    public Expression? Value { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
}
