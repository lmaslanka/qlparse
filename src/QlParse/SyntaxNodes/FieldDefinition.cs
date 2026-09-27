namespace QlParse;

public sealed class FieldDefinition
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required DataType Type { get; init; }
}
