namespace QlParse;

public sealed class PgRoutineArgument
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? Mode { get; init; }
    public required DataType Type { get; init; }
}
