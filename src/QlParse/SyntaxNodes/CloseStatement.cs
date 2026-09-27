namespace QlParse;

public sealed class CloseStatement : Query
{
    public required SyntaxToken CloseKeyword { get; init; }
    public required SyntaxToken Cursor { get; init; }
}
