namespace QlParse;

// `CONTAINS(columns, 'search condition' [, LANGUAGE l])` and `FREETEXT(columns, 'text' [, LANGUAGE l])`.
// Columns is a column, a parenthesized column list (a row constructor), `*`, or `PROPERTY(column, 'name')`.
public sealed class TsqlFullTextPredicateExpression : Expression
{
    public required SyntaxToken FunctionName { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Columns { get; init; }
    public required SyntaxToken Comma { get; init; }
    public required Expression SearchCondition { get; init; }
    public SyntaxToken? LanguageComma { get; init; }
    public SyntaxToken? LanguageKeyword { get; init; }
    public Expression? Language { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
