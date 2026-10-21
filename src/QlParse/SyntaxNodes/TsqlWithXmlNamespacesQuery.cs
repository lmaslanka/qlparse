namespace QlParse;

// `WITH XMLNAMESPACES ('uri' AS prefix, DEFAULT 'uri') [, cte AS (...)] statement`.
public sealed class TsqlWithXmlNamespacesQuery : Query
{
    public required SyntaxToken WithKeyword { get; init; }
    public required SyntaxToken XmlNamespacesKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<TsqlXmlNamespace> Namespaces { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? Comma { get; init; }
    public IReadOnlyList<CommonTableExpression> Ctes { get; init; } = [];
    public required Query Query { get; init; }
}
