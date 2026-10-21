namespace QlParse;

public sealed class ShowStatement : Query
{
    public required SyntaxToken ShowKeyword { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Name { get; init; } = [];
}
