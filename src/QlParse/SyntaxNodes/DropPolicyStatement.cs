namespace QlParse;

public sealed class DropPolicyStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken PolicyKeyword { get; init; }
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Table { get; init; }
    public SyntaxToken? Behavior { get; init; }
}
