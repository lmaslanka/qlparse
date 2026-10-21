namespace QlParse;

public sealed class PgCreateForeignTableStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken ForeignKeyword { get; init; }
    public required SyntaxToken TableKeyword { get; init; }
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<PgForeignColumn> Columns { get; init; } = [];

    // Table-level `[CONSTRAINT name] CHECK (...)` elements of the column list, in source order.
    public IReadOnlyList<TableConstraint> Constraints { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? InheritsKeyword { get; init; }
    public SyntaxToken? InheritsOpenParen { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>> Inherits { get; init; } = [];
    public SyntaxToken? InheritsCloseParen { get; init; }
    public PgForeignTablePartition? Partition { get; init; }
    public required SyntaxToken ServerKeyword { get; init; }
    public required SyntaxToken Server { get; init; }
    public PgOptionList? Options { get; init; }
}
