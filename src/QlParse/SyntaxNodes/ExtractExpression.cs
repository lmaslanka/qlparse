namespace QlParse;

public sealed class ExtractExpression : Expression
{
    public required SyntaxToken ExtractKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken Field { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required Expression Source { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
