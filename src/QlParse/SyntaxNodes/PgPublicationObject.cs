namespace QlParse;

public sealed class PgPublicationObject
{
    public required SourceSpan Span { get; init; }
    public SyntaxToken? TableKeyword { get; init; }
    public SyntaxToken? InKeyword { get; init; }
    public SyntaxToken? SchemaKeyword { get; init; }
    public SyntaxToken? OnlyKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public SyntaxToken? Star { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken> Columns { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? WhereKeyword { get; init; }
    public SyntaxToken? WhereOpenParen { get; init; }
    public Expression? Where { get; init; }
    public SyntaxToken? WhereCloseParen { get; init; }
}
