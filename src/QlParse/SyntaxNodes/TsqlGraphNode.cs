namespace QlParse;

// A node table alias in a MATCH pattern, or `LAST_NODE(alias)`.
public sealed class TsqlGraphNode : TsqlGraphElement
{
    public SyntaxToken? LastNodeKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}
