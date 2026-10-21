namespace QlParse;

// `{CREATE | ALTER} EVENT SESSION name ON {SERVER | DATABASE} [event_or_target, ...] [WITH (option, ...)]`
// and `ALTER EVENT SESSION name ON SERVER STATE = {START | STOP}`.
public sealed class TsqlEventSessionStatement : Query
{
    public required SyntaxToken CreateOrAlterKeyword { get; init; }
    public required SyntaxToken EventKeyword { get; init; }
    public required SyntaxToken SessionKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required SyntaxToken Scope { get; init; }
    public IReadOnlyList<TsqlEventSessionItem> Items { get; init; } = [];
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
    public TsqlOption? State { get; init; }
}
