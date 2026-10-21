namespace QlParse;

// @name [AS] {type | CURSOR} [VARYING] [[NOT] NULL] [= default] [OUT|OUTPUT] [READONLY].
public sealed class TsqlParameter
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public DataType? DataType { get; init; }
    public SyntaxToken? CursorKeyword { get; init; }
    public SyntaxToken? VaryingKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? NullKeyword { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public Expression? Default { get; init; }
    public SyntaxToken? OutputKeyword { get; init; }
    public SyntaxToken? ReadonlyKeyword { get; init; }
}
