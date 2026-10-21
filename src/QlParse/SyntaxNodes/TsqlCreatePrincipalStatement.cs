namespace QlParse;

// CREATE {LOGIN | USER | ROLE | SERVER ROLE | APPLICATION ROLE} name [AUTHORIZATION owner]
// [{FOR | FROM | WITHOUT} source] [WITH option, ...]. SourceKeywords is e.g. `FROM WINDOWS`, `FOR LOGIN`, `WITHOUT LOGIN`,
// `FROM EXTERNAL PROVIDER` or `FROM ASYMMETRIC KEY`, and Source the login, certificate or key name when the source has one.
public sealed class TsqlCreatePrincipalStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> KindKeywords { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Owner { get; init; }
    public IReadOnlyList<SyntaxToken> SourceKeywords { get; init; } = [];
    public SyntaxToken? Source { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public IReadOnlyList<TsqlObjectOption> Options { get; init; } = [];
}
