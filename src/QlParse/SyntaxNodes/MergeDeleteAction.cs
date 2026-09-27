namespace QlParse;

public sealed class MergeDeleteAction : MergeAction
{
    public required SyntaxToken DeleteKeyword { get; init; }
}
