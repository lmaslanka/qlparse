namespace QlParse;

public sealed class ResetStatement : Query
{
    public required SyntaxToken ResetKeyword { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Name { get; init; } = [];
}
