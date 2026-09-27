namespace QlParse;

public sealed class IterateStatement : Query
{
    public required SyntaxToken IterateKeyword { get; init; }
    public required SyntaxToken Label { get; init; }
}
