namespace QlParse;

// Postgres COPY. Inline data rows that follow `FROM STDIN` in a psql script are a client-side
// concern and are not parsed; the statement ends at its options / WHERE clause.
public sealed class CopyStatement : Query
{
    public required SyntaxToken CopyKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> TableName { get; init; } = [];
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
    public SyntaxToken? QueryOpenParen { get; init; }
    public Query? Query { get; init; }
    public SyntaxToken? QueryCloseParen { get; init; }
    public required SyntaxToken DirectionKeyword { get; init; }
    public SyntaxToken? ProgramKeyword { get; init; }
    public required SyntaxToken Target { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? OptionsOpenParen { get; init; }
    public IReadOnlyList<CopyOption> Options { get; init; } = [];
    public SyntaxToken? OptionsCloseParen { get; init; }
    public WhereClause? Where { get; init; }
}
