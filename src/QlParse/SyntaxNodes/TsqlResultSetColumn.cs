namespace QlParse;

public sealed class TsqlResultSetColumn
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required DataType DataType { get; init; }
    public SyntaxToken? CollateKeyword { get; init; }
    public SyntaxToken? Collation { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? NullKeyword { get; init; }
}
