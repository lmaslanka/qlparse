namespace QlParse;

public sealed class SetConnectionStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken ConnectionKeyword { get; init; }
    public required SyntaxToken Target { get; init; }
}
