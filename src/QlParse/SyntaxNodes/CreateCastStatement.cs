namespace QlParse;

public sealed class CreateCastStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken CastKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required DataType Source { get; init; }
    public required SyntaxToken AsKeyword { get; init; }
    public required DataType Target { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required RoutineDesignator Routine { get; init; }
    public SyntaxToken? AssignmentAsKeyword { get; init; }
    public SyntaxToken? AssignmentKeyword { get; init; }
}
