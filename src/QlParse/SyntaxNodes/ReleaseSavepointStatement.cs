namespace QlParse;

public sealed class ReleaseSavepointStatement : Query
{
    public required SyntaxToken ReleaseKeyword { get; init; }
    public required SyntaxToken SavepointKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}
