namespace QlParse;

// CREATE of a non-rowstore index: `[CLUSTERED|NONCLUSTERED] COLUMNSTORE`, `[PRIMARY] XML`, `SPATIAL`, `JSON`
// or `VECTOR` (KindKeyword). A clustered columnstore index has no column list; ORDER (...) belongs to
// columnstore, USING XML INDEX ... FOR {VALUE|PATH|PROPERTY} to secondary XML, USING tessellation to
// spatial, and FOR ('path', ...) to JSON indexes.
public sealed class TsqlCreateSpecialIndexStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public SyntaxToken? PrimaryKeyword { get; init; }
    public SyntaxToken? ClusteredKeyword { get; init; }
    public required SyntaxToken KindKeyword { get; init; }
    public required SyntaxToken IndexKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<IndexColumn>? Columns { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? OrderKeyword { get; init; }
    public SyntaxToken? OrderOpen { get; init; }
    public IReadOnlyList<SyntaxToken>? OrderColumns { get; init; }
    public SyntaxToken? OrderClose { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public SyntaxToken? UsingXmlKeyword { get; init; }
    public SyntaxToken? UsingIndexKeyword { get; init; }
    public SyntaxToken? UsingIndexName { get; init; }
    public SyntaxToken? Tessellation { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? SecondaryKind { get; init; }
    public SyntaxToken? ForOpen { get; init; }
    public IReadOnlyList<SyntaxToken>? Paths { get; init; }
    public SyntaxToken? ForClose { get; init; }
    public SyntaxToken? WhereKeyword { get; init; }
    public Expression? Where { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlRelationalOptionList? Options { get; init; }
    public TsqlStorageClause? Storage { get; init; }
}
