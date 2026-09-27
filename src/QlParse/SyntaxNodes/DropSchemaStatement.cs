namespace QlParse;

public sealed class DropSchemaStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}
