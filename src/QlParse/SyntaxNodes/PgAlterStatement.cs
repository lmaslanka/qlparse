namespace QlParse;

public sealed class PgAlterStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> ObjectKind { get; init; }
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public required PgObjectReference Target { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Table { get; init; }
    public required IReadOnlyList<PgAlterAction> Actions { get; init; }
}
