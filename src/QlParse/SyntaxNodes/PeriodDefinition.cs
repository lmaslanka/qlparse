namespace QlParse;

public sealed class PeriodDefinition : TableElement
{
    public required SyntaxToken PeriodKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken StartColumn { get; init; }
    public required SyntaxToken Comma { get; init; }
    public required SyntaxToken EndColumn { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
