namespace QlParse;

// `FOR XML mode [, directive ...]`, `FOR JSON mode [, option ...]` or `FOR BROWSE`. The first option is the mode
// (RAW, AUTO, EXPLICIT, PATH); BROWSE has none.
public sealed class TsqlForClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken Format { get; init; }
    public IReadOnlyList<TsqlForOption> Options { get; init; } = [];
}
