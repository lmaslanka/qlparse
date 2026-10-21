namespace QlParse;

// One RESULT SETS entry: (column type [NULL|NOT NULL], ...), or AS {OBJECT|TYPE} name, or AS FOR XML.
public sealed class TsqlResultSetDefinition
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<TsqlResultSetColumn> Columns { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Kind { get; init; } = [];
    public IReadOnlyList<SyntaxToken> Name { get; init; } = [];
}
