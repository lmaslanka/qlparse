namespace QlParse;

public sealed class AlterRoutineStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required RoutineDesignator Designator { get; init; }
    public IReadOnlyList<RoutineCharacteristic> Characteristics { get; init; } = [];
    public SyntaxToken? SqlKeyword { get; init; }
    public Query? Body { get; init; }
    public SyntaxToken? ExternalKeyword { get; init; }
    public SyntaxToken? NameKeyword { get; init; }
    public SyntaxToken? ExternalName { get; init; }
}
