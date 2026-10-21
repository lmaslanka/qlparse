namespace QlParse;

public sealed class PlpgsqlRaiseOption
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Name { get; init; }
    public required SyntaxToken AssignmentToken { get; init; }
    public required Expression Value { get; init; }
}
