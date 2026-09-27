namespace QlParse;

public sealed class WithQuery : Query
{
    public required SyntaxToken WithKeyword { get; init; }
    public SyntaxToken? RecursiveKeyword { get; init; }
    public SyntaxToken? RecursionLimit { get; init; }
    public required IReadOnlyList<CommonTableExpression> Ctes { get; init; }
    public required Query Query { get; init; }
}
