namespace QlParse;

// `{CHECK|NOCHECK} CONSTRAINT {ALL|name, ...}`; a `WITH {CHECK|NOCHECK}` prefix is in the statement's extras.
public sealed class TsqlCheckConstraintAction : AlterTableAction
{
    public required SyntaxToken CheckKeyword { get; init; }
    public required SyntaxToken ConstraintKeyword { get; init; }
    public SyntaxToken? AllKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> Names { get; init; } = [];
}
