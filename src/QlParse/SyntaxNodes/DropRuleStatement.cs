namespace QlParse;

public sealed class DropRuleStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken RuleKeyword { get; init; }
    public ExistenceCheck? IfExists { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Table { get; init; }
    public SyntaxToken? Behavior { get; init; }
}
