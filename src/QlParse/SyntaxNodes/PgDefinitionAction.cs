namespace QlParse;

public sealed class PgDefinitionAction : PgAlterAction
{
    public SyntaxToken? Keyword { get; init; }
    public required PgDefinitionList Definition { get; init; }
}
