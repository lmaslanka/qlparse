namespace QlParse;

public sealed class CreateAssertionStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken AssertionKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken CheckKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required Expression Check { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? DeferrableNotKeyword { get; init; }
    public SyntaxToken? DeferrableKeyword { get; init; }
    public SyntaxToken? InitiallyKeyword { get; init; }
    public SyntaxToken? InitiallyWhen { get; init; }
}
