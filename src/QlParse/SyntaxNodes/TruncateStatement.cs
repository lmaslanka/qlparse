namespace QlParse;

public sealed class TruncateStatement : Query
{
    public required SyntaxToken TruncateKeyword { get; init; }
    public SyntaxToken? TableKeyword { get; init; }
    public SyntaxToken? OnlyKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public SyntaxToken? DescendantStar { get; init; }
    public SyntaxToken? RestartKeyword { get; init; }
    public SyntaxToken? IdentityKeyword { get; init; }
}
