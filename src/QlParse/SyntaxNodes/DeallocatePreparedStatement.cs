namespace QlParse;

public sealed class DeallocatePreparedStatement : Query
{
    public required SyntaxToken DeallocateKeyword { get; init; }
    public SyntaxToken? PrepareKeyword { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public SyntaxToken? Name { get; init; }
}
