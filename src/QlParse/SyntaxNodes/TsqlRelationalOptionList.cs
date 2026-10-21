namespace QlParse;

// `(option, ...)`; the parentheses are absent in the legacy `WITH FILLFACTOR = n` form and in statistics
// `WITH FULLSCAN, NORECOMPUTE` lists.
public sealed class TsqlRelationalOptionList
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public required IReadOnlyList<TsqlRelationalOption> Options { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}
