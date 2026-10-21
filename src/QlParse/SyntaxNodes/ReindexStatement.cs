namespace QlParse;

public sealed class ReindexStatement : Query
{
    public required SyntaxToken ReindexKeyword { get; init; }
    public StorageParameterList? Options { get; init; }
    public required SyntaxToken Kind { get; init; }
    public SyntaxToken? ConcurrentlyKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Name { get; init; } = [];
}
