namespace QlParse;

public sealed class PlpgsqlLabel
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OpenToken { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken CloseToken { get; init; }
}
