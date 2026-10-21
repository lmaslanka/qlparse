namespace QlParse;

// ALTER AUTHORIZATION ON [class ::] entity TO {principal | SCHEMA OWNER}, where Principal is null for SCHEMA OWNER.
public sealed class TsqlAlterAuthorizationStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken AuthorizationKeyword { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required TsqlSecurable Securable { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public SyntaxToken? Principal { get; init; }
    public SyntaxToken? SchemaKeyword { get; init; }
    public SyntaxToken? OwnerKeyword { get; init; }
}
