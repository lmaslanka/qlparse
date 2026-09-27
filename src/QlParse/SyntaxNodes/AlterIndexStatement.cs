namespace QlParse;

public sealed class AlterIndexStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken IndexKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? RenameKeyword { get; init; }
    public SyntaxToken? ToKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? NewName { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? TableName { get; init; }
    public SyntaxToken? Action { get; init; }
}
