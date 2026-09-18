namespace QlParse;

public abstract class Query;

public sealed class ValuesRow
{
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Values { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class ValuesQuery : Query
{
    public required SyntaxToken ValuesKeyword { get; init; }
    public required IReadOnlyList<ValuesRow> Rows { get; init; }
}

public sealed class SetOperation : Query
{
    public required Query Left { get; init; }
    public required SyntaxToken Operator { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public SyntaxToken? CorrespondingKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public required Query Right { get; init; }
}

public sealed class ParenQuery : Query
{
    public required SyntaxToken OpenParen { get; init; }
    public required Query Inner { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class WithQuery : Query
{
    public required SyntaxToken WithKeyword { get; init; }
    public SyntaxToken? RecursiveKeyword { get; init; }
    public required IReadOnlyList<CommonTableExpression> Ctes { get; init; }
    public required Query Query { get; init; }
}

public sealed class CommonTableExpression
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

public abstract class Expression;

public sealed class DataType
{
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? SecondName { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public SyntaxToken? Precision { get; init; }
    public SyntaxToken? Scale { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? TimeKeyword { get; init; }
    public SyntaxToken? Zone { get; init; }
}

public sealed class CastExpression : Expression
{
    public required SyntaxToken CastKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required DataType Type { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class ColonCastExpression : Expression
{
    public required Expression Expression { get; init; }
    public required SyntaxToken DoubleColon { get; init; }
    public required DataType Type { get; init; }
}

public sealed class RowConstructorExpression : Expression
{
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Elements { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class MatchExpression : Expression
{
    public required Expression Left { get; init; }
    public required SyntaxToken MatchKeyword { get; init; }
    public SyntaxToken? UniqueKeyword { get; init; }
    public SyntaxToken? MatchType { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class OverlapsExpression : Expression
{
    public required Expression Left { get; init; }
    public required SyntaxToken OverlapsKeyword { get; init; }
    public required Expression Right { get; init; }
}

public sealed class CollateExpression : Expression
{
    public required Expression Expression { get; init; }
    public required SyntaxToken CollateKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}

public sealed class ArrayExpression : Expression
{
    public required SyntaxToken ArrayKeyword { get; init; }
    public required SyntaxToken OpenBracket { get; init; }
    public required IReadOnlyList<Expression> Elements { get; init; }
    public required SyntaxToken CloseBracket { get; init; }
}

public sealed class IdentifierExpression : Expression
{
    public required SyntaxToken Identifier { get; init; }
}

public sealed class LiteralExpression : Expression
{
    public required SyntaxToken Literal { get; init; }
}

public sealed class NiladicFunctionExpression : Expression
{
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public SyntaxToken? Precision { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}

public sealed class DatetimeLiteralExpression : Expression
{
    public required SyntaxToken KindKeyword { get; init; }
    public required SyntaxToken Literal { get; init; }
}

public sealed class IntervalField
{
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public SyntaxToken? Precision { get; init; }
    public SyntaxToken? Scale { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}

public sealed class IntervalQualifier
{
    public required IntervalField Start { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public IntervalField? End { get; init; }
}

public sealed class IntervalLiteralExpression : Expression
{
    public required SyntaxToken IntervalKeyword { get; init; }
    public SyntaxToken? Sign { get; init; }
    public required SyntaxToken Literal { get; init; }
    public required IntervalQualifier Qualifier { get; init; }
}

public sealed class TrimExpression : Expression
{
    public required SyntaxToken TrimKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public SyntaxToken? Specification { get; init; }
    public Expression? Characters { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public required Expression Source { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class ExtractExpression : Expression
{
    public required SyntaxToken ExtractKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken Field { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required Expression Source { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class SubstringExpression : Expression
{
    public required SyntaxToken SubstringKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Source { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required Expression Start { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public Expression? Length { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class UsingTransformExpression : Expression
{
    public required SyntaxToken FunctionKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken UsingKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class PositionExpression : Expression
{
    public required SyntaxToken PositionKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Needle { get; init; }
    public required SyntaxToken InKeyword { get; init; }
    public required Expression Haystack { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class BinaryExpression : Expression
{
    public required Expression Left { get; init; }
    public required SyntaxToken OperatorToken { get; init; }
    public required Expression Right { get; init; }
}

public sealed class MemberAccessExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken Dot { get; init; }
    public required SyntaxToken Member { get; init; }
}

public sealed class BetweenExpression : Expression
{
    public required Expression Target { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken BetweenKeyword { get; init; }
    public required Expression Lower { get; init; }
    public required SyntaxToken AndKeyword { get; init; }
    public required Expression Upper { get; init; }
}

public sealed class InExpression : Expression
{
    public required Expression Target { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken InKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Values { get; init; }
    public Query? Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class LikeExpression : Expression
{
    public required Expression Target { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken LikeKeyword { get; init; }
    public required Expression Pattern { get; init; }
    public SyntaxToken? EscapeKeyword { get; init; }
    public Expression? Escape { get; init; }
}

public sealed class IsExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken IsKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
}

public sealed class UnaryExpression : Expression
{
    public required SyntaxToken OperatorToken { get; init; }
    public required Expression Expression { get; init; }
}

public sealed class NotExpression : Expression
{
    public required SyntaxToken NotKeyword { get; init; }
    public required Expression Expression { get; init; }
}

public sealed class FilterClause
{
    public required SyntaxToken FilterKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken WhereKeyword { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class FunctionCallExpression : Expression
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Arguments { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public FilterClause? Filter { get; init; }
}

public sealed class HavingClause
{
    public required SyntaxToken HavingKeyword { get; init; }
    public required Expression Expression { get; init; }
}

public sealed class OffsetClause
{
    public required SyntaxToken OffsetKeyword { get; init; }
    public required Expression Count { get; init; }
}

public sealed class ParenExpression : Expression
{
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Inner { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class ScalarSubqueryExpression : Expression
{
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class ExistsExpression : Expression
{
    public required SyntaxToken ExistsKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class UniqueExpression : Expression
{
    public required SyntaxToken UniqueKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class QuantifiedSubqueryExpression : Expression
{
    public required Expression Left { get; init; }
    public required SyntaxToken OperatorToken { get; init; }
    public required SyntaxToken Quantifier { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class WhenClause
{
    public required SyntaxToken WhenKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required SyntaxToken ThenKeyword { get; init; }
    public required Expression Result { get; init; }
}

public sealed class CaseExpression : Expression
{
    public required SyntaxToken CaseKeyword { get; init; }
    public Expression? Operand { get; init; }
    public required IReadOnlyList<WhenClause> Arms { get; init; }
    public SyntaxToken? ElseKeyword { get; init; }
    public Expression? ElseResult { get; init; }
    public required SyntaxToken EndKeyword { get; init; }
}

public sealed class StarExpression : Expression
{
    public required SyntaxToken Star { get; init; }
}

public sealed class QualifiedStarExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken Dot { get; init; }
    public required SyntaxToken Star { get; init; }
}

public sealed class SelectItem
{
    public required Expression Expression { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}

public abstract class TableSource;

public sealed class TableReference : TableSource
{
    public required IReadOnlyList<SyntaxToken> NameParts { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
}

public sealed class DerivedTable : TableSource
{
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public required SyntaxToken Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
}

public sealed class JoinedTable : TableSource
{
    public required SyntaxToken OpenParen { get; init; }
    public required TableSource Table { get; init; }
    public required IReadOnlyList<JoinClause> Joins { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class GroupByClause
{
    public required SyntaxToken GroupKeyword { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required IReadOnlyList<Expression> Keys { get; init; }
}

public sealed class OrderByItem
{
    public required Expression Expression { get; init; }
    public SyntaxToken? Direction { get; init; }
}

public sealed class OrderByClause
{
    public required SyntaxToken OrderKeyword { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required IReadOnlyList<OrderByItem> Items { get; init; }
}

public sealed class LimitClause
{
    public required SyntaxToken LimitKeyword { get; init; }
    public required Expression Count { get; init; }
}

public abstract class JoinConstraint;

public sealed class OnConstraint : JoinConstraint
{
    public required SyntaxToken OnKeyword { get; init; }
    public required Expression Condition { get; init; }
}

public sealed class UsingConstraint : JoinConstraint
{
    public required SyntaxToken UsingKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> Columns { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class JoinClause
{
    public SyntaxToken? NaturalKeyword { get; init; }
    public SyntaxToken? JoinType { get; init; }
    public SyntaxToken? OuterKeyword { get; init; }
    public required SyntaxToken JoinKeyword { get; init; }
    public required TableSource Table { get; init; }
    public JoinConstraint? Constraint { get; init; }
}

public sealed class WhereClause
{
    public required SyntaxToken WhereKeyword { get; init; }
    public required Expression Expression { get; init; }
}

public sealed class CommaFrom
{
    public required SyntaxToken Comma { get; init; }
    public required TableSource Table { get; init; }
    public required IReadOnlyList<JoinClause> Joins { get; init; }
}

public sealed class LockClause
{
    public required SyntaxToken ForKeyword { get; init; }
    public SyntaxToken? ReadKeyword { get; init; }
    public SyntaxToken? OnlyKeyword { get; init; }
    public SyntaxToken? UpdateKeyword { get; init; }
    public SyntaxToken? OfKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
}

public sealed class SelectStatement : Query
{
    public required SyntaxToken SelectKeyword { get; init; }
    public SyntaxToken? DistinctKeyword { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public required IReadOnlyList<SelectItem> SelectList { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public TableSource? From { get; init; }
    public required IReadOnlyList<JoinClause> Joins { get; init; }
    public IReadOnlyList<CommaFrom> ExtraFrom { get; init; } = [];
    public WhereClause? Where { get; init; }
    public GroupByClause? GroupBy { get; init; }
    public HavingClause? Having { get; init; }
    public OrderByClause? OrderBy { get; init; }
    public LimitClause? Limit { get; init; }
    public OffsetClause? Offset { get; init; }
    public LockClause? Lock { get; init; }
}
