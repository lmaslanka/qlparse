namespace QlParse;

// CREATE AGGREGATE name (@param type, ...) RETURNS type EXTERNAL NAME assembly.class.
public sealed class TsqlCreateAggregateStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken AggregateKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<TsqlParameter> Parameters { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public required SyntaxToken ReturnsKeyword { get; init; }
    public required DataType ReturnType { get; init; }
    public required TsqlExternalName ExternalName { get; init; }
}
