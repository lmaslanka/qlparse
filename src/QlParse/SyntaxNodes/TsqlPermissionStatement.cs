namespace QlParse;

// GRANT | DENY | REVOKE permission, ... [ON [class ::] securable [(column, ...)]] {TO | FROM} principal, ...
// [WITH GRANT OPTION] [CASCADE] [AS principal]; GrantOptionFor holds REVOKE's `GRANT OPTION FOR`.
public sealed class TsqlPermissionStatement : Query
{
    public required SyntaxToken VerbKeyword { get; init; }
    public IReadOnlyList<SyntaxToken> GrantOptionFor { get; init; } = [];
    public required IReadOnlyList<TsqlPermission> Permissions { get; init; }
    public SyntaxToken? OnKeyword { get; init; }
    public TsqlSecurable? Securable { get; init; }
    public SyntaxToken? OpenParen { get; init; }
    public IReadOnlyList<SyntaxToken> Columns { get; init; } = [];
    public SyntaxToken? CloseParen { get; init; }
    public required SyntaxToken ToKeyword { get; init; }
    public required IReadOnlyList<SyntaxToken> Principals { get; init; }
    public GrantOptionClause? GrantOption { get; init; }
    public SyntaxToken? CascadeKeyword { get; init; }
    public SyntaxToken? AsKeyword { get; init; }
    public SyntaxToken? AsPrincipal { get; init; }
}
