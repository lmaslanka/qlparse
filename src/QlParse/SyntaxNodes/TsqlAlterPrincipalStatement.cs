namespace QlParse;

// ALTER {LOGIN | USER | ROLE | SERVER ROLE | APPLICATION ROLE} name action, where ActionKeywords is
// `WITH` (followed by Options), `ENABLE`, `DISABLE`, `ADD MEMBER`, `DROP MEMBER`, `ADD CREDENTIAL` or `DROP CREDENTIAL` (followed by Member).
public sealed class TsqlAlterPrincipalStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> KindKeywords { get; init; }
    public required SyntaxToken Name { get; init; }
    public required IReadOnlyList<SyntaxToken> ActionKeywords { get; init; }
    public SyntaxToken? Member { get; init; }
    public IReadOnlyList<TsqlObjectOption> Options { get; init; } = [];
}
