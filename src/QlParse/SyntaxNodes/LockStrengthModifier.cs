namespace QlParse;

// The NO KEY of `FOR NO KEY UPDATE`, or the KEY of `FOR KEY SHARE`.
public sealed class LockStrengthModifier
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? NoKeyword { get; init; }
    public required SyntaxToken KeyKeyword { get; init; }
}
