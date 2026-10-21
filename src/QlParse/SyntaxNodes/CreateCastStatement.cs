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
    public RoutineDesignator? Routine { get; init; }
    public SyntaxToken? FunctionKeyword { get; init; }
    public SyntaxToken? InoutKeyword { get; init; }
    public SyntaxToken? ArgumentsOpenParen { get; init; }
    public IReadOnlyList<PgRoutineArgument> Arguments { get; init; } = [];
    public SyntaxToken? ArgumentsCloseParen { get; init; }
    public SyntaxToken? AssignmentAsKeyword { get; init; }
    public SyntaxToken? AssignmentKeyword { get; init; }
}
