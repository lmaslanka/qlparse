namespace QlParse;

public sealed class LikeOption
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken KindKeyword { get; init; }
    public required SyntaxToken Option { get; init; }
}
