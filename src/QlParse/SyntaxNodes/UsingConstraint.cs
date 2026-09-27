namespace QlParse;

public sealed class UsingConstraint : JoinConstraint
{
    public required SyntaxToken UsingKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<SyntaxToken> Columns { get; init; }
    public required SyntaxToken CloseParen { get; init; }
}
