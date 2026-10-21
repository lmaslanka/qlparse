namespace QlParse;

// One entry of a SQL Server table, index or constraint option list: `NAME [= value] [(nested options)]
// [ON PARTITIONS (...)]`. Value holds the scalar value's tokens (`ON`, `80`, `'x'`, `dbo.h`, `6 MONTHS`, `-1`).
// An entry of a bare value list (`BOUNDING_BOX = (0, 0, 10, 10)`) or a flag (`FULLSCAN`) has only Name.
public sealed class TsqlRelationalOption
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public IReadOnlyList<SyntaxToken> Value { get; init; } = [];
    public TsqlRelationalOptionList? Options { get; init; }
    public TsqlOnPartitionsClause? Partitions { get; init; }
}
