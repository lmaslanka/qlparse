namespace QlParse;

public sealed class DeleteStatement : Query
{
    public required SyntaxToken DeleteKeyword { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required TargetTable Target { get; init; }
    public PortionClause? Portion { get; init; }
    public WhereClause? Where { get; init; }
    public PositionedClause? Positioned { get; init; }
}
