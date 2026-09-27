namespace QlParse;

public sealed class OnConstraint : JoinConstraint
{
    public required SyntaxToken OnKeyword { get; init; }
    public required Expression Condition { get; init; }
}
