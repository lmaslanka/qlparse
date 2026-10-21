namespace QlParse;

public sealed class LoadStatement : Query
{
    public required SyntaxToken LoadKeyword { get; init; }
    public required SyntaxToken FileName { get; init; }
}
