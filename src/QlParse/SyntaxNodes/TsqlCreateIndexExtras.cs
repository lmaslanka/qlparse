namespace QlParse;

// SQL Server-only parts of CREATE INDEX (INCLUDE, WITH (index options), ON storage, FILESTREAM_ON);
// allocated only when at least one is present. CLUSTERED / NONCLUSTERED is the ISO ClusteredKeyword.
public sealed class TsqlCreateIndexExtras
{
    public TsqlIncludeClause? Include { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlRelationalOptionList? Options { get; init; }
    public TsqlStorageClause? Storage { get; init; }
    public TsqlStorageClause? FilestreamOn { get; init; }
}
