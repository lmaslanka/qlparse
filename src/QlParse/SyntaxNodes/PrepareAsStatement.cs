namespace QlParse;

public sealed class PrepareAsStatement : Query
{
    public required SyntaxToken PrepareKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<DataType> ParameterTypes { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required Query Statement { get; init; }
}
