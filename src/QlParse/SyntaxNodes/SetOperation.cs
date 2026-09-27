namespace QlParse;

public sealed class SetOperation : Query
{
    public required Query Left { get; init; }
    public required SyntaxToken Operator { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public SyntaxToken? CorrespondingKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public required Query Right { get; init; }
}
