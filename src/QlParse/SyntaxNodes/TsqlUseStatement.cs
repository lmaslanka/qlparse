namespace QlParse;

public sealed class TsqlUseStatement : Query
{
    public required SyntaxToken UseKeyword { get; init; }
    public required SyntaxToken Database { get; init; }
}
