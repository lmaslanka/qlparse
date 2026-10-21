namespace QlParse;

public sealed class AlterSequenceStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken SequenceKeyword { get; init; }
    public ExistenceCheck? IfExists { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public DataType? DataType { get; init; }
    public IReadOnlyList<SequenceOption> Options { get; init; } = [];
    public AlterTableAction? Action { get; init; }
}
