namespace QlParse;

public sealed class SetAssignmentStatement : Query
{
    public required SyntaxToken SetKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public required IReadOnlyList<IReadOnlyList<SyntaxToken>> Targets { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken EqualsToken { get; init; }
    public required Expression Value { get; init; }
}
