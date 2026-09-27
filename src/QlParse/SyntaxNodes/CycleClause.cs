namespace QlParse;

public sealed class CycleClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken CycleKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Columns { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken MarkColumn { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required Expression MarkValue { get; init; }
    public required SyntaxToken DefaultKeyword { get; init; }
    public required Expression DefaultValue { get; init; }
    public required SyntaxToken UsingKeyword { get; init; }
    public required SyntaxToken PathColumn { get; init; }
}
