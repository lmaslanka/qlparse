namespace QlParse;

public sealed class CheckpointStatement : Query
{
    public required SyntaxToken CheckpointKeyword { get; init; }
}
