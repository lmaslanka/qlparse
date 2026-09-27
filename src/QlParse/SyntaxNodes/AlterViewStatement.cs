namespace QlParse;

public sealed class AlterViewStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken ViewKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required AlterViewAction Action { get; init; }
}
