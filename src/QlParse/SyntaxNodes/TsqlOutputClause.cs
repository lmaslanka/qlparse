namespace QlParse;

// `OUTPUT inserted.* | deleted.col | $action, ... [INTO {@table | table} [(columns)]]`.
public sealed class TsqlOutputClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OutputKeyword { get; init; }
    public required IReadOnlyList<SelectItem> Columns { get; init; }
    public SyntaxToken? IntoKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Target { get; init; }
    public SyntaxToken? TargetOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? TargetColumns { get; init; }
    public SyntaxToken? TargetCloseParen { get; init; }
}
