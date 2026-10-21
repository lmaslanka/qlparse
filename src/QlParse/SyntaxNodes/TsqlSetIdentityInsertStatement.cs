namespace QlParse;

// SET IDENTITY_INSERT table {ON | OFF}.
public sealed class TsqlSetIdentityInsertStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken IdentityInsertKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Table { get; init; }
    public required SyntaxToken Toggle { get; init; }
}
