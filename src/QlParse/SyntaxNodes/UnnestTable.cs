namespace QlParse;

public sealed class UnnestTable : TableSource
{
    public required SyntaxToken UnnestKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Expressions { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? OrdinalityKeyword { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
}
