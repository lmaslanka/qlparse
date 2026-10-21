namespace QlParse;

// `CREATE SEARCH PROPERTY LIST name [FROM [db.]source_list] [AUTHORIZATION owner]`.
public sealed class TsqlCreateSearchPropertyListStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken SearchKeyword { get; init; }
    public required SyntaxToken PropertyKeyword { get; init; }
    public required SyntaxToken ListKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Source { get; init; } = [];
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Owner { get; init; }
}
