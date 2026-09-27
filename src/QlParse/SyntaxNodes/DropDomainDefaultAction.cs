namespace QlParse;

public sealed class DropDomainDefaultAction : AlterDomainAction
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken DefaultKeyword { get; init; }
}
