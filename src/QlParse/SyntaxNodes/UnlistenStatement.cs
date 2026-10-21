namespace QlParse;

public sealed class UnlistenStatement : Query
{
    public required SyntaxToken UnlistenKeyword { get; init; }
    public required SyntaxToken Channel { get; init; }
}
