namespace QlParse;

// CREATE ASSEMBLY name [AUTHORIZATION owner] FROM {'path' | 0x...} [, ...] [WITH PERMISSION_SET = {SAFE | EXTERNAL_ACCESS | UNSAFE}].
public sealed class TsqlCreateAssemblyStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken AssemblyKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Owner { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required IReadOnlyList<Expression> Sources { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlObjectOption? Option { get; init; }
}
