using System.Runtime.CompilerServices;

namespace QlParse;

internal sealed partial class Parser
{
    // Workstream C: query & DML (COPY, ...).
    internal Query? TryParsePgDmlStatement() =>
        _current.Kind == SyntaxKind.Identifier && _current.Length == Len4 && IdentifierEquals(Keyword.Copy)
            ? ParseCopy()
            : null;

    // Postgres reserves RETURNING, so it never starts a bare alias in Postgres mode.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsPgReservedClauseWord() => IsPostgres && IdentifierEquals(Keyword.Returning);

    // A query, or (Postgres) a data-modifying statement in a WITH body / CTE / COPY source.
    internal Query ParsePgQueryOrDml(int minBindingPower)
    {
        if (_current.Kind == SyntaxKind.UpdateKeyword)
        {
            return ParseUpdate();
        }

        if (_current.Kind != SyntaxKind.Identifier)
        {
            return ParseQuery(minBindingPower);
        }

        if (IdentifierEquals(Keyword.Insert))
        {
            return ParseInsert();
        }

        if (IdentifierEquals(Keyword.Delete))
        {
            return ParseDelete();
        }

        return IdentifierEquals(Keyword.Merge) ? ParseMerge() : ParseQuery(minBindingPower);
    }
}
