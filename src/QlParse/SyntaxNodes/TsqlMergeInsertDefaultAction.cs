namespace QlParse;

// `WHEN NOT MATCHED THEN INSERT DEFAULT VALUES`.
public sealed class TsqlMergeInsertDefaultAction : MergeAction
{
    public required SyntaxToken InsertKeyword { get; init; }
    public required SyntaxToken DefaultKeyword { get; init; }
    public required SyntaxToken ValuesKeyword { get; init; }
}
