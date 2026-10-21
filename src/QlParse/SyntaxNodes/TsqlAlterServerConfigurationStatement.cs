namespace QlParse;

// `ALTER SERVER CONFIGURATION SET setting ...`: Setting holds the setting's words (PROCESS AFFINITY,
// DIAGNOSTICS LOG, BUFFER POOL EXTENSION, HADR CLUSTER CONTEXT, SOFTNUMA, ...) and Options the rest.
public sealed class TsqlAlterServerConfigurationStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken ServerKeyword { get; init; }
    public required SyntaxToken ConfigurationKeyword { get; init; }
    public required SyntaxToken SetKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Setting { get; init; }
    public required TsqlOptionList Options { get; init; }
}
