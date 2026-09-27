namespace QlParse;

public sealed class ReplaceViewAction : AlterViewAction
{
    public required SyntaxToken AsKeyword { get; init; }
    public required Query Query { get; init; }
}
