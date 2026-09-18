namespace QlParse;

public readonly record struct SyntaxToken(
    SyntaxKind Kind,
    int Position,
    int Length,
    int LeadingTriviaStart,
    int LeadingTriviaCount)
{
    public ReadOnlySpan<char> TextOf(string source) => source.AsSpan(Position, Length);
}
