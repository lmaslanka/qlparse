namespace QlParse;

public sealed class PlpgsqlVariableDeclaration : Query
{
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? ConstantKeyword { get; init; }
    public required DataType Type { get; init; }
    public PgTypeAttribute? TypeAttribute { get; init; }
    public SyntaxToken? CollateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Collation { get; init; } = [];
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? NullKeyword { get; init; }
    public SyntaxToken? AssignmentToken { get; init; }
    public Expression? Default { get; init; }
}
