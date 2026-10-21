namespace QlParse;

// The TABLE (column and constraint definitions) type of a table variable or a multi-statement function result.
public sealed class TsqlTableDefinition
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken TableKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<TableElement> Elements { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
