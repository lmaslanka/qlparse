namespace QlParse;

public sealed class ExistenceCheck
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken IfKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken ExistsKeyword { get; init; }
}
