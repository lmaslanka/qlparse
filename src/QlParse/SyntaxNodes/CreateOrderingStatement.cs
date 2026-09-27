namespace QlParse;

public sealed class CreateOrderingStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken OrderingKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? EqualsKeyword { get; init; }
    public SyntaxToken? OrderKeyword { get; init; }
    public required SyntaxToken Form { get; init; }
    public required SyntaxToken ByKeyword { get; init; }
    public required SyntaxToken Category { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public RoutineDesignator? Routine { get; init; }
}
