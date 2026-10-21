namespace QlParse;

// Postgres-only parts of CREATE VIEW (OR REPLACE, TEMPORARY, WITH (options));
// allocated only when at least one is present.
public sealed class PgCreateViewExtras
{
    public SyntaxToken? OrKeyword { get; init; }
    public SyntaxToken? ReplaceKeyword { get; init; }
    public SyntaxToken? TemporaryKeyword { get; init; }
    public SyntaxToken? OptionsWithKeyword { get; init; }
    public StorageParameterList? Options { get; init; }
}
