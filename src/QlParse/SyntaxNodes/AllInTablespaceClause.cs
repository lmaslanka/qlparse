namespace QlParse;

public sealed class AllInTablespaceClause
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken AllKeyword { get; init; }
    public required SyntaxToken InKeyword { get; init; }
    public required SyntaxToken TablespaceKeyword { get; init; }
    public required SyntaxToken Tablespace { get; init; }
    public SyntaxToken? OwnedKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Roles { get; init; } = [];
}
