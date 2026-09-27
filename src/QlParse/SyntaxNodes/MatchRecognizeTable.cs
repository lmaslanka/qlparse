namespace QlParse;

public sealed class MatchRecognizeTable : TableSource
{
    public required TableSource Input { get; init; }
    public required SyntaxToken MatchRecognizeKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public SyntaxToken? PartitionKeyword { get; init; }
    public SyntaxToken? PartitionBy { get; init; }
    public IReadOnlyList<Expression> PartitionByList { get; init; } = [];
    public OrderByClause? OrderBy { get; init; }
    public SyntaxToken? MeasuresKeyword { get; init; }
    public IReadOnlyList<PatternMeasure> Measures { get; init; } = [];
    public SyntaxToken? RowsKind { get; init; }
    public SyntaxToken? RowOrRows { get; init; }
    public SyntaxToken? PerKeyword { get; init; }
    public SyntaxToken? MatchKeyword { get; init; }
    public SyntaxToken? EmptyHandling { get; init; }
    public SyntaxToken? AfterKeyword { get; init; }
    public SyntaxToken? SkipKeyword { get; init; }
    public SyntaxToken? SkipTo { get; init; }
    public SyntaxToken? SkipPosition { get; init; }
    public SyntaxToken? SkipTarget { get; init; }
    public required SyntaxToken PatternKeyword { get; init; }
    public required SyntaxToken PatternOpen { get; init; }
    public required RowPattern Pattern { get; init; }
    public required SyntaxToken PatternClose { get; init; }
    public SyntaxToken? SubsetKeyword { get; init; }
    public IReadOnlyList<PatternSubset> Subsets { get; init; } = [];
    public required SyntaxToken DefineKeyword { get; init; }
    public required IReadOnlyList<PatternDefinition> Definitions { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
    public SyntaxToken? ColumnOpenParen { get; init; }
    public IReadOnlyList<SyntaxToken>? Columns { get; init; }
    public SyntaxToken? ColumnCloseParen { get; init; }
}
