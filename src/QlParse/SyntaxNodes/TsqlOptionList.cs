namespace QlParse;

// A comma-separated SQL Server option list, parenthesised or (after WITH, SET, ...) bare.
public sealed class TsqlOptionList
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public required IReadOnlyList<TsqlOption> Options { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}
