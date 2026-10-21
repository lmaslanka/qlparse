namespace QlParse;

public sealed class ReassignOwnedStatement : Query
{
    public required SyntaxToken ReassignKeyword { get; init; }
    public required SyntaxToken OwnedKeyword { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Roles { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required SyntaxToken NewRole { get; init; }
}
