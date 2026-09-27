namespace QlParse;

public sealed class DeclareSectionStatement : Query
{
    public required SyntaxToken BeginOrEnd { get; init; }
    public required SyntaxToken DeclareKeyword { get; init; }
    public required SyntaxToken SectionKeyword { get; init; }
}
