namespace QlParse;

public sealed class CreateTableStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public SyntaxToken? Scope { get; init; }
    public SyntaxToken? TemporaryKeyword { get; init; }
    public required SyntaxToken TableKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? OfKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? TypeName { get; init; }
    public SyntaxToken? UnderKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Supertable { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<TableElement>? Elements { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? AsColumnsOpen { get; init; }
    public IReadOnlyList<SyntaxToken>? AsColumns { get; init; }
    public SyntaxToken? AsColumnsClose { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public Query? Query { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? DataKeyword { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public SyntaxToken? CommitKeyword { get; init; }
    public SyntaxToken? CommitAction { get; init; }
    public SyntaxToken? RowsKeyword { get; init; }
    public SyntaxToken? SystemKeyword { get; init; }
    public SyntaxToken? VersioningKeyword { get; init; }
    public SyntaxToken? UnloggedKeyword { get; init; }
    public ExistenceCheck? IfNotExists { get; init; }
    public PartitionOfClause? PartitionOf { get; init; }
    public SyntaxToken? InheritsKeyword { get; init; }
    public SyntaxToken? InheritsOpen { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>>? Inherits { get; init; }
    public SyntaxToken? InheritsClose { get; init; }
    public PartitionByClause? PartitionBy { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public SyntaxToken? AccessMethod { get; init; }
    public SyntaxToken? StorageWithKeyword { get; init; }
    public StorageParameterList? StorageParameters { get; init; }
    public SyntaxToken? WithoutKeyword { get; init; }
    public SyntaxToken? OidsKeyword { get; init; }
    public SyntaxToken? TablespaceKeyword { get; init; }
    public SyntaxToken? Tablespace { get; init; }
}
