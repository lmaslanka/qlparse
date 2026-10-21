namespace QlParse;

public sealed class PgCreateOperatorStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken OperatorKeyword { get; init; }
    public required PgOperatorName Name { get; init; }
    public required PgDefinitionList Definition { get; init; }
}
