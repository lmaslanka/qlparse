namespace QlParse;

public sealed class DropDatabaseStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken DatabaseKeyword { get; init; }
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<KeywordOption> Options { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}
