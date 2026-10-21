namespace QlParse;

public sealed class PgCreateSubscriptionStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken SubscriptionKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken ConnectionKeyword { get; init; }
    public required SyntaxToken Connection { get; init; }
    public required SyntaxToken PublicationKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Publications { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public PgDefinitionList? Definition { get; init; }
}
