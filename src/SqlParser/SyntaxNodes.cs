namespace SqlParser;

internal abstract class Expression;

internal sealed class IdentifierExpression : Expression
{
    public required SyntaxToken Identifier { get; init; }
}

internal sealed class LiteralExpression : Expression
{
    public required SyntaxToken Literal { get; init; }
}

internal sealed class BinaryExpression : Expression
{
    public required Expression Left { get; init; }
    public required SyntaxToken OperatorToken { get; init; }
    public required Expression Right { get; init; }
}

internal sealed class WhereClause
{
    public required SyntaxToken WhereKeyword { get; init; }
    public required Expression Expression { get; init; }
}

internal sealed class SelectStatement
{
    public required SyntaxToken SelectKeyword { get; init; }
    public required IReadOnlyList<Expression> SelectList { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required SyntaxToken TableName { get; init; }
    public WhereClause? Where { get; init; }
}
