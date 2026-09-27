namespace QlParse;

public sealed class ConnectStatement : Query
{
    public required SyntaxToken ConnectKeyword { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required SyntaxToken Target { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? ConnectionName { get; init; }
    public SyntaxToken? UserKeyword { get; init; }
    public SyntaxToken? User { get; init; }
}
