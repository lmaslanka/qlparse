namespace QlParse;

// SET option[, option ...] {ON | OFF | value}: session options (NOCOUNT, XACT_ABORT, DATEFIRST, LANGUAGE ...)
// and the diagnostics options (SET STATISTICS {IO|TIME|XML|PROFILE}, SHOWPLAN_*, NOEXEC, PARSEONLY).
// StatisticsKeyword is the STATISTICS of SET STATISTICS IO, TIME ON; Options then holds IO and TIME.
// Exactly one of Toggle (ON or OFF) and Value is set.
public sealed class TsqlSetOptionStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public SyntaxToken? StatisticsKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Options { get; init; }
    public SyntaxToken? Toggle { get; init; }
    public Expression? Value { get; init; }
}
