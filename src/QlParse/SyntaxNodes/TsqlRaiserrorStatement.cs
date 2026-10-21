namespace QlParse;

// RAISERROR (msg, severity, state [, argument ...]) [WITH LOG, NOWAIT, SETERROR]; the legacy
// RAISERROR number 'message' form has no parentheses.
public sealed class TsqlRaiserrorStatement : Query
{
    public required SyntaxToken RaiserrorKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public required IReadOnlyList<Expression> Arguments { get; init; }
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Options { get; init; } = [];
}
