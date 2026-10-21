namespace QlParse;

// CURSOR [options] FOR select [FOR {READ ONLY | UPDATE [OF columns]}]; the trailing FOR clause is the
// query's lock clause.
public sealed class TsqlCursorDefinition
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken CursorKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Options { get; init; } = [];
    public required SyntaxToken ForKeyword { get; init; }
    public required Query Query { get; init; }
}
