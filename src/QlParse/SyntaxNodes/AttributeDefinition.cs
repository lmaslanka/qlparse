namespace QlParse;

public sealed class AttributeDefinition
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required DataType Type { get; init; }
    public SyntaxToken? ReferencesKeyword { get; init; }
    public SyntaxToken? AreKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? CheckedKeyword { get; init; }
    public ReferentialAction? OnDelete { get; init; }
    public DefaultClause? Default { get; init; }
    public SyntaxToken? CollateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Collation { get; init; }
}
