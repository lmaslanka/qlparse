namespace QlParse;

public sealed class SetCollationStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken CollationKeyword { get; init; }
    public SyntaxToken? Value { get; init; }
    public IReadOnlyList<SyntaxToken>? Name { get; init; }
}
