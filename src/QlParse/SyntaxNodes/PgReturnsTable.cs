namespace QlParse;

public sealed class PgReturnsTable
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken TableKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public IReadOnlyList<RoutineParameter> Columns { get; init; } = [];
    public required SyntaxToken CloseParen { get; init; }
}
