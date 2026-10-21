namespace QlParse;

// Postgres `LATERAL` before a FROM item that is not a parenthesized subquery: a function call,
// `ROWS FROM (...)`, UNNEST, or another table function. `LATERAL (query)` stays a DerivedTable.
public sealed class LateralTable : TableSource
{
    public required SyntaxToken LateralKeyword { get; init; }
    public required TableSource Table { get; init; }
}
