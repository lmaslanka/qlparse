namespace QlParse;

// Postgres `ALTER TABLE ... SET ACCESS METHOD method`.
public sealed class SetAccessMethodAction : AlterTableAction
{
    public required SyntaxToken SetKeyword { get; init; }
    public required SyntaxToken AccessKeyword { get; init; }
    public required SyntaxToken MethodKeyword { get; init; }
    public required SyntaxToken Method { get; init; }
}
