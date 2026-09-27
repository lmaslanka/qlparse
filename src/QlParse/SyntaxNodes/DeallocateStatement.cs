namespace QlParse;

public sealed class DeallocateStatement : Query
{
    public required SyntaxToken DeallocateKeyword { get; init; }
    public required SyntaxToken Kind { get; init; }
    public required SyntaxToken Name { get; init; }
}
