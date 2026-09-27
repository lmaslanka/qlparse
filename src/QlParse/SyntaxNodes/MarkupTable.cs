namespace QlParse;

public sealed class MarkupTable : TableSource
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public IReadOnlyList<SyntaxToken> Clauses { get; init; } = [];
    public IReadOnlyList<MarkupColumn> Columns { get; init; } = [];
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}
