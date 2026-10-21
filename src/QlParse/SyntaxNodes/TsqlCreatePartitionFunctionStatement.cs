namespace QlParse;

// `CREATE PARTITION FUNCTION name (type) AS RANGE [LEFT|RIGHT] FOR VALUES (boundary, ...)`.
public sealed class TsqlCreatePartitionFunctionStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken PartitionKeyword { get; init; }
    public required SyntaxToken FunctionKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required DataType ParameterType { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required SyntaxToken RangeKeyword { get; init; }
    public SyntaxToken? Boundary { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken ValuesKeyword { get; init; }
    public required SyntaxToken ValuesOpen { get; init; }
    public IReadOnlyList<Expression> Values { get; init; } = [];
    public required SyntaxToken ValuesClose { get; init; }
}
