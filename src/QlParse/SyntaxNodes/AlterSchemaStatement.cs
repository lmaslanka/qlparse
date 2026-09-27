namespace QlParse;

public sealed class AlterSchemaStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken RenameKeyword { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> NewName { get; init; }
}
