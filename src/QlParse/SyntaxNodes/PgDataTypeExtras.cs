namespace QlParse;

// Postgres-only parts of a data type (interval field restriction, `[n]` array bounds);
// allocated only when at least one is present.
public sealed class PgDataTypeExtras
{
    public IntervalQualifier? IntervalQualifier { get; init; }
    public IReadOnlyList<ArrayBound> ArrayBounds { get; init; } = [];
}
