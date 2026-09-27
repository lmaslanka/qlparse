namespace QlParse;

public sealed class PatternAlternation : RowPattern
{
    public required IReadOnlyList<RowPattern> Terms { get; init; }
}
