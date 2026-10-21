namespace QlParse;

// A SQL Server securable: `[class ::] name`, e.g. `OBJECT::dbo.t`, `XML SCHEMA COLLECTION::s.c`, or a bare `dbo.t`.
public sealed class TsqlSecurable
{
    public required SourceSpan Span { get; init; }
    public IReadOnlyList<SyntaxToken> ClassKeywords { get; init; } = [];
    public SyntaxToken? ColonColon { get; init; }
    public required IReadOnlyList<SyntaxToken> Name { get; init; }
}
