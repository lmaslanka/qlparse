namespace QlParse;

public sealed class PgCreateTypeStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken TypeKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? KindKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken> Labels { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public PgDefinitionList? Definition { get; init; }
}
