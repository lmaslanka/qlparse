namespace QlParse;

public sealed class DisconnectStatement : Query
{
    public required SyntaxToken DisconnectKeyword { get; init; }
    public required SyntaxToken Target { get; init; }
}
