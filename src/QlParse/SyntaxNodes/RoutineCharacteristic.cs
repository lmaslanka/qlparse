namespace QlParse;

public sealed class RoutineCharacteristic
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public IReadOnlyList<SyntaxToken> Tail { get; init; } = [];
}
