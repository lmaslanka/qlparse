namespace QlParse;

public sealed class PgOwnerAction : PgAlterAction
{
    public required SyntaxToken OwnerKeyword { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required SyntaxToken Owner { get; init; }
}
