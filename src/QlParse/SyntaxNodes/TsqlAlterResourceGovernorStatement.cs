namespace QlParse;

// `ALTER RESOURCE GOVERNOR {DISABLE | RECONFIGURE | RESET STATISTICS | WITH (option, ...)}`.
public sealed class TsqlAlterResourceGovernorStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken ResourceKeyword { get; init; }
    public required SyntaxToken GovernorKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Action { get; init; } = [];
    public SyntaxToken? WithKeyword { get; init; }
    public TsqlOptionList? Options { get; init; }
}
