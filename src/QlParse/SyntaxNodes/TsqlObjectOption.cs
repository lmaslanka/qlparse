namespace QlParse;

// One SQL Server option or clause: `KEYWORDS [= value] [(nested, ...)]`, e.g. `SIZE = 10 MB`,
// `RECOVERY SIMPLE`, `PASSWORD = 'p' MUST_CHANGE`, `ENCRYPTION BY PASSWORD = 'p'`, `FORMAT_OPTIONS (FIELD_TERMINATOR = ',')`.
// Value holds the value's tokens in source order (a unit or `%` after a number, flags after a string, dotted names);
// Keywords is empty only for a parenthesized list that continues the previous clause (`WITH VALUES (...), (...)`).
public sealed class TsqlObjectOption
{
    public required SourceSpan Span { get; init; }
    public required IReadOnlyList<SyntaxToken> Keywords { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public IReadOnlyList<SyntaxToken> Value { get; init; } = [];
    public TsqlObjectOptionList? Options { get; init; }
}
