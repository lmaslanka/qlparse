namespace QlParse;

// `CREATE PARTITION SCHEME name AS PARTITION function [ALL] TO (filegroup, ...)`.
public sealed class TsqlCreatePartitionSchemeStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken PartitionKeyword { get; init; }
    public required SyntaxToken SchemeKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required SyntaxToken AsPartitionKeyword { get; init; }
    public required SyntaxToken FunctionName { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> FileGroups { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
