namespace QlParse;

// One BULK INSERT option: `FIELDTERMINATOR = ','`, `TABLOCK`, `ORDER (col [ASC | DESC], ...)`.
public sealed class TsqlBulkInsertOption
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public Expression? Value { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<OrderByItem> Order { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}
