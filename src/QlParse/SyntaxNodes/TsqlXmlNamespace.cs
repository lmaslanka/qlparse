namespace QlParse;

// One XMLNAMESPACES declaration: `'uri' AS prefix` or `DEFAULT 'uri'`.
public sealed class TsqlXmlNamespace
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public required SyntaxToken Uri { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Prefix { get; init; }
}
