namespace QlParse;

public sealed class DeclareHandlerStatement : Query
{
    public required SyntaxToken DeclareKeyword { get; init; }
    public required SyntaxToken HandlerType { get; init; }
    public required SyntaxToken HandlerKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required IReadOnlyList<ConditionValue> Conditions { get; init; }
    public required Query Action { get; init; }
}
