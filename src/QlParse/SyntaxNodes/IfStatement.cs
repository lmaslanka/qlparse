namespace QlParse;

public sealed class IfStatement : Query
{
    public required SyntaxToken IfKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required SyntaxToken ThenKeyword { get; init; }
    public IReadOnlyList<Query> ThenStatements { get; init; } = [];
    public IReadOnlyList<ElseIfClause> ElseIfs { get; init; } = [];
    public SyntaxToken? ElseKeyword { get; init; }
    public IReadOnlyList<Query> ElseStatements { get; init; } = [];
    public required SyntaxToken EndKeyword { get; init; }
    public required SyntaxToken EndIfKeyword { get; init; }
}
