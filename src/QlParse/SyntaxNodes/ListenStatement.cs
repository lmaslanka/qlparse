namespace QlParse;

public sealed class ListenStatement : Query
{
    public required SyntaxToken ListenKeyword { get; init; }
    public required SyntaxToken Channel { get; init; }
}
