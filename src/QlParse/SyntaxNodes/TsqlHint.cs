namespace QlParse;

// A table hint (`NOLOCK`, `INDEX(ix)`, `INDEX = ix`, `SPATIAL_WINDOW_MAX_CELLS = 512`) or a query hint
// (`RECOMPILE`, `MAXDOP 4`, `HASH GROUP`, `OPTIMIZE FOR (@p = 1)`, `USE HINT ('...')`, `LABEL = 'x'`): one or more
// words, then an optional `= value`, bare value, or parenthesized argument list.
public sealed class TsqlHint
{
    public required SourceSpan Span { get; init; }
    public required IReadOnlyList<SyntaxToken> Keywords { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public Expression? Value { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<TsqlHintArgument> Arguments { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}
