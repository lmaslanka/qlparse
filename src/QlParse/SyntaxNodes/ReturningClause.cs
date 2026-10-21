namespace QlParse;

public sealed class ReturningClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ReturningKeyword { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<ReturningOutputAlias> OutputAliases { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public required IReadOnlyList<SelectItem> Items { get; init; }
}
