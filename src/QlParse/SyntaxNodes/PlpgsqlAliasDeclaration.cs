namespace QlParse;

public sealed class PlpgsqlAliasDeclaration : Query
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken AliasKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken Target { get; init; }
}
