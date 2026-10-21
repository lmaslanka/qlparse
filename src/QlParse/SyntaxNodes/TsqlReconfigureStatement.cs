namespace QlParse;

// `RECONFIGURE [WITH OVERRIDE]`.
public sealed class TsqlReconfigureStatement : Query
{
    public required SyntaxToken ReconfigureKeyword { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? OverrideKeyword { get; init; }
}
