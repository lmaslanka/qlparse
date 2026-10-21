namespace QlParse;

// One item of a SQL Server option list: `name [=] value`, a bare flag `name`, a bare value such as `'a'` in
// `= ('a', 'b')`, or `name [=] (nested, ...)`. Name holds the option words (empty for a bare value) and Value
// the value's tokens in source order (a sign, a number and its unit, `'logical' TO 'physical'`, several words).
public sealed class TsqlOption
{
    public required SourceSpan Span { get; init; }
    public IReadOnlyList<SyntaxToken> Name { get; init; } = [];
    public SyntaxToken? EqualsToken { get; init; }
    public IReadOnlyList<SyntaxToken> Value { get; init; } = [];
    public TsqlOptionList? Options { get; init; }
}
