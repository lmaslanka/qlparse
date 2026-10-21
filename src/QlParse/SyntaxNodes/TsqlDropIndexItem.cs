namespace QlParse;

// One index of a DROP INDEX list: `name ON table [WITH (...)]` or the legacy `table.name`.
public sealed class TsqlDropIndexItem
{
    public required SourceSpan Span { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? TableName { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlRelationalOptionList? Options { get; init; }
}
