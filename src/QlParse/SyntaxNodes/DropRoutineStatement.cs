namespace QlParse;

public sealed class DropRoutineStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required RoutineDesignator Designator { get; init; }
    public required SyntaxToken Behavior { get; init; }
}
