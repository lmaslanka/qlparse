namespace QlParse;

public sealed class PgCreateAggregateStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public SyntaxToken? OrKeyword { get; init; }
    public SyntaxToken? ReplaceKeyword { get; init; }
    public required SyntaxToken AggregateKeyword { get; init; }
    public required PgObjectReference Signature { get; init; }
    public required PgDefinitionList Definition { get; init; }
}
