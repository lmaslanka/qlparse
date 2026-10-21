namespace QlParse;

public sealed class AlterSchemaStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }

    // Null only for the Postgres `OWNER TO role` form, which sets OwnerKeyword / Owner and leaves NewName empty.
    public SyntaxToken? RenameKeyword { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> NewName { get; init; }
    public SyntaxToken? OwnerKeyword { get; init; }
    public SyntaxToken? Owner { get; init; }
}
