namespace QlParse;

public sealed class GraphTable : TableSource
{
    public required SyntaxToken GraphTableKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> GraphName { get; init; }
    public required SyntaxToken MatchKeyword { get; init; }
    public required IReadOnlyList<GraphElement> Pattern { get; init; }
    public SyntaxToken? WhereKeyword { get; init; }
    public Expression? Where { get; init; }
    public SyntaxToken? ColumnsKeyword { get; init; }
    public IReadOnlyList<Expression> Columns { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}
