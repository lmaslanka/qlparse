namespace QlParse;

// PL/pgSQL `GET {CURRENT | STACKED} DIAGNOSTICS`; allocated only when present.
public sealed class PgGetDiagnosticsExtras
{
    public SyntaxToken? ScopeKeyword { get; init; }
}
