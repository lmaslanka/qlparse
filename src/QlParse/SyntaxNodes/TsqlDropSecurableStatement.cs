namespace QlParse;

// DROP {LOGIN | USER | ROLE | SCHEMA | DATABASE | CERTIFICATE | SYMMETRIC KEY | ...} [IF EXISTS] name [, ...];
// Names is empty for DROP MASTER KEY.
public sealed class TsqlDropSecurableStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> KindKeywords { get; init; }
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>> Names { get; init; } = [];
}
