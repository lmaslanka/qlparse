namespace QlParse;

// Postgres-only parts of COMMENT ON (multi-word kinds, routine argument types, `ON table`);
// allocated only when at least one is present.
public sealed class PgCommentExtras
{
    public IReadOnlyList<SyntaxToken> ExtraKindKeywords { get; init; } = [];
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<DataType> ArgumentTypes { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public SyntaxToken? TargetOnKeyword { get; init; }
    public IReadOnlyList<SyntaxToken>? TableName { get; init; }
}
