namespace QlParse;

public sealed class SetConfigurationStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public SyntaxToken? ScopeKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    // `=`, TO, or the FROM of `FROM CURRENT`.
    public required SyntaxToken AssignmentToken { get; init; }
    public SyntaxToken? CurrentKeyword { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public IReadOnlyList<OptionValue> Values { get; init; } = [];
}
