namespace QlParse;

public sealed class PgCreateConversionStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public SyntaxToken? DefaultKeyword { get; init; }
    public required SyntaxToken ConversionKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken SourceEncoding { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required SyntaxToken TargetEncoding { get; init; }
    public required SyntaxToken FromKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Function { get; init; }
}
