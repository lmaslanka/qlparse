namespace QlParse;

// `CREATE SERVICE name [AUTHORIZATION owner] ON QUEUE queue [(contract, ...)]` and
// `ALTER SERVICE name [ON QUEUE queue] [({ADD | DROP} CONTRACT contract, ...)]`.
public sealed class TsqlServiceStatement : Query
{
    public required SyntaxToken CreateOrAlterKeyword { get; init; }
    public required SyntaxToken ServiceKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? AuthorizationKeyword { get; init; }
    public SyntaxToken? Owner { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public SyntaxToken? QueueKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Queue { get; init; } = [];
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<TsqlServiceContract> Contracts { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}
