namespace QlParse;

public sealed class RowsFromFunction
{
    public required SourceSpan Span { get; init; }
    public required Expression Function { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<FieldDefinition> ColumnDefinitions { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}
