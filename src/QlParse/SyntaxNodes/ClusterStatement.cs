namespace QlParse;

public sealed class ClusterStatement : Query
{
    public required SyntaxToken ClusterKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<KeywordOption> Options { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Table { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public SyntaxToken? Index { get; init; }

    // The pre-8.3 `CLUSTER index ON table` form: Index comes first, then ON and Table.
    public SyntaxToken? OnKeyword { get; init; }
}
