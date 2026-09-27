namespace QlParse;

public sealed class MergeStatement : Query
{
    public required SyntaxToken MergeKeyword { get; init; }
    public required SyntaxToken IntoKeyword { get; init; }
    public required TargetTable Target { get; init; }
    public required SyntaxToken UsingKeyword { get; init; }
    public required TableSource Source { get; init; }
    public required IReadOnlyList<JoinClause> SourceJoins { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required IReadOnlyList<MergeWhenClause> Whens { get; init; }
}
