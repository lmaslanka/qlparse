namespace QlParse;

// BEGIN TRY statements END TRY BEGIN CATCH statements END CATCH.
public sealed class TsqlTryCatchStatement : Query
{
    public required SyntaxToken TryBeginKeyword { get; init; }
    public required SyntaxToken TryKeyword { get; init; }
    public required IReadOnlyList<Query> TryStatements { get; init; }
    public required SyntaxToken TryEndKeyword { get; init; }
    public required SyntaxToken TryEndTryKeyword { get; init; }
    public required SyntaxToken CatchBeginKeyword { get; init; }
    public required SyntaxToken CatchKeyword { get; init; }
    public required IReadOnlyList<Query> CatchStatements { get; init; }
    public required SyntaxToken CatchEndKeyword { get; init; }
    public required SyntaxToken CatchEndCatchKeyword { get; init; }
}
