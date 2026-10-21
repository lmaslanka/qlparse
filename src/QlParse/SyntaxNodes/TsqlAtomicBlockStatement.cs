namespace QlParse;

// BEGIN ATOMIC WITH (TRANSACTION ISOLATION LEVEL = ..., LANGUAGE = ...) statements END, the body of a
// natively compiled module.
public sealed class TsqlAtomicBlockStatement : Query
{
    public required SyntaxToken BeginKeyword { get; init; }
    public required SyntaxToken AtomicKeyword { get; init; }
    public required SyntaxToken WithKeyword { get; init; }
    public required SyntaxToken OpenParen { get; init; }
    public required IReadOnlyList<TsqlOption> Options { get; init; }
    public required SyntaxToken CloseParen { get; init; }
    public required IReadOnlyList<Query> Statements { get; init; }
    public required SyntaxToken EndKeyword { get; init; }
}
