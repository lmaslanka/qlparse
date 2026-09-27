namespace QlParse;

public sealed class SavepointStatement : Query
{
    public required SyntaxToken SavepointKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}
