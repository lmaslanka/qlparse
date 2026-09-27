namespace QlParse;

public sealed class CreateIndexStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public SyntaxToken? UniqueKeyword { get; init; }
    public SyntaxToken? ClusteredKeyword { get; init; }
    public required SyntaxToken IndexKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<IndexColumn> Columns { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? WhereKeyword { get; init; }
    public Expression? Where { get; init; }
}
