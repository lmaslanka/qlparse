namespace QlParse;

// One row-level security predicate action: {ADD | ALTER | DROP} [FILTER | BLOCK] PREDICATE [fn(arg, ...)] ON table
// [{AFTER | BEFORE} {INSERT | UPDATE | DELETE}]. The function is absent for DROP.
public sealed class TsqlSecurityPredicate
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken ActionKeyword { get; init; }
    public SyntaxToken? KindKeyword { get; init; }
    public required SyntaxToken PredicateKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> FunctionName { get; init; } = [];
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<Expression> Arguments { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Table { get; init; }
    public IReadOnlyList<SyntaxToken> BlockOperation { get; init; } = [];
}
