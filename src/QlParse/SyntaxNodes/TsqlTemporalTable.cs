namespace QlParse;

// `table FOR SYSTEM_TIME CONTAINED IN (start, end) [[AS] alias]`; the other FOR SYSTEM_TIME forms are the ISO
// SystemTimeClause of TableReference.
public sealed class TsqlTemporalTable : TableSource
{
    public required TableSource Table { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken SystemTimeKeyword { get; init; }
    public required SyntaxToken ContainedKeyword { get; init; }
    public required SyntaxToken InKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Start { get; init; }
    public required SyntaxToken Comma { get; init; }
    public required Expression End { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}
