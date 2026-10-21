namespace QlParse;

public sealed class AnalyzeStatement : Query
{
    public required SyntaxToken AnalyzeKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<KeywordOption> Options { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public IReadOnlyList<MaintenanceTable> Tables { get; init; } = [];
}
