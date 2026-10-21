namespace QlParse;

public sealed class IndexParameters
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? IncludeKeyword { get; init; }
    public SyntaxToken? IncludeOpen { get; init; }
    public IReadOnlyList<SyntaxToken>? Include { get; init; }
    public SyntaxToken? IncludeClose { get; init; }
    public SyntaxToken? NullsKeyword { get; init; }
    public SyntaxToken? NullsNotKeyword { get; init; }
    public SyntaxToken? DistinctKeyword { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public StorageParameterList? Parameters { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public SyntaxToken? IndexKeyword { get; init; }
    public SyntaxToken? TablespaceKeyword { get; init; }
    public SyntaxToken? Tablespace { get; init; }
}
