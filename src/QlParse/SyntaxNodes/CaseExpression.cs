namespace QlParse;

public sealed class CaseExpression : Expression
{
    public required SyntaxToken CaseKeyword { get; init; }
    public Expression? Operand { get; init; }
    public required IReadOnlyList<WhenClause> Arms { get; init; }
    public SyntaxToken? ElseKeyword { get; init; }
    public Expression? ElseResult { get; init; }
    public required SyntaxToken EndKeyword { get; init; }
}
