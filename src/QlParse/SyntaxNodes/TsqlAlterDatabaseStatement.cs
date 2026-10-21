namespace QlParse;

// ALTER DATABASE {name | CURRENT} action, where ActionKeywords is the action verb: SET option, ... [WITH termination];
// ADD [LOG] FILE file, ... [TO FILEGROUP fg]; REMOVE FILE f; MODIFY FILE file; ADD | REMOVE | MODIFY FILEGROUP fg [property];
// MODIFY NAME = new; COLLATE c. Target is the file, filegroup, new name or collation; Options holds the SET options or the
// filegroup properties (`CONTAINS FILESTREAM`, `READ_ONLY`, `NAME = new`); Termination is `ROLLBACK IMMEDIATE`, `NO_WAIT`, ...
public sealed class TsqlAlterDatabaseStatement : Query
{
    public required SyntaxToken AlterKeyword { get; init; }
    public required SyntaxToken DatabaseKeyword { get; init; }
    public required SyntaxToken Name { get; init; }
    public required IReadOnlyList<SyntaxToken> ActionKeywords { get; init; }
    public SyntaxToken? EqualsToken { get; init; }
    public SyntaxToken? Target { get; init; }
    public IReadOnlyList<TsqlObjectOptionList> Files { get; init; } = [];
    public SyntaxToken? ToKeyword { get; init; }
    public SyntaxToken? FilegroupKeyword { get; init; }
    public SyntaxToken? Filegroup { get; init; }
    public IReadOnlyList<TsqlObjectOption> Options { get; init; } = [];
    public SyntaxToken? WithKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Termination { get; init; } = [];
}
