namespace QlParse;

public sealed class ParenQuery : Query
{
    public required SyntaxToken OpenParen { get; init; }
    public required Query Inner { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
