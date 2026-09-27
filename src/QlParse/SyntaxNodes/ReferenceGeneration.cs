namespace QlParse;

public sealed class ReferenceGeneration
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken RefKeyword { get; init; }
    public SyntaxToken? IsKeyword { get; init; }
    public SyntaxToken? SystemKeyword { get; init; }
    public SyntaxToken? GeneratedKeyword { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public DataType? PredefinedType { get; init; }
    public SyntaxToken? FromKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Attributes { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}
