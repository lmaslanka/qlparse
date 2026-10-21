namespace QlParse;

// SQL Server-only parts of ALTER TABLE (the `WITH {CHECK|NOCHECK}` prefix of ADD CONSTRAINT and
// CHECK CONSTRAINT); allocated only when present.
public sealed class TsqlAlterTableExtras
{
    public SyntaxToken? WithKeyword { get; init; }
    public SyntaxToken? CheckMode { get; init; }
}
