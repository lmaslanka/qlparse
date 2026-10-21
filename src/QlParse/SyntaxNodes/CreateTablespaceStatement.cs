namespace QlParse;

public sealed class CreateTablespaceStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken TablespaceKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? OwnerKeyword { get; init; }
    public SyntaxToken? Owner { get; init; }
    public required SyntaxToken LocationKeyword { get; init; }
    public required SyntaxToken Location { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public StorageParameterList? Options { get; init; }
}
