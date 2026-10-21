namespace QlParse;

// One RECEIVE column: `expression [[AS] alias]`, `alias = expression` or `@variable = expression`.
public sealed class TsqlReceiveColumn
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Target { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public required Expression Expression { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}
