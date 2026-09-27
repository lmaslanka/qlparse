namespace QlParse;

public sealed class MergeInsertAction : MergeAction
{
    public required SyntaxToken InsertKeyword { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
    public OverrideClause? Override { get; init; }
    public required SyntaxToken ValuesKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Values { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
