namespace QlParse;

public sealed class PgOptionList
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OptionsKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public IReadOnlyList<PgOptionItem> Items { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
}
