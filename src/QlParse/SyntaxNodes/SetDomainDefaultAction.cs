namespace QlParse;

public sealed class SetDomainDefaultAction : AlterDomainAction
{
    public required SyntaxToken SetKeyword { get; init; }
    public required DefaultClause Default { get; init; }
}
