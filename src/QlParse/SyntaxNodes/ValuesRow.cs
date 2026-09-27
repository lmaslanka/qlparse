namespace QlParse;

public sealed class ValuesRow
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Values { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
