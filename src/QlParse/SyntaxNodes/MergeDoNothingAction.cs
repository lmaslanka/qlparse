namespace QlParse;

// Postgres `WHEN [NOT] MATCHED ... THEN DO NOTHING`.
public sealed class MergeDoNothingAction : MergeAction
{
    public required SyntaxToken DoKeyword { get; init; }
    public required SyntaxToken NothingKeyword { get; init; }
}
