namespace QlParse;

// `ALTER FULLTEXT INDEX ON table action [WITH NO POPULATION]`. Action holds the action's words: ENABLE, DISABLE,
// SET (with Option: CHANGE_TRACKING, STOPLIST or SEARCH PROPERTY LIST), ADD or DROP (with Columns),
// ALTER COLUMN column {ADD | DROP} STATISTICAL_SEMANTICS, START {FULL | INCREMENTAL | UPDATE} POPULATION, or
// {STOP | PAUSE | RESUME} POPULATION.
public sealed class TsqlAlterFulltextIndexStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken FulltextKeyword { get; init; }
    public required SyntaxToken IndexKeyword { get; init; }
    public required SyntaxToken OnKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Table { get; init; }
    public required IReadOnlyList<SyntaxToken> Action { get; init; }
    public TsqlOption? Option { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<TsqlFulltextIndexColumn> Columns { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> NoPopulation { get; init; } = [];
}
