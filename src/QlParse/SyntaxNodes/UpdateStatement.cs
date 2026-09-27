namespace QlParse;

public sealed class UpdateStatement : Query
{
    public required SyntaxToken UpdateKeyword { get; init; }
    public required TargetTable Target { get; init; }
    public PortionClause? Portion { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required IReadOnlyList<SetClause> Assignments { get; init; }
    public WhereClause? Where { get; init; }
    public PositionedClause? Positioned { get; init; }
}
