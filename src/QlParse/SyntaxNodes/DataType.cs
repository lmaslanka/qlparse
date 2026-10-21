namespace QlParse;

public sealed class DataType
{
    // One slot holds the dialect-only parts: a type is parsed under one dialect's grammar, so it carries at
    // most one of them, and ISO types pay for a single null reference rather than one per dialect.
    private readonly object? _dialectExtras;

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
    public PgDataTypeExtras? Postgres
    {
        get => _dialectExtras as PgDataTypeExtras;
        init => _dialectExtras ??= value;
    }

    public TsqlDataTypeExtras? SqlServer
    {
        get => _dialectExtras as TsqlDataTypeExtras;
        init => _dialectExtras ??= value;
    }
}
