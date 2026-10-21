namespace QlParse;

public sealed class PgRoutineAttributesAction : PgAlterAction
{
    public IReadOnlyList<RoutineCharacteristic> Characteristics { get; init; } = [];
    public SyntaxToken? RestrictKeyword { get; init; }
}
