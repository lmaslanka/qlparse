namespace QlParse;

// Dynamic data masking: `MASKED WITH (FUNCTION = 'partial(1, "xx", 1)')`, or a bare `MASKED` after DROP.
public sealed class TsqlMaskingClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken MaskedKeyword { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public SyntaxToken? FunctionKeyword { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public SyntaxToken? Function { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}
