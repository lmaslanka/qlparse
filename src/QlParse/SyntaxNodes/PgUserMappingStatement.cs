namespace QlParse;

public sealed class PgUserMappingStatement : Query
{
    public required SyntaxToken Verb { get; init; }
    public required SyntaxToken UserKeyword { get; init; }
    public required SyntaxToken MappingKeyword { get; init; }
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public required SyntaxToken ForKeyword { get; init; }
    public required SyntaxToken User { get; init; }
    public required SyntaxToken ServerKeyword { get; init; }
    public required SyntaxToken Server { get; init; }
    public PgOptionList? Options { get; init; }
}
