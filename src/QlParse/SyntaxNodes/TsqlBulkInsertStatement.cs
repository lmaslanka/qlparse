namespace QlParse;

// `BULK INSERT table FROM 'file' [WITH (option [= value], ...)]`.
public sealed class TsqlBulkInsertStatement : Query
{
    public required SyntaxToken BulkKeyword { get; init; }
    public required SyntaxToken InsertKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required SyntaxToken Source { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<TsqlBulkInsertOption> Options { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}
