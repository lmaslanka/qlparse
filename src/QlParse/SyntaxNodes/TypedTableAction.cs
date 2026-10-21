namespace QlParse;

// Postgres `ALTER TABLE ... OF type_name` (TypeName set) or `ALTER TABLE ... NOT OF` (NotKeyword set).
public sealed class TypedTableAction : AlterTableAction
{
    public SyntaxToken? NotKeyword { get; init; }
    public required SyntaxToken OfKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> TypeName { get; init; } = [];
}
