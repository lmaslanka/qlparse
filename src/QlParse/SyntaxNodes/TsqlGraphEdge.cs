namespace QlParse;

// An edge step: `-(edge)->` (LessThan absent, Arrow is `->`) or `<-(edge)-` (Arrow is `-`).
public sealed class TsqlGraphEdge : TsqlGraphElement
{
    public SyntaxToken? LessThan { get; init; }
    public required SyntaxToken LeadingDash { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public required SyntaxToken Arrow { get; init; }
}
