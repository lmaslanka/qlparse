namespace QlParse;

public sealed class ExclusionElement
{
    public required SourceSpan Span { get; init; }
    public required IndexColumn Column { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required SyntaxToken Operator { get; init; }

    // The whole `OPERATOR(schema.op)` form when used; Operator is then the bare operator inside it.
    public PgOperatorName? QualifiedOperator { get; init; }
}
