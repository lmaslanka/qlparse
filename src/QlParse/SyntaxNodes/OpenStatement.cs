namespace QlParse;

public sealed class OpenStatement : Query
{
    public required SyntaxToken OpenKeyword { get; init; }
    public required SyntaxToken Cursor { get; init; }
}
