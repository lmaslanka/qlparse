namespace QlParse;

public sealed class CreateRuleStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public SyntaxToken? OrKeyword { get; init; }
    public SyntaxToken? ReplaceKeyword { get; init; }
    public required SyntaxToken RuleKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required SyntaxToken Event { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Table { get; init; }
    public SyntaxToken? WhereKeyword { get; init; }
    public Expression? Where { get; init; }
    public required SyntaxToken DoKeyword { get; init; }
    public SyntaxToken? Kind { get; init; }
    public SyntaxToken? NothingKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<Query> Commands { get; init; } = [];
    public IReadOnlyList<SyntaxToken> Semicolons { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}
