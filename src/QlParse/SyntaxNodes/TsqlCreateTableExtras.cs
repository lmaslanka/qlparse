namespace QlParse;

// SQL Server-only parts of CREATE TABLE (AS NODE / AS EDGE / AS FILETABLE, ON / TEXTIMAGE_ON / FILESTREAM_ON
// storage, WITH (table options)); allocated only when at least one is present.
public sealed class TsqlCreateTableExtras
{
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? TableKind { get; init; }
    public TsqlStorageClause? Storage { get; init; }
    public TsqlStorageClause? TextImageOn { get; init; }
    public TsqlStorageClause? FilestreamOn { get; init; }
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlRelationalOptionList? Options { get; init; }
}
