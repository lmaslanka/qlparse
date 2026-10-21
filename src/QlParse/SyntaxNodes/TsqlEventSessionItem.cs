namespace QlParse;

// `{ADD | DROP} {EVENT | TARGET} [module.]package.name [( [SET field = value, ...]
// [ACTION (package.action, ...)] [WHERE predicate] )]` in an extended events session.
public sealed class TsqlEventSessionItem
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Action { get; init; }
    public required SyntaxToken Kind { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public SyntaxToken? SetKeyword { get; init; }
    public TsqlOptionList? Settings { get; init; }
    public SyntaxToken? ActionKeyword { get; init; }
    public SyntaxToken? ActionOpenParen { get; init; }
    public IReadOnlyList<Expression> Actions { get; init; } = [];
    public SyntaxToken? ActionCloseParen { get; init; }
    public SyntaxToken? WhereKeyword { get; init; }
    public Expression? Predicate { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}
