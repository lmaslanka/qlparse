namespace QlParse;

public sealed class StorageParameterList
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<StorageParameter> Parameters { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
