namespace SqlParser;

internal abstract class Query;

internal sealed class SetOperation : Query
{
    public required Query Left { get; init; }
    public required SyntaxToken Operator { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public required Query Right { get; init; }
}

internal sealed class ParenQuery : Query
{
    public required SyntaxToken OpenParen { get; init; }
    public required Query Inner { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

internal sealed class WithQuery : Query
{
    public required SyntaxToken WithKeyword { get; init; }
    public SyntaxToken? RecursiveKeyword { get; init; }
    public required IReadOnlyList<CommonTableExpression> Ctes { get; init; }
    public required Query Query { get; init; }
}

internal sealed class CommonTableExpression
{
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required SyntaxToken OpenQuery { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseQuery { get; init; }
}

internal abstract class Expression;

internal sealed class DataType
{
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public SyntaxToken? Precision { get; init; }
    public SyntaxToken? Scale { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}

internal sealed class CastExpression : Expression
{
    public required SyntaxToken CastKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required DataType Type { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

internal sealed class ColonCastExpression : Expression
{
    public required Expression Expression { get; init; }
    public required SyntaxToken DoubleColon { get; init; }
    public required DataType Type { get; init; }
}

internal sealed class ArrayExpression : Expression
{
    public required SyntaxToken ArrayKeyword { get; init; }
    public required SyntaxToken OpenBracket { get; init; }
    public required IReadOnlyList<Expression> Elements { get; init; }
    public required SyntaxToken CloseBracket { get; init; }
}

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

internal sealed class MemberAccessExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken Dot { get; init; }
    public required SyntaxToken Member { get; init; }
}

internal sealed class BetweenExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken BetweenKeyword { get; init; }
    public required Expression Lower { get; init; }
    public required SyntaxToken AndKeyword { get; init; }
    public required Expression Upper { get; init; }
}

internal sealed class InExpression : Expression
{
    public required Expression Target { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken InKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Values { get; init; }
    public Query? Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

internal sealed class LikeExpression : Expression
{
    public required Expression Target { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken LikeKeyword { get; init; }
    public required Expression Pattern { get; init; }
}

internal sealed class IsNullExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken IsKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken NullKeyword { get; init; }
}

internal sealed class UnaryExpression : Expression
{
    public required SyntaxToken OperatorToken { get; init; }
    public required Expression Expression { get; init; }
}

internal sealed class NotExpression : Expression
{
    public required SyntaxToken NotKeyword { get; init; }
    public required Expression Expression { get; init; }
}

internal sealed class FunctionCallExpression : Expression
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Arguments { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

internal sealed class HavingClause
{
    public required SyntaxToken HavingKeyword { get; init; }
    public required Expression Expression { get; init; }
}

internal sealed class OffsetClause
{
    public required SyntaxToken OffsetKeyword { get; init; }
    public required Expression Count { get; init; }
}

internal sealed class ParenExpression : Expression
{
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Inner { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

internal sealed class ScalarSubqueryExpression : Expression
{
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

internal sealed class ExistsExpression : Expression
{
    public required SyntaxToken ExistsKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

internal sealed class QuantifiedSubqueryExpression : Expression
{
    public required Expression Left { get; init; }
    public required SyntaxToken OperatorToken { get; init; }
    public required SyntaxToken Quantifier { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

internal sealed class WhenClause
{
    public required SyntaxToken WhenKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required SyntaxToken ThenKeyword { get; init; }
    public required Expression Result { get; init; }
}

internal sealed class CaseExpression : Expression
{
    public required SyntaxToken CaseKeyword { get; init; }
    public Expression? Operand { get; init; }
    public required IReadOnlyList<WhenClause> Arms { get; init; }
    public SyntaxToken? ElseKeyword { get; init; }
    public Expression? ElseResult { get; init; }
    public required SyntaxToken EndKeyword { get; init; }
}

internal sealed class StarExpression : Expression
{
    public required SyntaxToken Star { get; init; }
}

internal sealed class QualifiedStarExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken Dot { get; init; }
    public required SyntaxToken Star { get; init; }
}

internal sealed class SelectItem
{
    public required Expression Expression { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}

internal abstract class TableSource;

internal sealed class TableReference : TableSource
{
    public required IReadOnlyList<SyntaxToken> NameParts { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}

internal sealed class DerivedTable : TableSource
{
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public required SyntaxToken Alias { get; init; }
}

internal sealed class GroupByClause
{
    public required SyntaxToken GroupKeyword { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required IReadOnlyList<Expression> Keys { get; init; }
}

internal sealed class OrderByItem
{
    public required Expression Expression { get; init; }
    public SyntaxToken? Direction { get; init; }
}

internal sealed class OrderByClause
{
    public required SyntaxToken OrderKeyword { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required IReadOnlyList<OrderByItem> Items { get; init; }
}

internal sealed class LimitClause
{
    public required SyntaxToken LimitKeyword { get; init; }
    public required Expression Count { get; init; }
}

internal abstract class JoinConstraint;

internal sealed class OnConstraint : JoinConstraint
{
    public required SyntaxToken OnKeyword { get; init; }
    public required Expression Condition { get; init; }
}

internal sealed class UsingConstraint : JoinConstraint
{
    public required SyntaxToken UsingKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> Columns { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

internal sealed class JoinClause
{
    public SyntaxToken? NaturalKeyword { get; init; }
    public SyntaxToken? JoinType { get; init; }
    public SyntaxToken? OuterKeyword { get; init; }
    public required SyntaxToken JoinKeyword { get; init; }
    public required TableSource Table { get; init; }
    public JoinConstraint? Constraint { get; init; }
}

internal sealed class WhereClause
{
    public required SyntaxToken WhereKeyword { get; init; }
    public required Expression Expression { get; init; }
}

internal sealed class SelectStatement : Query
{
    public required SyntaxToken SelectKeyword { get; init; }
    public SyntaxToken? DistinctKeyword { get; init; }
    public required IReadOnlyList<SelectItem> SelectList { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public TableSource? From { get; init; }
    public required IReadOnlyList<JoinClause> Joins { get; init; }
    public WhereClause? Where { get; init; }
    public GroupByClause? GroupBy { get; init; }
    public HavingClause? Having { get; init; }
    public OrderByClause? OrderBy { get; init; }
    public LimitClause? Limit { get; init; }
    public OffsetClause? Offset { get; init; }
}
