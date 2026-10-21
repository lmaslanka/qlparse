namespace QlParse;

public sealed class PgDropStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> ObjectKind { get; init; }
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public required IReadOnlyList<PgObjectReference> Objects { get; init; }
    public SyntaxToken? Behavior { get; init; }
}
