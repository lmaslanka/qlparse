namespace QlParse;

// `CREATE SYNONYM name FOR [server.][database.][schema.]object`.
public sealed class TsqlCreateSynonymStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken SynonymKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Target { get; init; }
}
