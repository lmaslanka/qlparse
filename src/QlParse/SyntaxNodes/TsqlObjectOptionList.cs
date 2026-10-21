namespace QlParse;

// A comma-separated SQL Server option list, parenthesized (`(NAME = n, SIZE = 10)`) or bare (`WITH a = 1, b = 2`).
public sealed class TsqlObjectOptionList
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public required IReadOnlyList<TsqlObjectOption> Items { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}
