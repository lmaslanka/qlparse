namespace QlParse;

public sealed class QualifiedOperatorExpression : Expression
{
    public Expression? Left { get; init; }
    public required SyntaxToken OperatorKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> SchemaParts { get; init; }
    public required SyntaxToken Operator { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public required Expression Right { get; init; }
}
