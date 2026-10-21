namespace QlParse;

// `NEXT VALUE FOR sequence OVER (ORDER BY ...)`: the ISO sequence expression plus the ordering that
// assigns the generated values across the rows of one statement.
public sealed class TsqlNextValueOverExpression : Expression
{
    public required NextValueExpression NextValue { get; init; }
    public required WindowSpecification Over { get; init; }
}
