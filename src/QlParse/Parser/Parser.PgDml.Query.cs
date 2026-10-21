namespace QlParse;

internal sealed partial class Parser
{
    // Reached from ParseSetPrimary only in Postgres mode, when the token is not SELECT, VALUES, or (.
    internal Query ParsePgSetPrimary() =>
        IdentifierEquals(Keyword.Table) ? ParseExplicitTable() : ParseSelectStatement();

    // TABLE [ONLY] name [*]  ==  SELECT * FROM [ONLY] name [*]
    private ExplicitTableQuery ParseExplicitTable()
    {
        var tableKeyword = Advance();
        var onlyKeyword = _current.Kind == SyntaxKind.OnlyKeyword ? Advance() : (SyntaxToken?)null;
        var nameParts = ParseQualifiedName();
        var star = _current.Kind == SyntaxKind.Star ? Advance() : (SyntaxToken?)null;
        return new ExplicitTableQuery
        {
            Span = SourceSpan.From(tableKeyword, star ?? nameParts[^1]),
            TableKeyword = tableKeyword,
            OnlyKeyword = onlyKeyword,
            NameParts = nameParts,
            DescendantStar = star,
        };
    }

    // `(TABLE t)` as a derived table: the paren is followed by TABLE and a name, not TABLE(...).
    internal bool IsPgParenTableQuery() =>
        IsPostgres
        && NextEquals(Keyword.Table)
        && _index + 1 < _tokens.Count
        && _tokens[_index + 1].Kind is SyntaxKind.Identifier or SyntaxKind.OnlyKeyword;

    internal PgCteExtras ParseCteMaterialization()
    {
        var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
        var materializedKeyword = ExpectIdent(Keyword.Materialized);
        return new PgCteExtras { NotKeyword = notKeyword, MaterializedKeyword = materializedKeyword };
    }

    // PG14: CYCLE columns SET mark USING path, with the TO/DEFAULT mark values omitted.
    internal CycleClause ParsePgCycleWithoutValues(
        SyntaxToken cycleKeyword,
        IReadOnlyList<SyntaxToken> columns,
        SyntaxToken setKeyword,
        SyntaxToken markColumn)
    {
        var usingKeyword = Advance();
        var pathColumn = Expect(SyntaxKind.Identifier);
        return new CycleClause
        {
            Span = SourceSpan.From(cycleKeyword, pathColumn),
            CycleKeyword = cycleKeyword,
            Columns = columns,
            SetKeyword = setKeyword,
            MarkColumn = markColumn,
            UsingKeyword = usingKeyword,
            PathColumn = pathColumn,
        };
    }

    internal DistinctOnClause ParseDistinctOn()
    {
        var onKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var expressions = ParseExpressionList();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new DistinctOnClause
        {
            Span = SourceSpan.From(onKeyword, closeParen),
            OnKeyword = onKeyword,
            OpenParen = openParen,
            Expressions = expressions,
            CloseParen = closeParen,
        };
    }

    // SELECT ... INTO [TEMPORARY | TEMP | UNLOGGED] [TABLE] new_table
    internal SelectIntoTable ParseSelectIntoTable()
    {
        var modifier = IdentifierEquals(Keyword.Temporary) || IdentifierEquals(Keyword.Temp) || IdentifierEquals(Keyword.Unlogged)
            ? Advance()
            : (SyntaxToken?)null;
        var tableKeyword = IdentifierEquals(Keyword.Table) ? Advance() : (SyntaxToken?)null;
        var nameParts = ParseQualifiedName();
        var start = modifier ?? tableKeyword ?? nameParts[0];
        return new SelectIntoTable
        {
            Span = SourceSpan.From(start, nameParts[^1]),
            Modifier = modifier,
            TableKeyword = tableKeyword,
            NameParts = nameParts,
        };
    }

    // LIMIT/OFFSET/FETCH may follow the locking clauses when none preceded them.
    internal LockClauses ParsePgLockTail(LockClause first, LimitOffsetFetch leading)
    {
        var last = first;
        while (last.Postgres?.NextLock is { } next)
        {
            last = next;
        }

        if (leading.End is not null)
        {
            return new LockClauses(first, leading, last.Span);
        }

        var trailing = ParseLimitOffsetFetch();
        return new LockClauses(first, trailing, trailing.End ?? last.Span);
    }

