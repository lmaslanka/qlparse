namespace QlParse;

// One `@name [AS] type [= value]` of a DECLARE. Exactly one of DataType, CursorKeyword and Table is set.
public sealed class TsqlVariableDeclaration
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public DataType? DataType { get; init; }
    public SyntaxToken? CursorKeyword { get; init; }
    public TsqlTableDefinition? Table { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public Expression? Value { get; init; }
}
