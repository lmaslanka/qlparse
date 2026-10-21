namespace QlParse;

// CREATE | ALTER {SERVER | DATABASE} AUDIT SPECIFICATION name [FOR SERVER AUDIT audit] {ADD | DROP} (...), ...
// [WITH (STATE = ...)]; Keywords holds the verb and object kind.
public sealed class TsqlAuditSpecificationStatement : Query
{
    public required IReadOnlyList<SyntaxToken> Keywords { get; init; }
    public required SyntaxToken Name { get; init; }
    public IReadOnlyList<SyntaxToken> ForServerAudit { get; init; } = [];
    public SyntaxToken? Audit { get; init; }
    public IReadOnlyList<TsqlAuditAction> Actions { get; init; } = [];
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlObjectOptionList? Options { get; init; }
}
