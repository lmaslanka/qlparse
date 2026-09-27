namespace QlParse;

public sealed class TableConstraint : TableElement
{
    public SyntaxToken? ConstraintKeyword { get; init; }
    public SyntaxToken? ConstraintName { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? NullKeyword { get; init; }
    public SyntaxToken? UniqueKeyword { get; init; }
    public SyntaxToken? NullsKeyword { get; init; }
    public SyntaxToken? NullsNotKeyword { get; init; }
    public SyntaxToken? DistinctKeyword { get; init; }
    public SyntaxToken? PrimaryKeyword { get; init; }
    public SyntaxToken? KeyKeyword { get; init; }
    public SyntaxToken? ForeignKeyword { get; init; }
    public SyntaxToken? ColumnsOpen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnsClose { get; init; }
    public SyntaxToken? ReferencesKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? ReferenceTable { get; init; }
    public SyntaxToken? ReferenceOpen { get; init; }
    public IReadOnlyList<SyntaxToken>? ReferenceColumns { get; init; }
    public SyntaxToken? ReferenceClose { get; init; }
    public SyntaxToken? MatchKeyword { get; init; }
    public SyntaxToken? MatchType { get; init; }
    public IReadOnlyList<ReferentialAction> Actions { get; init; } = [];
    public SyntaxToken? CheckKeyword { get; init; }
    public SyntaxToken? CheckOpen { get; init; }
    public Expression? Check { get; init; }
    public SyntaxToken? CheckClose { get; init; }
    public SyntaxToken? DeferrableNotKeyword { get; init; }
    public SyntaxToken? DeferrableKeyword { get; init; }
    public SyntaxToken? InitiallyKeyword { get; init; }
    public SyntaxToken? InitiallyWhen { get; init; }
}
