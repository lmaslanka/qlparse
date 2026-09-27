namespace QlParse;

public sealed class CreateSequenceStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken SequenceKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public DataType? DataType { get; init; }
    public IReadOnlyList<SequenceOption> Options { get; init; } = [];
}
