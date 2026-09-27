namespace QlParse;

public sealed class CaseStatement : Query
{
    public required SyntaxToken CaseKeyword { get; init; }
    public Expression? Operand { get; init; }
    public required IReadOnlyList<CaseStatementWhen> Whens { get; init; }
    public SyntaxToken? ElseKeyword { get; init; }
    public IReadOnlyList<Query> ElseStatements { get; init; } = [];
    public required SyntaxToken EndKeyword { get; init; }
    public required SyntaxToken EndCaseKeyword { get; init; }
}
