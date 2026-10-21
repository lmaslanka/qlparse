namespace QlParse;

// `ALTER PARTITION FUNCTION name () {SPLIT|MERGE} RANGE (boundary)`.
public sealed class TsqlAlterPartitionFunctionStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken PartitionKeyword { get; init; }
    public required SyntaxToken FunctionKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public required SyntaxToken ActionKeyword { get; init; }
    public required SyntaxToken RangeKeyword { get; init; }
    public required SyntaxToken ValueOpen { get; init; }
    public required Expression Value { get; init; }
    public required SyntaxToken ValueClose { get; init; }
}
