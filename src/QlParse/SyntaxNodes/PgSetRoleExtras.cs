namespace QlParse;

// Postgres `SET {SESSION | LOCAL} ROLE`; allocated only when present.
public sealed class PgSetRoleExtras
{
    public SyntaxToken? ScopeKeyword { get; init; }
}
