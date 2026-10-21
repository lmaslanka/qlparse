namespace QlParse;

// Under SQL Server the alias may be a string literal, and `alias = expression` sets Alias with the `=` token
// in AsKeyword (Span then starts at the alias).
public sealed class SelectItem
{
    public required SourceSpan Span { get; init; }
    public required Expression Expression { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}
