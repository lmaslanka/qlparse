namespace QlParse;

public sealed class ReleaseSavepointStatement : Query
{
    public required SyntaxToken ReleaseKeyword { get; init; }
    public SyntaxToken? SavepointKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
}
