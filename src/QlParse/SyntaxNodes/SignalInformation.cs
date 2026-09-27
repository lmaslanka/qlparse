namespace QlParse;

public sealed class SignalInformation
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken EqualsToken { get; init; }
    public required Expression Value { get; init; }
}
