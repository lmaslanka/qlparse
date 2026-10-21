namespace QlParse;

public sealed class PgAddEnumValueAction : PgAlterAction
{
    public required SyntaxToken AddKeyword { get; init; }
    public required SyntaxToken ValueKeyword { get; init; }
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public required SyntaxToken Value { get; init; }
    public SyntaxToken? Placement { get; init; }
    public SyntaxToken? Neighbor { get; init; }
}
