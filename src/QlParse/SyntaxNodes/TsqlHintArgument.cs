namespace QlParse;

// One argument of a hint: `ix`, `'HINT_NAME'`, `@p = 1` (EqualsToken + Assigned) or `@p UNKNOWN` (Modifier).
public sealed class TsqlHintArgument
{
    public required SourceSpan Span { get; init; }
    public required Expression Value { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public Expression? Assigned { get; init; }
    public SyntaxToken? Modifier { get; init; }
}
