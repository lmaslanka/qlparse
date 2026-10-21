namespace QlParse;

// A FROM item that takes part in an arbitrary-length MATCH pattern: `table FOR PATH [AS] alias`.
public sealed class TsqlGraphPathTable : TableSource
{
    public required IReadOnlyList<SyntaxToken> NameParts { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken PathKeyword { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? Alias { get; init; }
}
