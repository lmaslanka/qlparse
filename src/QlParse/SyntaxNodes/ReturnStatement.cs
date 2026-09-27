namespace QlParse;

public sealed class ReturnStatement : Query
{
    public required SyntaxToken ReturnKeyword { get; init; }
    public required Expression Value { get; init; }
}
