namespace QlParse;

// One `{ADMIN | INHERIT | SET} {OPTION | TRUE | FALSE}` option after the first in a Postgres role grant.
public sealed class PgRoleGrantOption
{
    public required SourceSpan Span { get; init; }
    public required SyntaxToken Kind { get; init; }
    public required SyntaxToken Value { get; init; }
}
