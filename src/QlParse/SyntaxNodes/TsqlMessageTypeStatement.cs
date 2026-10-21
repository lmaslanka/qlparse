namespace QlParse;

// `{CREATE | ALTER} MESSAGE TYPE name [AUTHORIZATION owner]
// [VALIDATION = {NONE | EMPTY | WELL_FORMED_XML | VALID_XML WITH SCHEMA COLLECTION name}]`.
public sealed class TsqlMessageTypeStatement : Query
{
    public required SyntaxToken CreateOrAlterKeyword { get; init; }
    public required SyntaxToken MessageKeyword { get; init; }
    public required SyntaxToken TypeKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Owner { get; init; }
    public SyntaxToken? ValidationKeyword { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public SyntaxToken? Validation { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? SchemaKeyword { get; init; }
    public SyntaxToken? CollectionKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> SchemaCollection { get; init; } = [];
}
