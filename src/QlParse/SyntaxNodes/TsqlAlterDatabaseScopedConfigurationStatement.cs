namespace QlParse;

// ALTER DATABASE SCOPED CONFIGURATION [FOR SECONDARY] {SET name = value | CLEAR PROCEDURE_CACHE [plan_handle]}.
public sealed class TsqlAlterDatabaseScopedConfigurationStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken DatabaseKeyword { get; init; }
    public required SyntaxToken ScopedKeyword { get; init; }
    public required SyntaxToken ConfigurationKeyword { get; init; }
    public SyntaxToken? ForKeyword { get; init; }
    public SyntaxToken? SecondaryKeyword { get; init; }
    public required SyntaxToken ActionKeyword { get; init; }
    public required TsqlObjectOption Option { get; init; }
}
