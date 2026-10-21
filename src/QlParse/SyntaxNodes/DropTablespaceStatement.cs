namespace QlParse;

public sealed class DropTablespaceStatement : Query
{
    public required SyntaxToken DropKeyword { get; init; }
    public required SyntaxToken TablespaceKeyword { get; init; }
    public ExistenceCheck? IfExists { get; init; }
    public required SyntaxToken Name { get; init; }
}
