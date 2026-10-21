namespace QlParse;

// One rowset function argument: an expression or `*`, a named argument (`FORMAT = 'CSV'`, `TABLE = t AS a`), or a
// keyword-led one (`BULK 'file'`, `LANGUAGE 1033`).
public sealed class TsqlRowsetArgument
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Name { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public required Expression Value { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}
