namespace QlParse;

// `{CREATE | ALTER} [EXTERNAL] RESOURCE POOL name [WITH (option, ...)]`.
public sealed class TsqlResourcePoolStatement : Query
{
    public required SyntaxToken CreateOrAlterKeyword { get; init; }
    public SyntaxToken? ExternalKeyword { get; init; }
    public required SyntaxToken ResourceKeyword { get; init; }
    public required SyntaxToken PoolKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
}
