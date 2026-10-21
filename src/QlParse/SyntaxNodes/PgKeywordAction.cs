namespace QlParse;

public sealed class PgKeywordAction : PgAlterAction
{
    public required IReadOnlyList<SyntaxToken> Keywords { get; init; }
    public IReadOnlyList<SyntaxToken> Values { get; init; } = [];
    public SyntaxToken? WithKeyword { get; init; }
    public PgDefinitionList? Definition { get; init; }
}
