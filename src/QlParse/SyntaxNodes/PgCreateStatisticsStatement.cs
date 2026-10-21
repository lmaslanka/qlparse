namespace QlParse;

public sealed class PgCreateStatisticsStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken StatisticsKeyword { get; init; }
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken> Kinds { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public IReadOnlyList<Expression> Expressions { get; init; } = [];
    public required SyntaxToken FromKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Table { get; init; }
}
