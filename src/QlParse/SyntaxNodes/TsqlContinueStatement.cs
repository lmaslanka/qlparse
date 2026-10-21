namespace QlParse;

public sealed class TsqlContinueStatement : Query
{
    public required SyntaxToken ContinueKeyword { get; init; }
}
