namespace QlParse;

public sealed class PgForeignColumn
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required DataType Type { get; init; }
    public PgOptionList? Options { get; init; }
    public SyntaxToken? CollateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Collation { get; init; }
    public SyntaxToken? ConstraintKeyword { get; init; }
    public SyntaxToken? ConstraintName { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? NullKeyword { get; init; }
    public DefaultClause? Default { get; init; }
}
