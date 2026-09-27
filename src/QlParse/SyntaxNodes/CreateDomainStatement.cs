namespace QlParse;

public sealed class CreateDomainStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken DomainKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public required DataType Type { get; init; }
    public DefaultClause? Default { get; init; }
    public IReadOnlyList<DomainConstraint> Constraints { get; init; } = [];
    public SyntaxToken? CollateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Collation { get; init; }
}
