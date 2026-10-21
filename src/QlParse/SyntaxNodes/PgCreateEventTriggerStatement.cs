namespace QlParse;

public sealed class PgCreateEventTriggerStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken EventKeyword { get; init; }
    public required SyntaxToken TriggerKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required SyntaxToken Event { get; init; }
    public SyntaxToken? WhenKeyword { get; init; }
    public IReadOnlyList<PgEventTriggerFilter> Filters { get; init; } = [];
    public required PgExecuteFunctionClause Execute { get; init; }
}
