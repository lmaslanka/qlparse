namespace QlParse;

public sealed class VacuumStatement : Query
{
    public required SyntaxToken VacuumKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<KeywordOption> Options { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public IReadOnlyList<MaintenanceTable> Tables { get; init; } = [];
}
