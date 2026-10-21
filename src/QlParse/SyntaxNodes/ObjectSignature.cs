namespace QlParse;

public sealed class ObjectSignature
{
    public required SourceSpan Span { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<DataType> ArgumentTypes { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}
