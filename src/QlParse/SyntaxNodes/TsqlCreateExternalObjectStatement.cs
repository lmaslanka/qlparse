namespace QlParse;

// PolyBase and AI objects: CREATE EXTERNAL {DATA SOURCE | FILE FORMAT | MODEL} name [AUTHORIZATION owner] WITH (option, ...).
public sealed class TsqlCreateExternalObjectStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken ExternalKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> KindKeywords { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Owner { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required TsqlObjectOptionList Options { get; init; }
}
