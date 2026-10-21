namespace QlParse;

// RETURN [expr]; an inline table-valued function body is RETURN [(] select [)], kept in Query.
public sealed class TsqlReturnStatement : Query
{
    public required SyntaxToken ReturnKeyword { get; init; }
    public Expression? Value { get; init; }
    public Query? Query { get; init; }
}
