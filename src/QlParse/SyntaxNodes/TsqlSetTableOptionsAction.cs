namespace QlParse;

// `SET (LOCK_ESCALATION = ..., SYSTEM_VERSIONING = ..., ...)`.
public sealed class TsqlSetTableOptionsAction : AlterTableAction
{
    public required SyntaxToken SetKeyword { get; init; }
    public required TsqlRelationalOptionList Options { get; init; }
}
