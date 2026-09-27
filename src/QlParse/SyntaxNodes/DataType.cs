namespace QlParse;

public sealed class DataType
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public IReadOnlyList<SyntaxToken> NameTail { get; init; } = [];
    public SyntaxToken? OpenParen { get; init; }
    public SyntaxToken? Precision { get; init; }
    public SyntaxToken? Scale { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? TimeKeyword { get; init; }
    public SyntaxToken? Zone { get; init; }
    public IReadOnlyList<CollectionSuffix> Collections { get; init; } = [];
    public IReadOnlyList<FieldDefinition>? Fields { get; init; }
    public DataType? ReferencedType { get; init; }
    public SyntaxToken? ScopeKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? ScopeName { get; init; }
    public SyntaxToken? Modifier { get; init; }
}
