namespace QlParse;

public abstract class Query
{
    public required SourceSpan Span { get; init; }
}

public sealed class DirectSqlScript : Query
{
    public required IReadOnlyList<Query> Statements { get; init; }
    public required IReadOnlyList<SyntaxToken> Semicolons { get; init; }
}

public sealed class ModuleDefinition : Query
{
    public required SyntaxToken ModuleKeyword { get; init; }
    public SyntaxToken? Name { get; init; }
    public SyntaxToken? NamesKeyword { get; init; }
    public SyntaxToken? AreKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? CharacterSet { get; init; }
    public required SyntaxToken LanguageKeyword { get; init; }
    public required SyntaxToken Language { get; init; }
    public SyntaxToken? SchemaKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? SchemaName { get; init; }
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Authorization { get; init; }
    public SyntaxToken? PathKeyword { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>> Path { get; init; } = [];
    public IReadOnlyList<Query> Contents { get; init; } = [];
}

public sealed class ModuleProcedure : Query
{
    public required SyntaxToken ProcedureKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<RoutineParameter> Parameters { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? Semicolon { get; init; }
    public required Query Statement { get; init; }
    public SyntaxToken? StatementSemicolon { get; init; }
}

public sealed class EmbeddedSqlStatement : Query
{
    public required SyntaxToken ExecKeyword { get; init; }
    public required SyntaxToken SqlKeyword { get; init; }
    public Query? Statement { get; init; }
    public SyntaxToken? EndKeyword { get; init; }
    public SyntaxToken? Minus { get; init; }
    public SyntaxToken? EndExec { get; init; }
    public SyntaxToken? Semicolon { get; init; }
}

public sealed class DeclareSectionStatement : Query
{
    public required SyntaxToken BeginOrEnd { get; init; }
    public required SyntaxToken DeclareKeyword { get; init; }
    public required SyntaxToken SectionKeyword { get; init; }
}

public sealed class WheneverStatement : Query
{
    public required SyntaxToken WheneverKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken Condition { get; init; }
    public required SyntaxToken Action { get; init; }
    public SyntaxToken? GoKeyword { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public SyntaxToken? Target { get; init; }
}

public sealed class ValuesRow
{
    public required SourceSpan Span { get; init; }
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
    public SyntaxToken? RecursionLimit { get; init; }
    public required IReadOnlyList<CommonTableExpression> Ctes { get; init; }
    public required Query Query { get; init; }
}

public sealed class SearchClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken SearchKeyword { get; init; }
    public required SyntaxToken OrderKeyword { get; init; }
    public required SyntaxToken FirstKeyword { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Columns { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken SequenceColumn { get; init; }
}

public sealed class CycleClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken CycleKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Columns { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken MarkColumn { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required Expression MarkValue { get; init; }
    public required SyntaxToken DefaultKeyword { get; init; }
    public required Expression DefaultValue { get; init; }
    public required SyntaxToken UsingKeyword { get; init; }
    public required SyntaxToken PathColumn { get; init; }
}

public sealed class CommonTableExpression
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required SyntaxToken OpenQuery { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseQuery { get; init; }
    public SearchClause? Search { get; init; }
    public CycleClause? Cycle { get; init; }
}

public abstract class Expression
{
    public required SourceSpan Span { get; init; }
}

public sealed class MdarrayDimension
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Lower { get; init; }
    public SyntaxToken? Colon { get; init; }
    public required SyntaxToken Upper { get; init; }
}

public sealed class CollectionSuffix
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Keyword { get; init; }
    public SyntaxToken? OpenBracket { get; init; }
    public SyntaxToken? Cardinality { get; init; }
    public SyntaxToken? CloseBracket { get; init; }
    public IReadOnlyList<MdarrayDimension>? Dimensions { get; init; }
}

public sealed class FieldDefinition
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required DataType Type { get; init; }
}

public sealed class DataType
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public IReadOnlyList<SyntaxToken> NameTail { get; init; } = [];
    public SyntaxToken? OpenParen { get; init; }
    public SyntaxToken? Precision { get; init; }
    public SyntaxToken? Scale { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? TimeKeyword { get; init; }
    public SyntaxToken? Zone { get; init; }
    public IReadOnlyList<CollectionSuffix> Collections { get; init; } = [];
    public IReadOnlyList<FieldDefinition>? Fields { get; init; }
    public DataType? ReferencedType { get; init; }
    public SyntaxToken? ScopeKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? ScopeName { get; init; }
    public SyntaxToken? Modifier { get; init; }
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

public sealed class TreatExpression : Expression
{
    public required SyntaxToken TreatKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required DataType Type { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class DerefExpression : Expression
{
    public required SyntaxToken DerefKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class RefValueExpression : Expression
{
    public required SyntaxToken RefKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class DereferenceExpression : Expression
{
    public required Expression Reference { get; init; }
    public required SyntaxToken Arrow { get; init; }
    public required SyntaxToken Attribute { get; init; }
}

public sealed class SpecifictypeExpression : Expression
{
    public required SyntaxToken SpecifictypeKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class MethodInvocationExpression : Expression
{
    public SyntaxToken? OpenParen { get; init; }
    public required Expression Target { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public DataType? Type { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken Dot { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken ArgumentsOpen { get; init; }
    public required IReadOnlyList<Expression> Arguments { get; init; }
    public required SyntaxToken ArgumentsClose { get; init; }
}

public sealed class StaticMethodInvocationExpression : Expression
{
    public required Expression Type { get; init; }
    public required SyntaxToken DoubleColon { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Arguments { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class NewSpecificationExpression : Expression
{
    public required SyntaxToken NewKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TypeName { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Arguments { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class NullIfExpression : Expression
{
    public required SyntaxToken NullIfKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression First { get; init; }
    public required Expression Second { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class CoalesceExpression : Expression
{
    public required SyntaxToken CoalesceKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Arguments { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class NextValueExpression : Expression
{
    public required SyntaxToken NextKeyword { get; init; }
    public required SyntaxToken ValueKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> NameParts { get; init; }
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

public sealed class PeriodExpression : Expression
{
    public required SyntaxToken PeriodKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Start { get; init; }
    public required SyntaxToken Comma { get; init; }
    public required Expression End { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class PeriodPredicateExpression : Expression
{
    public required Expression Left { get; init; }
    public SyntaxToken? ImmediatelyKeyword { get; init; }
    public required SyntaxToken OperatorToken { get; init; }
    public required Expression Right { get; init; }
}

public sealed class SystemTimeClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken SystemTimeKeyword { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? OfKeyword { get; init; }
    public Expression? Point { get; init; }
    public SyntaxToken? BetweenKeyword { get; init; }
    public SyntaxToken? Qualifier { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public Expression? Start { get; init; }
    public SyntaxToken? AndKeyword { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public Expression? End { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
}

public sealed class PortionClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken PortionKeyword { get; init; }
    public required SyntaxToken OfKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required Expression Start { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required Expression End { get; init; }
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

public sealed class ArrayQueryExpression : Expression
{
    public required SyntaxToken ArrayKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class MultisetExpression : Expression
{
    public required SyntaxToken MultisetKeyword { get; init; }
    public required SyntaxToken OpenBracket { get; init; }
    public required IReadOnlyList<Expression> Elements { get; init; }
    public required SyntaxToken CloseBracket { get; init; }
}

public sealed class MultisetQueryExpression : Expression
{
    public required SyntaxToken Keyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class MultisetOperationExpression : Expression
{
    public required Expression Left { get; init; }
    public required SyntaxToken MultisetKeyword { get; init; }
    public required SyntaxToken Operator { get; init; }
    public SyntaxToken? Quantifier { get; init; }
    public required Expression Right { get; init; }
}

public sealed class MultisetSetExpression : Expression
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class AbsentOnNullExpression : Expression
{
    public required SyntaxToken AbsentKeyword { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required SyntaxToken NullKeyword { get; init; }
}

public sealed class IdentifierExpression : Expression
{
    public required SyntaxToken Identifier { get; init; }
}

public sealed class HostParameterExpression : Expression
{
    public required SyntaxToken QuestionMark { get; init; }
}

public sealed class EmbeddedHostExpression : Expression
{
    public required SyntaxToken Name { get; init; }
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
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public SyntaxToken? Precision { get; init; }
    public SyntaxToken? Scale { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}

public sealed class IntervalQualifier
{
    public required SourceSpan Span { get; init; }
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

public sealed class SpecialFormExpression : Expression
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Arguments { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public SyntaxToken? UsingName { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class OverlayExpression : Expression
{
    public required SyntaxToken OverlayKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Source { get; init; }
    public required SyntaxToken PlacingKeyword { get; init; }
    public required Expression Replacement { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required Expression Start { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public Expression? Length { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class BinaryExpression : Expression
{
    public required Expression Left { get; init; }
    public required SyntaxToken OperatorToken { get; init; }
    public required Expression Right { get; init; }
}

public sealed class JsonAccessorExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken OpenBracket { get; init; }
    public required Expression Index { get; init; }
    public required SyntaxToken CloseBracket { get; init; }
}

public sealed class MarkupCallExpression : Expression
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public IReadOnlyList<SyntaxToken> Clauses { get; init; } = [];
    public DataType? Returning { get; init; }
    public Query? Query { get; init; }
    public OrderByClause? OrderBy { get; init; }
    public IReadOnlyList<MarkupColumn> Columns { get; init; } = [];
    public FilterClause? Filter { get; init; }
    public WindowSpecification? Over { get; init; }
}

public sealed class MarkupColumn
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public DataType? Type { get; init; }
    public Expression? Path { get; init; }
    public Expression? Default { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? OrdinalityKeyword { get; init; }
    public IReadOnlyList<MarkupColumn> Nested { get; init; } = [];
}

public sealed class MarkupTable : TableSource
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public IReadOnlyList<SyntaxToken> Clauses { get; init; } = [];
    public IReadOnlyList<MarkupColumn> Columns { get; init; } = [];
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}

public abstract class RowPattern
{
    public required SourceSpan Span { get; init; }
}

public sealed class PatternPrimary : RowPattern
{
    public required SyntaxToken Token { get; init; }
}

public sealed class PatternConcatenation : RowPattern
{
    public required IReadOnlyList<RowPattern> Factors { get; init; }
}

public sealed class PatternAlternation : RowPattern
{
    public required IReadOnlyList<RowPattern> Terms { get; init; }
}

public sealed class PatternQuantified : RowPattern
{
    public required RowPattern Primary { get; init; }
    public required SyntaxToken Quantifier { get; init; }
    public SyntaxToken? Low { get; init; }
    public SyntaxToken? Comma { get; init; }
    public SyntaxToken? High { get; init; }
    public SyntaxToken? CloseBrace { get; init; }
    public SyntaxToken? Reluctant { get; init; }
}

public sealed class PatternGroup : RowPattern
{
    public required SyntaxToken OpenParen { get; init; }
    public required RowPattern Pattern { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class PatternExclusion : RowPattern
{
    public required SyntaxToken OpenBrace { get; init; }
    public required SyntaxToken Minus { get; init; }
    public required RowPattern Pattern { get; init; }
    public required SyntaxToken CloseMinus { get; init; }
    public required SyntaxToken CloseBrace { get; init; }
}

public sealed class PatternPermute : RowPattern
{
    public required SyntaxToken PermuteKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<RowPattern> Patterns { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class PatternMeasure
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Semantics { get; init; }
    public required Expression Expression { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}

public sealed class PatternDefinition
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required Expression Condition { get; init; }
}

public sealed class PatternSubset
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken EqualsToken { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> Variables { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class MatchRecognizeTable : TableSource
{
    public required TableSource Input { get; init; }
    public required SyntaxToken MatchRecognizeKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? PartitionKeyword { get; init; }
    public SyntaxToken? PartitionBy { get; init; }
    public IReadOnlyList<Expression> PartitionByList { get; init; } = [];
    public OrderByClause? OrderBy { get; init; }
    public SyntaxToken? MeasuresKeyword { get; init; }
    public IReadOnlyList<PatternMeasure> Measures { get; init; } = [];
    public SyntaxToken? RowsKind { get; init; }
    public SyntaxToken? RowOrRows { get; init; }
    public SyntaxToken? PerKeyword { get; init; }
    public SyntaxToken? MatchKeyword { get; init; }
    public SyntaxToken? EmptyHandling { get; init; }
    public SyntaxToken? AfterKeyword { get; init; }
    public SyntaxToken? SkipKeyword { get; init; }
    public SyntaxToken? SkipTo { get; init; }
    public SyntaxToken? SkipPosition { get; init; }
    public SyntaxToken? SkipTarget { get; init; }
    public required SyntaxToken PatternKeyword { get; init; }
    public required SyntaxToken PatternOpen { get; init; }
    public required RowPattern Pattern { get; init; }
    public required SyntaxToken PatternClose { get; init; }
    public SyntaxToken? SubsetKeyword { get; init; }
    public IReadOnlyList<PatternSubset> Subsets { get; init; } = [];
    public required SyntaxToken DefineKeyword { get; init; }
    public required IReadOnlyList<PatternDefinition> Definitions { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
}

public sealed class PtfArgument
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? KindKeyword { get; init; }
    public SyntaxToken? SemanticsKeyword { get; init; }
    public Query? Query { get; init; }
    public IReadOnlyList<Expression> Values { get; init; } = [];
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? PartitionKeyword { get; init; }
    public SyntaxToken? PartitionBy { get; init; }
    public IReadOnlyList<Expression> PartitionByList { get; init; } = [];
    public OrderByClause? OrderBy { get; init; }
    public SyntaxToken? PruneKeyword { get; init; }
    public Expression? Prune { get; init; }
    public SyntaxToken? CopartitionKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Names { get; init; } = [];
    public Expression? Scalar { get; init; }
}

public sealed class PtfTable : TableSource
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public IReadOnlyList<PtfArgument> Arguments { get; init; } = [];
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}

public sealed class MdarrayConstructorExpression : Expression
{
    public required SyntaxToken MdarrayKeyword { get; init; }
    public SyntaxToken? OpenBracket { get; init; }
    public IReadOnlyList<MdarrayDimension> Dimensions { get; init; } = [];
    public SyntaxToken? CloseBracket { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public IReadOnlyList<Expression> Elements { get; init; } = [];
    public Query? Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class MdarrayAxis
{
    public required SourceSpan Span { get; init; }
    public Expression? Lower { get; init; }
    public SyntaxToken? Colon { get; init; }
    public Expression? Upper { get; init; }
    public SyntaxToken? Star { get; init; }
}

public sealed class MdarraySliceExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken OpenBracket { get; init; }
    public required IReadOnlyList<MdarrayAxis> Axes { get; init; }
    public required SyntaxToken CloseBracket { get; init; }
}

public sealed class MdarrayAggregateExpression : Expression
{
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Argument { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class GraphProperty
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken Colon { get; init; }
    public required Expression Value { get; init; }
}

public sealed class GraphElement
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Variable { get; init; }
    public IReadOnlyList<SyntaxToken> Labels { get; init; } = [];
    public IReadOnlyList<GraphProperty> Properties { get; init; } = [];
    public Expression? Where { get; init; }
    public bool Edge { get; init; }
    public SyntaxToken? Quantifier { get; init; }
    public IReadOnlyList<GraphElement> Group { get; init; } = [];
}

public sealed class GraphTable : TableSource
{
    public required SyntaxToken GraphTableKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> GraphName { get; init; }
    public required SyntaxToken MatchKeyword { get; init; }
    public required IReadOnlyList<GraphElement> Pattern { get; init; }
    public SyntaxToken? WhereKeyword { get; init; }
    public Expression? Where { get; init; }
    public SyntaxToken? ColumnsKeyword { get; init; }
    public IReadOnlyList<Expression> Columns { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}

public sealed class GraphTableElement
{
    public required SourceSpan Span { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? KeyKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> KeyColumns { get; init; } = [];
    public SyntaxToken? SourceKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> SourceColumns { get; init; } = [];
    public IReadOnlyList<SyntaxToken> SourceReference { get; init; } = [];
    public SyntaxToken? DestinationKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> DestinationColumns { get; init; } = [];
    public IReadOnlyList<SyntaxToken> DestinationReference { get; init; } = [];
    public IReadOnlyList<SyntaxToken> Labels { get; init; } = [];
    public IReadOnlyList<SyntaxToken> Properties { get; init; } = [];
}

public sealed class CreatePropertyGraphStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken PropertyKeyword { get; init; }
    public required SyntaxToken GraphKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken VertexKeyword { get; init; }
    public required IReadOnlyList<GraphTableElement> Vertices { get; init; }
    public SyntaxToken? EdgeKeyword { get; init; }
    public IReadOnlyList<GraphTableElement> Edges { get; init; } = [];
}

public sealed class DropPropertyGraphStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken PropertyKeyword { get; init; }
    public required SyntaxToken GraphKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? Behavior { get; init; }
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

public sealed class SimilarExpression : Expression
{
    public required Expression Target { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken SimilarKeyword { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required Expression Pattern { get; init; }
    public SyntaxToken? EscapeKeyword { get; init; }
    public Expression? Escape { get; init; }
}

public sealed class DistinctFromExpression : Expression
{
    public required Expression Left { get; init; }
    public required SyntaxToken IsKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken DistinctKeyword { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required Expression Right { get; init; }
}

public sealed class NormalizedPredicateExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken IsKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? Form { get; init; }
    public required SyntaxToken NormalizedKeyword { get; init; }
}

public sealed class IsExpression : Expression
{
    public required Expression Target { get; init; }
    public required SyntaxToken IsKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
    public SyntaxToken? KindKeyword { get; init; }
    public SyntaxToken? UniqueWith { get; init; }
    public SyntaxToken? UniqueKeyword { get; init; }
    public SyntaxToken? KeysKeyword { get; init; }
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
    public required SourceSpan Span { get; init; }
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
    public SyntaxToken? FromKeyword { get; init; }
    public SyntaxToken? FromPosition { get; init; }
    public SyntaxToken? NullTreatment { get; init; }
    public SyntaxToken? NullsKeyword { get; init; }
    public FilterClause? Filter { get; init; }
    public WindowSpecification? Over { get; init; }
}

public sealed class WindowFrameBound
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? UnboundedKeyword { get; init; }
    public SyntaxToken? CurrentKeyword { get; init; }
    public Expression? Offset { get; init; }
    public required SyntaxToken Endpoint { get; init; }
}

public sealed class WindowFrame
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Units { get; init; }
    public SyntaxToken? BetweenKeyword { get; init; }
    public required WindowFrameBound Start { get; init; }
    public SyntaxToken? AndKeyword { get; init; }
    public WindowFrameBound? End { get; init; }
    public SyntaxToken? ExcludeKeyword { get; init; }
    public SyntaxToken? Exclusion { get; init; }
    public SyntaxToken? ExclusionTail { get; init; }
}

public sealed class WindowSpecification
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? OverKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public SyntaxToken? Name { get; init; }
    public SyntaxToken? PartitionKeyword { get; init; }
    public SyntaxToken? PartitionByKeyword { get; init; }
    public IReadOnlyList<Expression> PartitionBy { get; init; } = [];
    public OrderByClause? OrderBy { get; init; }
    public WindowFrame? Frame { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}

public sealed class WindowDefinition
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required WindowSpecification Specification { get; init; }
}

public sealed class WindowClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WindowKeyword { get; init; }
    public required IReadOnlyList<WindowDefinition> Windows { get; init; }
}

public sealed class HavingClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken HavingKeyword { get; init; }
    public required Expression Expression { get; init; }
}

public sealed class OffsetClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OffsetKeyword { get; init; }
    public required Expression Count { get; init; }
    public SyntaxToken? RowKeyword { get; init; }
}

public sealed class FetchClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken FetchKeyword { get; init; }
    public required SyntaxToken PositionKeyword { get; init; }
    public Expression? Count { get; init; }
    public SyntaxToken? PercentKeyword { get; init; }
    public required SyntaxToken RowKeyword { get; init; }
    public required SyntaxToken OnlyOrWith { get; init; }
    public SyntaxToken? TiesKeyword { get; init; }
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
    public required SourceSpan Span { get; init; }
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
    public required SourceSpan Span { get; init; }
    public required Expression Expression { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}

public abstract class TableSource
{
    public required SourceSpan Span { get; init; }
}

public sealed class TableReference : TableSource
{
    public required IReadOnlyList<SyntaxToken> NameParts { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
    public SystemTimeClause? SystemTime { get; init; }
}

public sealed class DerivedTable : TableSource
{
    public SyntaxToken? LateralKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Query Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public required SyntaxToken Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
}

public sealed class OnlyTable : TableSource
{
    public required SyntaxToken OnlyKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> NameParts { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
    public SystemTimeClause? SystemTime { get; init; }
}

public sealed class TableFunction : TableSource
{
    public SyntaxToken? TableKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> NameParts { get; init; } = [];
    public required SyntaxToken OpenParen { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public Query? Query { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
}

public sealed class SampledTable : TableSource
{
    public required TableSource Table { get; init; }
    public required SyntaxToken TablesampleKeyword { get; init; }
    public required SyntaxToken Method { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Percentage { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? RepeatableKeyword { get; init; }
    public SyntaxToken? RepeatOpenParen { get; init; }
    public Expression? RepeatArgument { get; init; }
    public SyntaxToken? RepeatCloseParen { get; init; }
}

public sealed class JoinedTable : TableSource
{
    public required SyntaxToken OpenParen { get; init; }
    public required TableSource Table { get; init; }
    public required IReadOnlyList<JoinClause> Joins { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class UnnestTable : TableSource
{
    public required SyntaxToken UnnestKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Expressions { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? OrdinalityKeyword { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
}

public sealed class GroupingOperationExpression : Expression
{
    public required SyntaxToken Keyword { get; init; }
    public SyntaxToken? SetsKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Elements { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class EmptyGroupingSetExpression : Expression
{
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class GroupByClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken GroupKeyword { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required IReadOnlyList<Expression> Keys { get; init; }
}

public sealed class OrderByItem
{
    public required SourceSpan Span { get; init; }
    public required Expression Expression { get; init; }
    public SyntaxToken? Direction { get; init; }
    public SyntaxToken? NullsKeyword { get; init; }
    public SyntaxToken? NullOrder { get; init; }
}

public sealed class OrderByClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OrderKeyword { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required IReadOnlyList<OrderByItem> Items { get; init; }
}

public sealed class LimitClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken LimitKeyword { get; init; }
    public required Expression Count { get; init; }
}

public abstract class JoinConstraint
{
    public required SourceSpan Span { get; init; }
}

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
    public required SourceSpan Span { get; init; }
    public SyntaxToken? NaturalKeyword { get; init; }
    public SyntaxToken? JoinType { get; init; }
    public SyntaxToken? OuterKeyword { get; init; }
    public required SyntaxToken JoinKeyword { get; init; }
    public required TableSource Table { get; init; }
    public JoinConstraint? Constraint { get; init; }
}

public sealed class WhereClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WhereKeyword { get; init; }
    public required Expression Expression { get; init; }
}

public sealed class CommaFrom
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Comma { get; init; }
    public required TableSource Table { get; init; }
    public required IReadOnlyList<JoinClause> Joins { get; init; }
}

public sealed class LockClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public SyntaxToken? ReadKeyword { get; init; }
    public SyntaxToken? OnlyKeyword { get; init; }
    public SyntaxToken? UpdateKeyword { get; init; }
    public SyntaxToken? ShareKeyword { get; init; }
    public SyntaxToken? OfKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
}

public sealed class InsertStatement : Query
{
    public required SyntaxToken InsertKeyword { get; init; }
    public required SyntaxToken IntoKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
    public OverrideClause? Override { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public SyntaxToken? ValuesKeyword { get; init; }
    public Query? Query { get; init; }
}

public sealed class OverrideClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OverridingKeyword { get; init; }
    public required SyntaxToken Kind { get; init; }
    public required SyntaxToken ValueKeyword { get; init; }
}

public sealed class TargetTable
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? OnlyKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> NameParts { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}

public sealed class SetTarget
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OpenBracket { get; init; }
    public Expression? Index { get; init; }
    public SyntaxToken? CloseBracket { get; init; }
}

public sealed class SetClause
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public required IReadOnlyList<SetTarget> Targets { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken EqualsToken { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public Expression? Value { get; init; }
}

public sealed class PositionedClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WhereKeyword { get; init; }
    public required SyntaxToken CurrentKeyword { get; init; }
    public required SyntaxToken OfKeyword { get; init; }
    public required SyntaxToken Cursor { get; init; }
}

public sealed class UpdateStatement : Query
{
    public required SyntaxToken UpdateKeyword { get; init; }
    public required TargetTable Target { get; init; }
    public PortionClause? Portion { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required IReadOnlyList<SetClause> Assignments { get; init; }
    public WhereClause? Where { get; init; }
    public PositionedClause? Positioned { get; init; }
}

public sealed class DeleteStatement : Query
{
    public required SyntaxToken DeleteKeyword { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required TargetTable Target { get; init; }
    public PortionClause? Portion { get; init; }
    public WhereClause? Where { get; init; }
    public PositionedClause? Positioned { get; init; }
}

public abstract class MergeAction
{
    public required SourceSpan Span { get; init; }
}

public sealed class MergeUpdateAction : MergeAction
{
    public required SyntaxToken UpdateKeyword { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required IReadOnlyList<SetClause> Assignments { get; init; }
}

public sealed class MergeDeleteAction : MergeAction
{
    public required SyntaxToken DeleteKeyword { get; init; }
}

public sealed class MergeInsertAction : MergeAction
{
    public required SyntaxToken InsertKeyword { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
    public OverrideClause? Override { get; init; }
    public required SyntaxToken ValuesKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<Expression> Values { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class MergeWhenClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WhenKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken MatchedKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? ByKind { get; init; }
    public SyntaxToken? AndKeyword { get; init; }
    public Expression? Condition { get; init; }
    public required SyntaxToken ThenKeyword { get; init; }
    public required MergeAction Action { get; init; }
}

public sealed class MergeStatement : Query
{
    public required SyntaxToken MergeKeyword { get; init; }
    public required SyntaxToken IntoKeyword { get; init; }
    public required TargetTable Target { get; init; }
    public required SyntaxToken UsingKeyword { get; init; }
    public required TableSource Source { get; init; }
    public required IReadOnlyList<JoinClause> SourceJoins { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required IReadOnlyList<MergeWhenClause> Whens { get; init; }
}

public sealed class TruncateStatement : Query
{
    public required SyntaxToken TruncateKeyword { get; init; }
    public required SyntaxToken TableKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public SyntaxToken? RestartKeyword { get; init; }
    public SyntaxToken? IdentityKeyword { get; init; }
}

public sealed class SchemaDefinition : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Name { get; init; }
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Authorization { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public SyntaxToken? CharacterKeyword { get; init; }
    public SyntaxToken? SetKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? CharacterSet { get; init; }
    public SyntaxToken? PathKeyword { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>>? Path { get; init; }
    public IReadOnlyList<CreateTableStatement> Elements { get; init; } = [];
}

public sealed class AlterSchemaStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken RenameKeyword { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> NewName { get; init; }
}

public sealed class DropSchemaStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}

public abstract class TableElement
{
    public required SourceSpan Span { get; init; }
}

public sealed class DefaultClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken DefaultKeyword { get; init; }
    public required Expression Value { get; init; }
}

public sealed class SequenceOption
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public Expression? Value { get; init; }
}

public sealed class IdentityColumn
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken GeneratedKeyword { get; init; }
    public SyntaxToken? AlwaysKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required SyntaxToken IdentityKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SequenceOption> Options { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}

public sealed class GeneratedColumn
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken GeneratedKeyword { get; init; }
    public SyntaxToken? AlwaysKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public Expression? Expression { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? RowKeyword { get; init; }
    public SyntaxToken? Bound { get; init; }
}

public sealed class ReferentialAction
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required SyntaxToken Event { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? SetKeyword { get; init; }
    public required SyntaxToken Action { get; init; }
}

public sealed class TableConstraint : TableElement
{
    public SyntaxToken? ConstraintKeyword { get; init; }
    public SyntaxToken? ConstraintName { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? NullKeyword { get; init; }
    public SyntaxToken? UniqueKeyword { get; init; }
    public SyntaxToken? NullsKeyword { get; init; }
    public SyntaxToken? NullsNotKeyword { get; init; }
    public SyntaxToken? DistinctKeyword { get; init; }
    public SyntaxToken? PrimaryKeyword { get; init; }
    public SyntaxToken? KeyKeyword { get; init; }
    public SyntaxToken? ForeignKeyword { get; init; }
    public SyntaxToken? ColumnsOpen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnsClose { get; init; }
    public SyntaxToken? ReferencesKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? ReferenceTable { get; init; }
    public SyntaxToken? ReferenceOpen { get; init; }
    public IReadOnlyList<SyntaxToken>? ReferenceColumns { get; init; }
    public SyntaxToken? ReferenceClose { get; init; }
    public SyntaxToken? MatchKeyword { get; init; }
    public SyntaxToken? MatchType { get; init; }
    public IReadOnlyList<ReferentialAction> Actions { get; init; } = [];
    public SyntaxToken? CheckKeyword { get; init; }
    public SyntaxToken? CheckOpen { get; init; }
    public Expression? Check { get; init; }
    public SyntaxToken? CheckClose { get; init; }
    public SyntaxToken? DeferrableNotKeyword { get; init; }
    public SyntaxToken? DeferrableKeyword { get; init; }
    public SyntaxToken? InitiallyKeyword { get; init; }
    public SyntaxToken? InitiallyWhen { get; init; }
}

public sealed class LikeOption
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken KindKeyword { get; init; }
    public required SyntaxToken Option { get; init; }
}

public sealed class LikeClause : TableElement
{
    public required SyntaxToken LikeKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public IReadOnlyList<LikeOption> Options { get; init; } = [];
}

public sealed class ColumnDefinition : TableElement
{
    public required SyntaxToken Name { get; init; }
    public DataType? Type { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? OptionsKeyword { get; init; }
    public DefaultClause? Default { get; init; }
    public IdentityColumn? Identity { get; init; }
    public GeneratedColumn? Generated { get; init; }
    public SyntaxToken? CollateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Collation { get; init; }
    public SyntaxToken? ScopeKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? ScopeName { get; init; }
    public IReadOnlyList<TableConstraint> Constraints { get; init; } = [];
}

public sealed class RefIsClause : TableElement
{
    public required SyntaxToken RefKeyword { get; init; }
    public required SyntaxToken IsKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? Generation { get; init; }
    public SyntaxToken? GeneratedKeyword { get; init; }
}

public sealed class CreateTableStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public SyntaxToken? Scope { get; init; }
    public SyntaxToken? TemporaryKeyword { get; init; }
    public required SyntaxToken TableKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? OfKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? TypeName { get; init; }
    public SyntaxToken? UnderKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Supertable { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<TableElement>? Elements { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? AsColumnsOpen { get; init; }
    public IReadOnlyList<SyntaxToken>? AsColumns { get; init; }
    public SyntaxToken? AsColumnsClose { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public Query? Query { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? DataKeyword { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public SyntaxToken? CommitKeyword { get; init; }
    public SyntaxToken? CommitAction { get; init; }
    public SyntaxToken? RowsKeyword { get; init; }
    public SyntaxToken? SystemKeyword { get; init; }
    public SyntaxToken? VersioningKeyword { get; init; }
}

public sealed class PeriodDefinition : TableElement
{
    public required SyntaxToken PeriodKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken StartColumn { get; init; }
    public required SyntaxToken Comma { get; init; }
    public required SyntaxToken EndColumn { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class AddPeriodAction : AlterTableAction
{
    public required SyntaxToken AddKeyword { get; init; }
    public required PeriodDefinition Period { get; init; }
}

public sealed class DropPeriodAction : AlterTableAction
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken PeriodKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}

public sealed class SystemVersioningAction : AlterTableAction
{
    public required SyntaxToken VerbKeyword { get; init; }
    public required SyntaxToken SystemKeyword { get; init; }
    public required SyntaxToken VersioningKeyword { get; init; }
}

public abstract class AlterTableAction
{
    public required SourceSpan Span { get; init; }
}

public sealed class AddColumnAction : AlterTableAction
{
    public required SyntaxToken AddKeyword { get; init; }
    public SyntaxToken? ColumnKeyword { get; init; }
    public required ColumnDefinition Column { get; init; }
}

public sealed class DropColumnAction : AlterTableAction
{
    public required SyntaxToken DropKeyword { get; init; }
    public SyntaxToken? ColumnKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}

public sealed class AddConstraintAction : AlterTableAction
{
    public required SyntaxToken AddKeyword { get; init; }
    public required TableConstraint Constraint { get; init; }
}

public sealed class DropConstraintAction : AlterTableAction
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken ConstraintKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}

public sealed class AlterColumnAction : AlterTableAction
{
    public required SyntaxToken AlterKeyword { get; init; }
    public SyntaxToken? ColumnKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? SetKeyword { get; init; }
    public SyntaxToken? DropKeyword { get; init; }
    public SyntaxToken? AddKeyword { get; init; }
    public SyntaxToken? DataKeyword { get; init; }
    public SyntaxToken? TypeKeyword { get; init; }
    public DataType? DataType { get; init; }
    public DefaultClause? Default { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? NullKeyword { get; init; }
    public SyntaxToken? ScopeKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? ScopeName { get; init; }
    public SyntaxToken? Behavior { get; init; }
    public SyntaxToken? RestartKeyword { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public Expression? RestartValue { get; init; }
    public SyntaxToken? GeneratedKeyword { get; init; }
    public SyntaxToken? AlwaysKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public SyntaxToken? IdentityKeyword { get; init; }
    public SequenceOption? IdentityOption { get; init; }
}

public sealed class AlterTableStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken TableKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required AlterTableAction Action { get; init; }
}

public sealed class DropTableStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken TableKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}

public sealed class SelectStatement : Query
{
    public required SyntaxToken SelectKeyword { get; init; }
    public SyntaxToken? DistinctKeyword { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public required IReadOnlyList<SelectItem> SelectList { get; init; }
    public SyntaxToken? IntoKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? IntoTargets { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public TableSource? From { get; init; }
    public required IReadOnlyList<JoinClause> Joins { get; init; }
    public IReadOnlyList<CommaFrom> ExtraFrom { get; init; } = [];
    public WhereClause? Where { get; init; }
    public GroupByClause? GroupBy { get; init; }
    public HavingClause? Having { get; init; }
    public WindowClause? Window { get; init; }
    public OrderByClause? OrderBy { get; init; }
    public LimitClause? Limit { get; init; }
    public OffsetClause? Offset { get; init; }
    public FetchClause? Fetch { get; init; }
    public LockClause? Lock { get; init; }
}

public sealed class CreateViewStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public SyntaxToken? RecursiveKeyword { get; init; }
    public required SyntaxToken ViewKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? OfKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? TypeName { get; init; }
    public SyntaxToken? UnderKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Superview { get; init; }
    public SyntaxToken? ColumnsOpen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnsClose { get; init; }
    public IReadOnlyList<TableElement>? Elements { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required Query Query { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? Levels { get; init; }
    public SyntaxToken? CheckKeyword { get; init; }
    public SyntaxToken? OptionKeyword { get; init; }
}

public abstract class AlterViewAction
{
    public required SourceSpan Span { get; init; }
}

public sealed class ReplaceViewAction : AlterViewAction
{
    public required SyntaxToken AsKeyword { get; init; }
    public required Query Query { get; init; }
}

public sealed class AlterViewColumnAction : AlterViewAction
{
    public required AlterColumnAction Column { get; init; }
}

public sealed class AlterViewStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken ViewKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required AlterViewAction Action { get; init; }
}

public sealed class DropViewStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken ViewKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}

public sealed class DomainConstraint
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? ConstraintKeyword { get; init; }
    public SyntaxToken? ConstraintName { get; init; }
    public required SyntaxToken CheckKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Check { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? DeferrableNotKeyword { get; init; }
    public SyntaxToken? DeferrableKeyword { get; init; }
    public SyntaxToken? InitiallyKeyword { get; init; }
    public SyntaxToken? InitiallyWhen { get; init; }
}

public sealed class CreateDomainStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken DomainKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public required DataType Type { get; init; }
    public DefaultClause? Default { get; init; }
    public IReadOnlyList<DomainConstraint> Constraints { get; init; } = [];
    public SyntaxToken? CollateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Collation { get; init; }
}

public abstract class AlterDomainAction
{
    public required SourceSpan Span { get; init; }
}

public sealed class SetDomainDefaultAction : AlterDomainAction
{
    public required SyntaxToken SetKeyword { get; init; }
    public required DefaultClause Default { get; init; }
}

public sealed class DropDomainDefaultAction : AlterDomainAction
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken DefaultKeyword { get; init; }
}

public sealed class AddDomainConstraintAction : AlterDomainAction
{
    public required SyntaxToken AddKeyword { get; init; }
    public required DomainConstraint Constraint { get; init; }
}

public sealed class DropDomainConstraintAction : AlterDomainAction
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken ConstraintKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}

public sealed class AlterDomainStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken DomainKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required AlterDomainAction Action { get; init; }
}

public sealed class DropDomainStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken DomainKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}

public sealed class AttributeDefinition
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required DataType Type { get; init; }
    public SyntaxToken? ReferencesKeyword { get; init; }
    public SyntaxToken? AreKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? CheckedKeyword { get; init; }
    public ReferentialAction? OnDelete { get; init; }
    public DefaultClause? Default { get; init; }
    public SyntaxToken? CollateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Collation { get; init; }
}

public sealed class ReferenceGeneration
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken RefKeyword { get; init; }
    public SyntaxToken? IsKeyword { get; init; }
    public SyntaxToken? SystemKeyword { get; init; }
    public SyntaxToken? GeneratedKeyword { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public DataType? PredefinedType { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Attributes { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}

public sealed class TypeCastOption
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken CastKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken Source { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required SyntaxToken Target { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required SyntaxToken Function { get; init; }
}

public sealed class RoutineParameter
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Mode { get; init; }
    public SyntaxToken? Name { get; init; }
    public required DataType Type { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? LocatorKeyword { get; init; }
    public SyntaxToken? ResultKeyword { get; init; }
}

public sealed class RoutineCharacteristic
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public IReadOnlyList<SyntaxToken> Tail { get; init; } = [];
}

public sealed class MethodSpecification
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? OverridingKeyword { get; init; }
    public SyntaxToken? Kind { get; init; }
    public required SyntaxToken MethodKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public IReadOnlyList<RoutineParameter> Parameters { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
    public required SyntaxToken ReturnsKeyword { get; init; }
    public required DataType ReturnsType { get; init; }
    public SyntaxToken? ReturnsAsKeyword { get; init; }
    public SyntaxToken? ReturnsLocator { get; init; }
    public SyntaxToken? SpecificKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? SpecificName { get; init; }
    public SyntaxToken? SelfResultKeyword { get; init; }
    public SyntaxToken? SelfResultAsKeyword { get; init; }
    public SyntaxToken? ResultKeyword { get; init; }
    public SyntaxToken? SelfLocatorKeyword { get; init; }
    public SyntaxToken? SelfLocatorAsKeyword { get; init; }
    public SyntaxToken? LocatorKeyword { get; init; }
    public IReadOnlyList<RoutineCharacteristic> Characteristics { get; init; } = [];
}

public sealed class CreateTypeStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken TypeKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? UnderKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Supertype { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public DataType? Representation { get; init; }
    public SyntaxToken? MembersOpen { get; init; }
    public IReadOnlyList<AttributeDefinition>? Attributes { get; init; }
    public SyntaxToken? MembersClose { get; init; }
    public SyntaxToken? NotInstantiableKeyword { get; init; }
    public SyntaxToken? InstantiableKeyword { get; init; }
    public SyntaxToken? NotFinalKeyword { get; init; }
    public SyntaxToken? FinalKeyword { get; init; }
    public ReferenceGeneration? Reference { get; init; }
    public IReadOnlyList<TypeCastOption> Casts { get; init; } = [];
    public IReadOnlyList<MethodSpecification> Methods { get; init; } = [];
}

public sealed class DropTypeStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken TypeKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}

public sealed class RoutineDesignator
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? SpecificKeyword { get; init; }
    public SyntaxToken? Kind { get; init; }
    public required SyntaxToken RoutineType { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? ForType { get; init; }
}

public sealed class CreateOrderingStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken OrderingKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? EqualsKeyword { get; init; }
    public SyntaxToken? OrderKeyword { get; init; }
    public required SyntaxToken Form { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required SyntaxToken Category { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public RoutineDesignator? Routine { get; init; }
}

public sealed class CreateCastStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken CastKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required DataType Source { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required DataType Target { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required RoutineDesignator Routine { get; init; }
    public SyntaxToken? AssignmentAsKeyword { get; init; }
    public SyntaxToken? AssignmentKeyword { get; init; }
}

public sealed class TransformElement
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Direction { get; init; }
    public required SyntaxToken SqlKeyword { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required RoutineDesignator Routine { get; init; }
}

public sealed class TransformGroup
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<TransformElement> Elements { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class CreateTransformStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken TransformKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required IReadOnlyList<TransformGroup> Groups { get; init; }
}

public sealed class CreateAssertionStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken AssertionKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken CheckKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Check { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? DeferrableNotKeyword { get; init; }
    public SyntaxToken? DeferrableKeyword { get; init; }
    public SyntaxToken? InitiallyKeyword { get; init; }
    public SyntaxToken? InitiallyWhen { get; init; }
}

public sealed class DropAssertionStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken AssertionKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}

public sealed class CreateCharacterSetStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken CharacterKeyword { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public required SyntaxToken GetKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Source { get; init; }
    public SyntaxToken? CollateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Collation { get; init; }
}

public sealed class DropCharacterSetStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken CharacterKeyword { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
}

public sealed class CreateCollationStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken CollationKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> CharacterSet { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Source { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? PadKeyword { get; init; }
    public SyntaxToken? SpaceKeyword { get; init; }
}

public sealed class DropCollationStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken CollationKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}

public sealed class CreateTranslationStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken TranslationKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> SourceCharacterSet { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TargetCharacterSet { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Existing { get; init; }
    public RoutineDesignator? Routine { get; init; }
}

public sealed class DropTranslationStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken TranslationKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
}

public sealed class CreateSequenceStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken SequenceKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public DataType? DataType { get; init; }
    public IReadOnlyList<SequenceOption> Options { get; init; } = [];
}

public sealed class DropSequenceStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken SequenceKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken Behavior { get; init; }
}

public sealed class IndexColumn
{
    public required SourceSpan Span { get; init; }
    public required Expression Expression { get; init; }
    public SyntaxToken? Direction { get; init; }
    public SyntaxToken? NullsKeyword { get; init; }
    public SyntaxToken? NullsOrder { get; init; }
}

public sealed class CreateIndexStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public SyntaxToken? UniqueKeyword { get; init; }
    public SyntaxToken? ClusteredKeyword { get; init; }
    public required SyntaxToken IndexKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<IndexColumn> Columns { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? WhereKeyword { get; init; }
    public Expression? Where { get; init; }
}

public sealed class AlterIndexStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken IndexKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? RenameKeyword { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? NewName { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? TableName { get; init; }
    public SyntaxToken? Action { get; init; }
}

public sealed class DropIndexStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken IndexKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? TableName { get; init; }
    public SyntaxToken? Behavior { get; init; }
}

public sealed class CommentStatement : Query
{
    public required SyntaxToken CommentKeyword { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required SyntaxToken Kind { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken IsKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
}

public sealed class PrivilegeAction
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? PrivilegesKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}

public sealed class PrivilegeObject
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Kind { get; init; }
    public SyntaxToken? SetKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Name { get; init; }
    public RoutineDesignator? Routine { get; init; }
}

public sealed class GrantOptionClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required SyntaxToken Kind { get; init; }
    public required SyntaxToken OptionKeyword { get; init; }
}

public sealed class RevokeOptionClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Kind { get; init; }
    public required SyntaxToken OptionKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
}

public sealed class GrantPrivilegeStatement : Query
{
    public required SyntaxToken GrantKeyword { get; init; }
    public required IReadOnlyList<PrivilegeAction> Actions { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required PrivilegeObject Object { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Grantees { get; init; }
    public GrantOptionClause? HierarchyOption { get; init; }
    public GrantOptionClause? GrantOption { get; init; }
    public SyntaxToken? GrantedKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? Grantor { get; init; }
}

public sealed class GrantRoleStatement : Query
{
    public required SyntaxToken GrantKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Roles { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Grantees { get; init; }
    public GrantOptionClause? AdminOption { get; init; }
    public SyntaxToken? GrantedKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? Grantor { get; init; }
}

public sealed class RevokePrivilegeStatement : Query
{
    public required SyntaxToken RevokeKeyword { get; init; }
    public RevokeOptionClause? Option { get; init; }
    public required IReadOnlyList<PrivilegeAction> Actions { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required PrivilegeObject Object { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Grantees { get; init; }
    public SyntaxToken? GrantedKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? Grantor { get; init; }
    public required SyntaxToken Behavior { get; init; }
}

public sealed class RevokeRoleStatement : Query
{
    public required SyntaxToken RevokeKeyword { get; init; }
    public RevokeOptionClause? Option { get; init; }
    public required IReadOnlyList<SyntaxToken> Roles { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Grantees { get; init; }
    public SyntaxToken? GrantedKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? Grantor { get; init; }
    public required SyntaxToken Behavior { get; init; }
}

public sealed class CreateRoleStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken RoleKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? AdminKeyword { get; init; }
    public SyntaxToken? Grantor { get; init; }
}

public sealed class DropRoleStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken RoleKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}

public sealed class SetRoleStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken RoleKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
}

public sealed class TransactionMode
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? IsolationKeyword { get; init; }
    public SyntaxToken? LevelKeyword { get; init; }
    public SyntaxToken? Access { get; init; }
    public SyntaxToken? Level { get; init; }
    public SyntaxToken? ReadKeyword { get; init; }
    public SyntaxToken? DiagnosticsKeyword { get; init; }
    public SyntaxToken? SizeKeyword { get; init; }
    public Expression? Size { get; init; }
}

public sealed class StartTransactionStatement : Query
{
    public required SyntaxToken StartKeyword { get; init; }
    public required SyntaxToken TransactionKeyword { get; init; }
    public IReadOnlyList<TransactionMode> Modes { get; init; } = [];
}

public sealed class SetTransactionStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public SyntaxToken? LocalKeyword { get; init; }
    public required SyntaxToken TransactionKeyword { get; init; }
    public required IReadOnlyList<TransactionMode> Modes { get; init; }
}

public sealed class CommitStatement : Query
{
    public required SyntaxToken CommitKeyword { get; init; }
    public SyntaxToken? WorkKeyword { get; init; }
    public SyntaxToken? AndKeyword { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? ChainKeyword { get; init; }
}

public sealed class RollbackStatement : Query
{
    public required SyntaxToken RollbackKeyword { get; init; }
    public SyntaxToken? WorkKeyword { get; init; }
    public SyntaxToken? AndKeyword { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public SyntaxToken? ChainKeyword { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public SyntaxToken? SavepointKeyword { get; init; }
    public SyntaxToken? Savepoint { get; init; }
}

public sealed class SavepointStatement : Query
{
    public required SyntaxToken SavepointKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}

public sealed class ReleaseSavepointStatement : Query
{
    public required SyntaxToken ReleaseKeyword { get; init; }
    public required SyntaxToken SavepointKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}

public sealed class SetConstraintsStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken ConstraintsKeyword { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>>? Names { get; init; }
    public required SyntaxToken Mode { get; init; }
}

public sealed class SetSessionAuthorizationStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken SessionKeyword { get; init; }
    public required SyntaxToken AuthorizationKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
}

public sealed class SetSessionCharacteristicsStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken SessionKeyword { get; init; }
    public required SyntaxToken CharacteristicsKeyword { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required IReadOnlyList<TransactionMode> Modes { get; init; }
}

public sealed class SetNamesStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken NamesKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
    public SyntaxToken? CollateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Collation { get; init; }
}

public sealed class SetCharacterSetStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken CharacterKeyword { get; init; }
    public required SyntaxToken CharacterSetKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
}

public sealed class SetCollationStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken CollationKeyword { get; init; }
    public SyntaxToken? Value { get; init; }
    public IReadOnlyList<SyntaxToken>? Name { get; init; }
}

public sealed class SetTimeZoneStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken TimeKeyword { get; init; }
    public required SyntaxToken ZoneKeyword { get; init; }
    public SyntaxToken? LocalKeyword { get; init; }
    public Expression? Value { get; init; }
}

public sealed class SetCatalogStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken CatalogKeyword { get; init; }
    public SyntaxToken? Value { get; init; }
    public IReadOnlyList<SyntaxToken>? Name { get; init; }
}

public sealed class SetSchemaStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken SchemaKeyword { get; init; }
    public SyntaxToken? Value { get; init; }
    public IReadOnlyList<SyntaxToken>? Name { get; init; }
}

public sealed class SetPathStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken PathKeyword { get; init; }
    public SyntaxToken? Value { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>>? Names { get; init; }
}

public sealed class ConnectStatement : Query
{
    public required SyntaxToken ConnectKeyword { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required SyntaxToken Target { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? ConnectionName { get; init; }
    public SyntaxToken? UserKeyword { get; init; }
    public SyntaxToken? User { get; init; }
}

public sealed class DisconnectStatement : Query
{
    public required SyntaxToken DisconnectKeyword { get; init; }
    public required SyntaxToken Target { get; init; }
}

public sealed class SetConnectionStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken ConnectionKeyword { get; init; }
    public required SyntaxToken Target { get; init; }
}

public sealed class DeclareCursorStatement : Query
{
    public required SyntaxToken DeclareKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? Sensitivity { get; init; }
    public SyntaxToken? NoScroll { get; init; }
    public SyntaxToken? Scroll { get; init; }
    public required SyntaxToken CursorKeyword { get; init; }
    public SyntaxToken? HoldWith { get; init; }
    public SyntaxToken? Hold { get; init; }
    public SyntaxToken? ReturnWith { get; init; }
    public SyntaxToken? ReturnKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required Query Query { get; init; }
}

public sealed class OpenStatement : Query
{
    public required SyntaxToken OpenKeyword { get; init; }
    public required SyntaxToken Cursor { get; init; }
}

public sealed class FetchStatement : Query
{
    public required SyntaxToken FetchKeyword { get; init; }
    public SyntaxToken? Orientation { get; init; }
    public Expression? Offset { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public required SyntaxToken Cursor { get; init; }
    public required SyntaxToken IntoKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Targets { get; init; }
}

public sealed class CloseStatement : Query
{
    public required SyntaxToken CloseKeyword { get; init; }
    public required SyntaxToken Cursor { get; init; }
}

public sealed class AllocateCursorStatement : Query
{
    public required SyntaxToken AllocateKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? Sensitivity { get; init; }
    public SyntaxToken? NoScroll { get; init; }
    public SyntaxToken? Scroll { get; init; }
    public required SyntaxToken CursorKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken Statement { get; init; }
}

public sealed class DeallocateStatement : Query
{
    public required SyntaxToken DeallocateKeyword { get; init; }
    public required SyntaxToken Kind { get; init; }
    public required SyntaxToken Name { get; init; }
}

public sealed class PrepareStatement : Query
{
    public required SyntaxToken PrepareKeyword { get; init; }
    public SyntaxToken? Scope { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required SyntaxToken Source { get; init; }
}

public sealed class ExecuteStatement : Query
{
    public required SyntaxToken ExecuteKeyword { get; init; }
    public SyntaxToken? Scope { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? IntoKeyword { get; init; }
    public SyntaxToken? IntoSqlKeyword { get; init; }
    public SyntaxToken? IntoDescriptorKeyword { get; init; }
    public SyntaxToken? IntoDescriptorScope { get; init; }
    public SyntaxToken? IntoDescriptor { get; init; }
    public IReadOnlyList<SyntaxToken>? Targets { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public SyntaxToken? UsingSqlKeyword { get; init; }
    public SyntaxToken? UsingDescriptorKeyword { get; init; }
    public SyntaxToken? UsingDescriptorScope { get; init; }
    public SyntaxToken? UsingDescriptor { get; init; }
    public IReadOnlyList<SyntaxToken>? Arguments { get; init; }
}

public sealed class ExecuteImmediateStatement : Query
{
    public required SyntaxToken ExecuteKeyword { get; init; }
    public required SyntaxToken ImmediateKeyword { get; init; }
    public required SyntaxToken Source { get; init; }
}

public sealed class DescribeStatement : Query
{
    public required SyntaxToken DescribeKeyword { get; init; }
    public SyntaxToken? InputOrOutput { get; init; }
    public SyntaxToken? Scope { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken UsingOrInto { get; init; }
    public SyntaxToken? SqlKeyword { get; init; }
    public required SyntaxToken DescriptorKeyword { get; init; }
    public SyntaxToken? DescriptorScope { get; init; }
    public required SyntaxToken Descriptor { get; init; }
}

public sealed class DynamicDeclareCursorStatement : Query
{
    public required SyntaxToken DeclareKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? Sensitivity { get; init; }
    public SyntaxToken? NoScroll { get; init; }
    public SyntaxToken? Scroll { get; init; }
    public required SyntaxToken CursorKeyword { get; init; }
    public SyntaxToken? HoldWith { get; init; }
    public SyntaxToken? Hold { get; init; }
    public SyntaxToken? ReturnWith { get; init; }
    public SyntaxToken? ReturnKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken Statement { get; init; }
}

public sealed class AllocateDescriptorStatement : Query
{
    public required SyntaxToken AllocateKeyword { get; init; }
    public SyntaxToken? SqlKeyword { get; init; }
    public required SyntaxToken DescriptorKeyword { get; init; }
    public SyntaxToken? Scope { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? MaxKeyword { get; init; }
    public SyntaxToken? Occurrences { get; init; }
}

public sealed class DiagnosticsItem
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Target { get; init; }
    public required SyntaxToken EqualsToken { get; init; }
    public required SyntaxToken Name { get; init; }
}

public sealed class GetDiagnosticsStatement : Query
{
    public required SyntaxToken GetKeyword { get; init; }
    public required SyntaxToken DiagnosticsKeyword { get; init; }
    public SyntaxToken? ConditionKeyword { get; init; }
    public Expression? ConditionNumber { get; init; }
    public required IReadOnlyList<DiagnosticsItem> Items { get; init; }
}

public sealed class SignalInformation
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken EqualsToken { get; init; }
    public required Expression Value { get; init; }
}

public sealed class SignalStatement : Query
{
    public required SyntaxToken SignalKeyword { get; init; }
    public SyntaxToken? SqlStateKeyword { get; init; }
    public SyntaxToken? ValueKeyword { get; init; }
    public SyntaxToken? State { get; init; }
    public SyntaxToken? Condition { get; init; }
    public SyntaxToken? SetKeyword { get; init; }
    public IReadOnlyList<SignalInformation> Items { get; init; } = [];
}

public sealed class ResignalStatement : Query
{
    public required SyntaxToken ResignalKeyword { get; init; }
    public SyntaxToken? SqlStateKeyword { get; init; }
    public SyntaxToken? ValueKeyword { get; init; }
    public SyntaxToken? State { get; init; }
    public SyntaxToken? Condition { get; init; }
    public SyntaxToken? SetKeyword { get; init; }
    public IReadOnlyList<SignalInformation> Items { get; init; } = [];
}

public sealed class CompoundStatement : Query
{
    public SyntaxToken? Label { get; init; }
    public SyntaxToken? Colon { get; init; }
    public required SyntaxToken BeginKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? AtomicKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
    public required SyntaxToken EndKeyword { get; init; }
    public SyntaxToken? EndLabel { get; init; }
}

public sealed class DeclareVariableStatement : Query
{
    public required SyntaxToken DeclareKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Names { get; init; }
    public required DataType Type { get; init; }
    public DefaultClause? Default { get; init; }
}

public sealed class DeclareConditionStatement : Query
{
    public required SyntaxToken DeclareKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken ConditionKeyword { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? SqlStateKeyword { get; init; }
    public SyntaxToken? ValueKeyword { get; init; }
    public SyntaxToken? State { get; init; }
}

public sealed class ConditionValue
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? SqlStateKeyword { get; init; }
    public SyntaxToken? ValueKeyword { get; init; }
    public SyntaxToken? State { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}

public sealed class DeclareHandlerStatement : Query
{
    public required SyntaxToken DeclareKeyword { get; init; }
    public required SyntaxToken HandlerType { get; init; }
    public required SyntaxToken HandlerKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required IReadOnlyList<ConditionValue> Conditions { get; init; }
    public required Query Action { get; init; }
}

public sealed class SetAssignmentStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public required IReadOnlyList<IReadOnlyList<SyntaxToken>> Targets { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken EqualsToken { get; init; }
    public required Expression Value { get; init; }
}

public sealed class ElseIfClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ElseIfKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required SyntaxToken ThenKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
}

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

public sealed class CaseStatementWhen
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken WhenKeyword { get; init; }
    public required Expression Operand { get; init; }
    public required SyntaxToken ThenKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
}

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

public sealed class LoopStatement : Query
{
    public SyntaxToken? Label { get; init; }
    public SyntaxToken? Colon { get; init; }
    public required SyntaxToken LoopKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
    public required SyntaxToken EndKeyword { get; init; }
    public required SyntaxToken EndLoopKeyword { get; init; }
    public SyntaxToken? EndLabel { get; init; }
}

public sealed class WhileStatement : Query
{
    public SyntaxToken? Label { get; init; }
    public SyntaxToken? Colon { get; init; }
    public required SyntaxToken WhileKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required SyntaxToken DoKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
    public required SyntaxToken EndKeyword { get; init; }
    public required SyntaxToken EndWhileKeyword { get; init; }
    public SyntaxToken? EndLabel { get; init; }
}

public sealed class RepeatStatement : Query
{
    public SyntaxToken? Label { get; init; }
    public SyntaxToken? Colon { get; init; }
    public required SyntaxToken RepeatKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
    public required SyntaxToken UntilKeyword { get; init; }
    public required Expression Condition { get; init; }
    public required SyntaxToken EndKeyword { get; init; }
    public required SyntaxToken EndRepeatKeyword { get; init; }
    public SyntaxToken? EndLabel { get; init; }
}

public sealed class ForStatement : Query
{
    public SyntaxToken? Label { get; init; }
    public SyntaxToken? Colon { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken Variable { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public SyntaxToken? CursorName { get; init; }
    public SyntaxToken? CursorKeyword { get; init; }
    public SyntaxToken? CursorForKeyword { get; init; }
    public required Query CursorQuery { get; init; }
    public required SyntaxToken DoKeyword { get; init; }
    public IReadOnlyList<Query> Statements { get; init; } = [];
    public required SyntaxToken EndKeyword { get; init; }
    public required SyntaxToken EndForKeyword { get; init; }
    public SyntaxToken? EndLabel { get; init; }
}

public sealed class LeaveStatement : Query
{
    public required SyntaxToken LeaveKeyword { get; init; }
    public required SyntaxToken Label { get; init; }
}

public sealed class IterateStatement : Query
{
    public required SyntaxToken IterateKeyword { get; init; }
    public required SyntaxToken Label { get; init; }
}

public sealed class TransitionClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Kind { get; init; }
    public SyntaxToken? RowOrTable { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}

public sealed class CreateTriggerStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken TriggerKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken ActionTime { get; init; }
    public SyntaxToken? OfKeyword { get; init; }
    public required SyntaxToken Event { get; init; }
    public SyntaxToken? ColumnsOfKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> TableName { get; init; }
    public SyntaxToken? ReferencingKeyword { get; init; }
    public IReadOnlyList<TransitionClause> Transitions { get; init; } = [];
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? EachKeyword { get; init; }
    public SyntaxToken? Granularity { get; init; }
    public SyntaxToken? WhenKeyword { get; init; }
    public SyntaxToken? WhenOpen { get; init; }
    public Expression? When { get; init; }
    public SyntaxToken? WhenClose { get; init; }
    public required Query Body { get; init; }
}

public sealed class DropTriggerStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken TriggerKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
}

public sealed class CreateRoutineStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public SyntaxToken? KindPrefix { get; init; }
    public required SyntaxToken RoutineKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public IReadOnlyList<RoutineParameter> Parameters { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? ReturnsKeyword { get; init; }
    public DataType? ReturnsType { get; init; }
    public SyntaxToken? ReturnsAsKeyword { get; init; }
    public SyntaxToken? ReturnsLocator { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? ForType { get; init; }
    public IReadOnlyList<RoutineCharacteristic> Characteristics { get; init; } = [];
    public SyntaxToken? SqlKeyword { get; init; }
    public Query? Body { get; init; }
    public SyntaxToken? ExternalKeyword { get; init; }
    public SyntaxToken? NameKeyword { get; init; }
    public SyntaxToken? ExternalName { get; init; }
}

public sealed class AlterRoutineStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required RoutineDesignator Designator { get; init; }
    public IReadOnlyList<RoutineCharacteristic> Characteristics { get; init; } = [];
    public SyntaxToken? SqlKeyword { get; init; }
    public Query? Body { get; init; }
    public SyntaxToken? ExternalKeyword { get; init; }
    public SyntaxToken? NameKeyword { get; init; }
    public SyntaxToken? ExternalName { get; init; }
}

public sealed class DropRoutineStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required RoutineDesignator Designator { get; init; }
    public required SyntaxToken Behavior { get; init; }
}

public sealed class CallStatement : Query
{
    public required SyntaxToken CallKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
}

public sealed class ReturnStatement : Query
{
    public required SyntaxToken ReturnKeyword { get; init; }
    public required Expression Value { get; init; }
}
