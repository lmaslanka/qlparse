namespace QlParse;

public sealed class PgTypeAttribute
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Percent { get; init; }
    public required SyntaxToken Attribute { get; init; }
}
