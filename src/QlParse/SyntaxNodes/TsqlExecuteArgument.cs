namespace QlParse;

// [@parameter =] {value | @variable [OUTPUT] | DEFAULT}. Exactly one of Value and DefaultKeyword is set.
public sealed class TsqlExecuteArgument
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Parameter { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public Expression? Value { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public SyntaxToken? OutputKeyword { get; init; }
}
