namespace QlParse;

// A SQL Server script split into batches by client-side GO lines; produced only when the input holds at
// least one GO. Each batch owns the separator that ends it.
public sealed class TsqlScript : Query
{
    public required IReadOnlyList<TsqlBatch> Batches { get; init; }
}
