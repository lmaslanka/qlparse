namespace QlParse;

public sealed class MaintenanceTable
{
    public required SourceSpan Span { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken> Columns { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
}
