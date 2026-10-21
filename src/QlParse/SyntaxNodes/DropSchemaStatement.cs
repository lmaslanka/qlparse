namespace QlParse;

public sealed class DropSchemaStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }

    // Null only for Postgres, where CASCADE / RESTRICT is optional.
    public SyntaxToken? Behavior { get; init; }
    public ExistenceCheck? IfExists { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>> AdditionalNames { get; init; } = [];
}
