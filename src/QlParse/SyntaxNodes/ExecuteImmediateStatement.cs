namespace QlParse;

public sealed class ExecuteImmediateStatement : Query
{
    public required SyntaxToken ExecuteKeyword { get; init; }
    public required SyntaxToken ImmediateKeyword { get; init; }
    public required SyntaxToken Source { get; init; }
}
