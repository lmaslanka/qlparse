namespace QlParse;

// Legacy CREATE DEFAULT name AS constant_expression / CREATE RULE name AS condition_expression;
// KindKeyword is DEFAULT or RULE.
public sealed class TsqlCreateDefaultOrRuleStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken KindKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required Expression Expression { get; init; }
}
