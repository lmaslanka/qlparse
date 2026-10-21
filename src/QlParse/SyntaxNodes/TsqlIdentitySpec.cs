namespace QlParse;

// `IDENTITY [(seed, increment)] [NOT FOR REPLICATION]` column property.
public sealed class TsqlIdentitySpec
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken IdentityKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public Expression? Seed { get; init; }
    public SyntaxToken? Comma { get; init; }
    public Expression? Increment { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public TsqlNotForReplication? NotForReplication { get; init; }
}
