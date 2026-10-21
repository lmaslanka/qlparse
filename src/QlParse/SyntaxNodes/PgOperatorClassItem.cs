namespace QlParse;

public sealed class PgOperatorClassItem
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Kind { get; init; }
    public SyntaxToken? Number { get; init; }
    public PgOperatorName? Operator { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<DataType> Types { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? Purpose { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? SortFamily { get; init; }
    public PgObjectReference? Function { get; init; }
    public DataType? StorageType { get; init; }
}
