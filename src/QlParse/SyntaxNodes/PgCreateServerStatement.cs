namespace QlParse;

public sealed class PgCreateServerStatement : Query
{
    public required SyntaxToken CreateKeyword { get; init; }
    public required SyntaxToken ServerKeyword { get; init; }
    public SyntaxToken? IfKeyword { get; init; }
    public SyntaxToken? NotKeyword { get; init; }
    public SyntaxToken? ExistsKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public SyntaxToken? TypeKeyword { get; init; }
    public SyntaxToken? Type { get; init; }
    public SyntaxToken? VersionKeyword { get; init; }
    public SyntaxToken? Version { get; init; }
    public required SyntaxToken ForeignKeyword { get; init; }
    public required SyntaxToken DataKeyword { get; init; }
    public required SyntaxToken WrapperKeyword { get; init; }
    public required SyntaxToken Wrapper { get; init; }
    public PgOptionList? Options { get; init; }
}
