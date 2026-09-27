namespace QlParse;

public sealed class EmbeddedSqlStatement : Query
{
    public required SyntaxToken ExecKeyword { get; init; }
    public required SyntaxToken SqlKeyword { get; init; }
    public Query? Statement { get; init; }
    public SyntaxToken? EndKeyword { get; init; }
    public SyntaxToken? Minus { get; init; }
    public SyntaxToken? EndExec { get; init; }
    public SyntaxToken? Semicolon { get; init; }
}
