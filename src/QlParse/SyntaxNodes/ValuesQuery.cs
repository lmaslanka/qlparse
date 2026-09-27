namespace QlParse;

public sealed class ValuesQuery : Query
{
    public required SyntaxToken ValuesKeyword { get; init; }
    public required IReadOnlyList<ValuesRow> Rows { get; init; }
}
