namespace QlParse;

public sealed class RefIsClause : TableElement
{
    public required SyntaxToken RefKeyword { get; init; }
    public required SyntaxToken IsKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? Generation { get; init; }
    public SyntaxToken? GeneratedKeyword { get; init; }
}
