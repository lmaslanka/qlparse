namespace QlParse;

public sealed class DeclareVariableStatement : Query
{
    public required SyntaxToken DeclareKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Names { get; init; }
    public required DataType Type { get; init; }
    public DefaultClause? Default { get; init; }
}
