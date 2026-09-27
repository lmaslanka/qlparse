namespace QlParse;

public sealed class SetNamesStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken NamesKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
    public SyntaxToken? CollateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Collation { get; init; }
}
