namespace QlParse;

// A SHORTEST_PATH arbitrary-length group: `(elements)+` or `(elements){1,n}`.
public sealed class TsqlGraphGroup : TsqlGraphElement
{
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<TsqlGraphElement> Elements { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public required IReadOnlyList<SyntaxToken> Quantifier { get; init; }
}
