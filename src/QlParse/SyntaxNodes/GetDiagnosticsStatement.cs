namespace QlParse;

public sealed class GetDiagnosticsStatement : Query
{
    public required SyntaxToken GetKeyword { get; init; }
    public required SyntaxToken DiagnosticsKeyword { get; init; }
    public SyntaxToken? ConditionKeyword { get; init; }
    public Expression? ConditionNumber { get; init; }
    public required IReadOnlyList<DiagnosticsItem> Items { get; init; }
}
