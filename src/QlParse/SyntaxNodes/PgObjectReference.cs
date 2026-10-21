namespace QlParse;

public sealed class PgObjectReference
{
    public required SourceSpan Span { get; init; }
    public IReadOnlyList<SyntaxToken> Name { get; init; } = [];
    public PgOperatorName? Operator { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public SyntaxToken? Star { get; init; }
    public IReadOnlyList<PgRoutineArgument> Arguments { get; init; } = [];
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? OrderKeyword { get; init; }
    public SyntaxToken? ByKeyword { get; init; }
    public IReadOnlyList<PgRoutineArgument> OrderedArguments { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? UsingKeyword { get; init; }
    public SyntaxToken? AccessMethod { get; init; }

    // `FOR type LANGUAGE lang` (a transform): the type is the single entry of Arguments.
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? LanguageKeyword { get; init; }
    public SyntaxToken? Language { get; init; }
}
