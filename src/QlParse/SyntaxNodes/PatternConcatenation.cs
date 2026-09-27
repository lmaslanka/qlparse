namespace QlParse;

public sealed class PatternConcatenation : RowPattern
{
    public required IReadOnlyList<RowPattern> Factors { get; init; }
}
