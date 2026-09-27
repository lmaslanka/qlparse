namespace QlParse;

public sealed class SetPathStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken PathKeyword { get; init; }
    public SyntaxToken? Value { get; init; }
    public IReadOnlyList<IReadOnlyList<SyntaxToken>>? Names { get; init; }
}
