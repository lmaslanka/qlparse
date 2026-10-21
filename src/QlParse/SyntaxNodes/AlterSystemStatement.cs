namespace QlParse;

public sealed class AlterSystemStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken SystemKeyword { get; init; }
    public required Query Action { get; init; }
}
