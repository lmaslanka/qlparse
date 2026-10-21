namespace QlParse;

public sealed class TsqlBreakStatement : Query
{
    public required SyntaxToken BreakKeyword { get; init; }
}
