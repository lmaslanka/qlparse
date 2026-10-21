namespace QlParse;

// One column of a rowset WITH schema: `name type ['path' | ordinal] [AS JSON]`.
public sealed class TsqlRowsetColumn
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required DataType Type { get; init; }
    public Expression? Path { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? JsonKeyword { get; init; }
}
