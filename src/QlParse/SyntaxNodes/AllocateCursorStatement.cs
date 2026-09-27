namespace QlParse;

public sealed class AllocateCursorStatement : Query
{
    public required SyntaxToken AllocateKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? Sensitivity { get; init; }
    public SyntaxToken? NoScroll { get; init; }
    public SyntaxToken? Scroll { get; init; }
    public required SyntaxToken CursorKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken Statement { get; init; }
}
