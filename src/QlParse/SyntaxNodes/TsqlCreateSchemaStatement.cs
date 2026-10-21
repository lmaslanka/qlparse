namespace QlParse;

// CREATE SCHEMA {name [AUTHORIZATION owner] | AUTHORIZATION owner} [element ...], where each element is a
// CREATE TABLE, CREATE VIEW, GRANT, DENY or REVOKE statement.
public sealed class TsqlCreateSchemaStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public SyntaxToken? Name { get; init; }
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Owner { get; init; }
    public IReadOnlyList<Query> Elements { get; init; } = [];
}
