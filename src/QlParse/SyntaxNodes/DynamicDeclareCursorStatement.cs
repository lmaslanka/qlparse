namespace QlParse;

public sealed class DynamicDeclareCursorStatement : Query
{
    public required SyntaxToken DeclareKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? Sensitivity { get; init; }
    public SyntaxToken? NoScroll { get; init; }
    public SyntaxToken? Scroll { get; init; }
    public required SyntaxToken CursorKeyword { get; init; }
    public SyntaxToken? HoldWith { get; init; }
    public SyntaxToken? Hold { get; init; }
    public SyntaxToken? ReturnWith { get; init; }
    public SyntaxToken? ReturnKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken Statement { get; init; }
}
