namespace QlParse;

public sealed class PgDefinitionElement
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public SyntaxToken? Sign { get; init; }
    public SyntaxToken? Literal { get; init; }
    public DataType? Type { get; init; }
    public PgOperatorName? Operator { get; init; }
}
