namespace QlParse;

// One FOR XML / FOR JSON mode or directive: `PATH('row')`, `ROOT('r')`, `TYPE`, `ELEMENTS XSINIL`, `BINARY BASE64`,
// `INCLUDE_NULL_VALUES`, ...
public sealed class TsqlForOption
{
    public required SourceSpan Span { get; init; }
    public required IReadOnlyList<SyntaxToken> Keywords { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public Expression? Argument { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}
