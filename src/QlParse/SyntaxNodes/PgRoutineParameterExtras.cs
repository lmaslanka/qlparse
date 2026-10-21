namespace QlParse;

// Postgres-only parts of a routine parameter (`%TYPE`, DEFAULT / `=` value);
// allocated only when at least one is present.
public sealed class PgRoutineParameterExtras
{
    public PgTypeAttribute? TypeAttribute { get; init; }
    public SyntaxToken? DefaultToken { get; init; }
    public Expression? Default { get; init; }
}
