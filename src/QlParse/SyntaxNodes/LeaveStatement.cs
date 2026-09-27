namespace QlParse;

public sealed class LeaveStatement : Query
{
    public required SyntaxToken LeaveKeyword { get; init; }
    public required SyntaxToken Label { get; init; }
}
