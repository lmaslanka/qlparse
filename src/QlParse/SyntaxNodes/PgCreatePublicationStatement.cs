namespace QlParse;

public sealed class PgCreatePublicationStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken PublicationKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public SyntaxToken? TablesKeyword { get; init; }
    public IReadOnlyList<PgPublicationObject> Objects { get; init; } = [];
    public SyntaxToken? WithKeyword { get; init; }
    public PgDefinitionList? Definition { get; init; }
}
