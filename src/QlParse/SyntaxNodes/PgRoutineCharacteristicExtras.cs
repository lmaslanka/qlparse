namespace QlParse;

// Postgres routine characteristic payloads (TRANSFORM FOR TYPE types, SET values);
// allocated only when at least one is present.
public sealed class PgRoutineCharacteristicExtras
{
    public IReadOnlyList<DataType> Types { get; init; } = [];
    public IReadOnlyList<OptionValue> Values { get; init; } = [];
}
