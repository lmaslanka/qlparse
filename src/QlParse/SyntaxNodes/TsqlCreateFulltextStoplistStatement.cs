namespace QlParse;

// `CREATE FULLTEXT STOPLIST name [FROM {[db.]source_stoplist | SYSTEM STOPLIST}] [AUTHORIZATION owner]`.
public sealed class TsqlCreateFulltextStoplistStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken FulltextKeyword { get; init; }
    public required SyntaxToken StoplistKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Source { get; init; } = [];
    public SyntaxToken? SystemKeyword { get; init; }
    public SyntaxToken? SystemStoplistKeyword { get; init; }
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Owner { get; init; }
}
