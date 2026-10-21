namespace QlParse;

public sealed class ExplainStatement : Query
{
    public required SyntaxToken ExplainKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<KeywordOption> Options { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public required Query Statement { get; init; }
}
