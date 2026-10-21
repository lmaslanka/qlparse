namespace QlParse;

// GOTO label.
public sealed class TsqlGotoStatement : Query
{
    public required SyntaxToken GotoKeyword { get; init; }
    public required SyntaxToken Label { get; init; }
}
