namespace QlParse;

// `SETUSER ['user' [WITH NORESET]]`.
public sealed class TsqlSetUserStatement : Query
{
    public required SyntaxToken SetUserKeyword { get; init; }
    public SyntaxToken? User { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? NoResetKeyword { get; init; }
}
