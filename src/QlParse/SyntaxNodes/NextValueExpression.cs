namespace QlParse;

public sealed class NextValueExpression : Expression
{
    public required SyntaxToken NextKeyword { get; init; }
    public required SyntaxToken ValueKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> NameParts { get; init; }
}
