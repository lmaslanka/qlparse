namespace QlParse;

public sealed class PgOperatorName
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? OperatorKeyword { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken> SchemaParts { get; init; } = [];
    public required SyntaxToken Operator { get; init; }
    public SyntaxToken? CloseParen { get; init; }
}
