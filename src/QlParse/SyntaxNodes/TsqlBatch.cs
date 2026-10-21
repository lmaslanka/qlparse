namespace QlParse;

// One SQL Server batch: statements whose `;` terminators are optional. Semicolons holds every terminator in
// source order (a statement may have none or several). Separator is the GO line ending the batch; it is
// absent on the last batch of a script and on a GO-free input of two or more statements.
public sealed class TsqlBatch : Query
{
    public required IReadOnlyList<Query> Statements { get; init; }
    public required IReadOnlyList<SyntaxToken> Semicolons { get; init; }
    public TsqlBatchSeparator? Separator { get; init; }
}
