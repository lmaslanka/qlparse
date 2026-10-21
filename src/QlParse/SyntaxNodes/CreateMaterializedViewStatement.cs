namespace QlParse;

public sealed class CreateMaterializedViewStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken MaterializedKeyword { get; init; }
    public required SyntaxToken ViewKeyword { get; init; }
    public ExistenceCheck? IfNotExists { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? ColumnsOpen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnsClose { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public SyntaxToken? Method { get; init; }
    public SyntaxToken? OptionsWithKeyword { get; init; }
    public StorageParameterList? Options { get; init; }
    public SyntaxToken? TablespaceKeyword { get; init; }
    public SyntaxToken? Tablespace { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required Query Query { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? DataKeyword { get; init; }
}
