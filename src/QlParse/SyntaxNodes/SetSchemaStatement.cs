namespace QlParse;

public sealed class SetSchemaStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public SyntaxToken? Value { get; init; }
    public IReadOnlyList<SyntaxToken>? Name { get; init; }
}
