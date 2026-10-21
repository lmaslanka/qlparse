namespace QlParse;

public sealed class UpdateStatement : Query
{
    public required SyntaxToken UpdateKeyword { get; init; }
    public required TargetTable Target { get; init; }
    public PortionClause? Portion { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required IReadOnlyList<SetClause> Assignments { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public TableSource? From { get; init; }
    public IReadOnlyList<JoinClause> Joins { get; init; } = [];
    public IReadOnlyList<CommaFrom> ExtraFrom { get; init; } = [];
    public WhereClause? Where { get; init; }
    public PositionedClause? Positioned { get; init; }
    public ReturningClause? Returning { get; init; }
}
