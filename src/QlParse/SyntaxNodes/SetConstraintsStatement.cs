namespace QlParse;

public sealed class SetConstraintsStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken ConstraintsKeyword { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>>? Names { get; init; }
    public required SyntaxToken Mode { get; init; }
}
