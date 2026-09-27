namespace QlParse;

public sealed class TableFunction : TableSource
{
    public SyntaxToken? TableKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> NameParts { get; init; } = [];
    public required SyntaxToken OpenParen { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public Query? Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
}