    // FOR {UPDATE | NO KEY UPDATE | SHARE | KEY SHARE} [OF name [, ...]] [NOWAIT | SKIP LOCKED] [FOR ...]
    internal LockClause ParsePgLockClause(SyntaxToken forKeyword)
    {
        if (_current.Kind == SyntaxKind.ReadKeyword)
        {
            var readKeyword = Advance();
            var onlyKeyword = Expect(SyntaxKind.OnlyKeyword);
            return new LockClause
            {
                Span = SourceSpan.From(forKeyword, onlyKeyword),
                ForKeyword = forKeyword,
                ReadKeyword = readKeyword,
                OnlyKeyword = onlyKeyword,
            };
        }

        var strength = ParsePgLockStrength();
        var targets = ParsePgLockTargets();
        var columns = targets.Columns;
        var wait = ParsePgLockWait();
        var end = wait.End ?? (columns is null ? strength.Last : columns[^1]);
        var next = _current.Kind == SyntaxKind.ForKeyword ? ParseLockClause() : null;
        return new LockClause
        {
            Span = SourceSpan.From(forKeyword, end),
            ForKeyword = forKeyword,
            UpdateKeyword = strength.UpdateKeyword,
            ShareKeyword = strength.ShareKeyword,
            OfKeyword = targets.OfKeyword,
            Columns = columns,
            Postgres = BuildPgLockExtras(strength, wait, next, targets),
        };
    }

    private static PgLockExtras? BuildPgLockExtras(
        PgLockStrength strength,
        PgLockWait wait,
        LockClause? next,
        PgLockTargets targets)
    {
        if (strength.KeyKeyword is null && wait.End is null && next is null && targets.QualifiedNames.Count == 0)
        {
            return null;
        }

        return new PgLockExtras
        {
            NoKeyword = strength.NoKeyword,
            KeyKeyword = strength.KeyKeyword,
            NowaitKeyword = wait.NowaitKeyword,
            SkipKeyword = wait.SkipKeyword,
            LockedKeyword = wait.LockedKeyword,
            NextLock = next,
            OfNames = targets.QualifiedNames,
        };
    }

    private readonly record struct PgLockStrength(
        SyntaxToken? NoKeyword,
        SyntaxToken? KeyKeyword,
        SyntaxToken? UpdateKeyword,
        SyntaxToken? ShareKeyword,
        SyntaxToken Last);

    private PgLockStrength ParsePgLockStrength()
    {
        if (_current.Kind == SyntaxKind.UpdateKeyword)
        {
            var updateKeyword = Advance();
            return new PgLockStrength(null, null, updateKeyword, null, updateKeyword);
        }

        if (IdentifierEquals(Keyword.Share))
        {
            var shareKeyword = Advance();
            return new PgLockStrength(null, null, null, shareKeyword, shareKeyword);
        }

        if (IdentifierEquals(Keyword.No))
        {
            var noKeyword = Advance();
            var keyKeyword = ExpectIdent(Keyword.Key);
            var updateKeyword = Expect(SyntaxKind.UpdateKeyword);
            return new PgLockStrength(noKeyword, keyKeyword, updateKeyword, null, updateKeyword);
        }

        if (IdentifierEquals(Keyword.Key))
        {
            var keyKeyword = Advance();
            var shareKeyword = ExpectIdent(Keyword.Share);
            return new PgLockStrength(null, keyKeyword, null, shareKeyword, shareKeyword);
        }

        throw new SqlParseException("Expected READ, UPDATE, NO KEY UPDATE, SHARE, or KEY SHARE", _current.Position);
    }

    private readonly record struct PgLockWait(SyntaxToken? NowaitKeyword, SyntaxToken? SkipKeyword, SyntaxToken? LockedKeyword, SyntaxToken? End);

    private PgLockWait ParsePgLockWait()
    {
        if (IdentifierEquals(Keyword.Nowait))
        {
            var nowaitKeyword = Advance();
            return new PgLockWait(nowaitKeyword, null, null, nowaitKeyword);
        }

        if (!IdentifierEquals(Keyword.Skip))
        {
            return default;
        }

        var skipKeyword = Advance();
        var lockedKeyword = ExpectIdent(Keyword.Locked);
        return new PgLockWait(null, skipKeyword, lockedKeyword, lockedKeyword);
    }
}
