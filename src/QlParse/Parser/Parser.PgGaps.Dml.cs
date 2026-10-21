using System.Runtime.CompilerServices;

namespace QlParse;

// Postgres query/DML gaps: multi-table TRUNCATE, LATERAL function items, ORDER BY / LIMIT after a
// query that does not end in a SELECT, `(TABLE t)` subqueries, qualified lock targets, MERGE DO NOTHING.
// Every entry point is reached only in Postgres mode, where ISO parsing would otherwise stop or fail.
internal sealed partial class Parser
{
    internal readonly record struct PgTruncateTail(TruncateIdentity Identity, PgTruncateExtras? Extras, SourceSpan? End);

    // `[, [ONLY] name [*] ...] [{RESTART | CONTINUE} IDENTITY] [CASCADE | RESTRICT]` after TRUNCATE's first table.
    internal PgTruncateTail ParsePgTruncateTail(SyntaxToken? onlyKeyword, SyntaxToken? star)
    {
        var additionalTables = ParsePgTruncateTables();
        var identity = ParseTruncateIdentity();
        var behavior = TryParseDropBehavior();
        var tablesEnd = additionalTables.Count > 0 ? additionalTables[^1].Span : (SourceSpan?)null;
        var end = (behavior ?? identity.IdentityKeyword)?.Span ?? tablesEnd;
        if (onlyKeyword is null && star is null && tablesEnd is null && behavior is null)
        {
            return new PgTruncateTail(identity, null, end);
        }

        var extras = new PgTruncateExtras
        {
            OnlyKeyword = onlyKeyword,
            DescendantStar = star,
            AdditionalTables = additionalTables,
            Behavior = behavior,
        };
        return new PgTruncateTail(identity, extras, end);
    }

    // `, [ONLY] name [*] ...` after TRUNCATE's first table.
    private IReadOnlyList<LockTarget> ParsePgTruncateTables()
    {
        if (_current.Kind != SyntaxKind.Comma)
        {
            return [];
        }

        var tables = new List<LockTarget>();
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            tables.Add(ParseLockTarget());
        }

        return tables;
    }

    // LATERAL (query) alias, or (Postgres) LATERAL before any other FROM item.
    internal TableSource ParseLateral()
    {
        var lateral = Advance();
        return IsPostgres && _current.Kind != SyntaxKind.OpenParen ? ParsePgLateralTable(lateral) : ParseDerivedTable(lateral);
    }

    // LATERAL before a function call, ROWS FROM (...), UNNEST (...) or another non-subquery FROM item.
    private LateralTable ParsePgLateralTable(SyntaxToken lateralKeyword)
    {
        var table = ParseTablePrimary();
        return new LateralTable
        {
            Span = SourceSpan.From(lateralKeyword, table.Span),
            LateralKeyword = lateralKeyword,
            Table = table,
        };
    }

    // ORDER BY / LIMIT / OFFSET / FETCH / FOR after a whole query expression. Only a top-level query
    // (not the right operand of a set operator) takes them, so they apply to the whole set operation.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsPgQueryTailStart(int minBindingPower) =>
        IsPostgres
        && minBindingPower <= UnionBindingPower
        && _current.Kind is SyntaxKind.OrderKeyword or SyntaxKind.LimitKeyword or SyntaxKind.OffsetKeyword
            or SyntaxKind.FetchKeyword or SyntaxKind.ForKeyword;

    internal OrderedQuery ParsePgOrderedQuery(Query query)
    {
        var rest = ParseSelectRestClauses();
        return new OrderedQuery
        {
            Span = SourceSpan.From(query.Span, rest.End ?? query.Span),
            Query = query,
            OrderBy = rest.OrderBy,
            Limit = rest.Limit,
            Offset = rest.Offset,
            Fetch = rest.Fetch,
            Lock = rest.Lock,
        };
    }

    // `(TABLE t)` as an expression subquery: TABLE followed by a name, not the TABLE(...) function.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsPgTableQueryStart() =>
        IsPostgres && IdentifierEquals(Keyword.Table) && NextKind is SyntaxKind.Identifier or SyntaxKind.OnlyKeyword;

    internal readonly record struct PgLockTargets(
        SyntaxToken? OfKeyword,
        IReadOnlyList<SyntaxToken>? Columns,
        IReadOnlyList<IReadOnlyList<SyntaxToken>> QualifiedNames);

    // OF name [, ...] where a name may be qualified. Columns holds the last part of each name, and the
    // full names are kept only when at least one of them is qualified.
    internal PgLockTargets ParsePgLockTargets()
    {
        if (_current.Kind != SyntaxKind.OfKeyword)
        {
            return new PgLockTargets(null, null, []);
        }

        var ofKeyword = Advance();
        var names = new List<IReadOnlyList<SyntaxToken>> { ParseQualifiedName() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            names.Add(ParseQualifiedName());
        }

        var columns = new List<SyntaxToken>(names.Count);
        var qualified = false;
        foreach (var name in names)
        {
            columns.Add(name[^1]);
            qualified |= name.Count > 1;
        }

        return new PgLockTargets(ofKeyword, columns, qualified ? names : []);
    }

    internal MergeAction? TryParsePgMergeDoNothing()
    {
        if (!IsPostgres || !IdentifierEquals(Keyword.Do))
        {
            return null;
        }

        var doKeyword = Advance();
        var nothingKeyword = ExpectIdent(Keyword.Nothing);
        return new MergeDoNothingAction
        {
            Span = SourceSpan.From(doKeyword, nothingKeyword),
            DoKeyword = doKeyword,
            NothingKeyword = nothingKeyword,
        };
    }
}
