namespace QlParse;

// Postgres-only parts of CREATE CAST (`WITH FUNCTION f(args)`, `WITH INOUT`);
// allocated only when at least one is present.
public sealed class PgCreateCastExtras
{
    public SyntaxToken? FunctionKeyword { get; init; }
    public SyntaxToken? InoutKeyword { get; init; }
    public SyntaxToken? ArgumentsOpenParen { get; init; }
    public IReadOnlyList<PgRoutineArgument> Arguments { get; init; } = [];
    public SyntaxToken? ArgumentsCloseParen { get; init; }
}
