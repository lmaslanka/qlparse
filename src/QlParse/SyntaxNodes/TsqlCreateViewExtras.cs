namespace QlParse;

// SQL Server-only parts of CREATE VIEW (`WITH SCHEMABINDING, ENCRYPTION, VIEW_METADATA`); allocated only when
// present.
public sealed class TsqlCreateViewExtras
{
    public required SyntaxToken AttributesWithKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Attributes { get; init; }
}
