namespace QlParse;

public sealed class MergeUpdateAction : MergeAction
{
    public required SyntaxToken UpdateKeyword { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required IReadOnlyList<SetClause> Assignments { get; init; }
}
