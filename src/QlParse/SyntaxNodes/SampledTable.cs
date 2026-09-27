namespace QlParse;

public sealed class SampledTable : TableSource
{
    public required TableSource Table { get; init; }
    public required SyntaxToken TablesampleKeyword { get; init; }
    public required SyntaxToken Method { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Percentage { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? RepeatableKeyword { get; init; }
    public SyntaxToken? RepeatOpenParen { get; init; }
    public Expression? RepeatArgument { get; init; }
    public SyntaxToken? RepeatCloseParen { get; init; }
}
