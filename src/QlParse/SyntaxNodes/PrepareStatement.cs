namespace QlParse;

public sealed class PrepareStatement : Query
{
    public required SyntaxToken PrepareKeyword { get; init; }
    public SyntaxToken? Scope { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required SyntaxToken Source { get; init; }
}
