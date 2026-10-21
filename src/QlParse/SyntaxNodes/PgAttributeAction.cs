namespace QlParse;

public sealed class PgAttributeAction : PgAlterAction
{
    public required SyntaxToken Action { get; init; }
    public required SyntaxToken AttributeKeyword { get; init; }
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? SetKeyword { get; init; }
    public SyntaxToken? DataKeyword { get; init; }
    public SyntaxToken? TypeKeyword { get; init; }
    public DataType? Type { get; init; }
    public SyntaxToken? CollateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? Collation { get; init; }
    public SyntaxToken? Behavior { get; init; }
}
