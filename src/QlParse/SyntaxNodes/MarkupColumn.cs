namespace QlParse;

public sealed class MarkupColumn
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public DataType? Type { get; init; }
    public Expression? Path { get; init; }
    public Expression? Default { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? OrdinalityKeyword { get; init; }
    public IReadOnlyList<MarkupColumn> Nested { get; init; } = [];
}
