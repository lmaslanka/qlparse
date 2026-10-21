namespace QlParse;

// `ALTER PARTITION SCHEME name NEXT USED [filegroup]`.
public sealed class TsqlAlterPartitionSchemeStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken PartitionKeyword { get; init; }
    public required SyntaxToken SchemeKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken NextKeyword { get; init; }
    public required SyntaxToken UsedKeyword { get; init; }
    public SyntaxToken? FileGroup { get; init; }
}
