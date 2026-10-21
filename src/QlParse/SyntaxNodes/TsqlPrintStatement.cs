namespace QlParse;

public sealed class TsqlPrintStatement : Query
{
    public required SyntaxToken PrintKeyword { get; init; }
    public required Expression Value { get; init; }
}
