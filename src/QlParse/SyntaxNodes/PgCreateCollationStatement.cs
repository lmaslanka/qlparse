namespace QlParse;

public sealed class PgCreateCollationStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken CollationKeyword { get; init; }
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public PgDefinitionList? Definition { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Source { get; init; }
}
