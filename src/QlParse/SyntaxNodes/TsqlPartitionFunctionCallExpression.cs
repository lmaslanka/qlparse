namespace QlParse;

// `[database.]$PARTITION.function(expression)`: the partition number a value maps to.
public sealed class TsqlPartitionFunctionCallExpression : Expression
{
    public SyntaxToken? Database { get; init; }
    public SyntaxToken? DatabaseDot { get; init; }
    public required SyntaxToken PartitionKeyword { get; init; }
    public required SyntaxToken Dot { get; init; }
    public required SyntaxToken FunctionName { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Argument { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
