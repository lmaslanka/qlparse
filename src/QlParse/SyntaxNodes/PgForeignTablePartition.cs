namespace QlParse;

public sealed class PgForeignTablePartition
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken PartitionKeyword { get; init; }
    public required SyntaxToken OfKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Parent { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? ValuesKeyword { get; init; }
    public SyntaxToken? BoundKind { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<Expression> Values { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public SyntaxToken? ToOpenParen { get; init; }
    public IReadOnlyList<Expression> ToValues { get; init; } = [];
    public SyntaxToken? ToCloseParen { get; init; }
    public PgDefinitionList? HashBound { get; init; }
}
