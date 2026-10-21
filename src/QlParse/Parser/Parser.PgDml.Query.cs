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

    internal MaterializationClause ParseCteMaterialization()
    {
        var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
        var materializedKeyword = ExpectIdent(Keyword.Materialized);
        return new MaterializationClause
        {
            Span = SourceSpan.From(notKeyword ?? materializedKeyword, materializedKeyword),
            NotKeyword = notKeyword,
            MaterializedKeyword = materializedKeyword,
        };
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
        while (last.NextLock is not null)
        {
            last = last.NextLock;
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
        ParseLockColumns(out var ofKeyword, out var columns);
        var wait = ParsePgLockWait();
        var end = wait?.Span ?? (columns is null ? strength.Last.Span : columns[^1].Span);
        var next = _current.Kind == SyntaxKind.ForKeyword ? ParseLockClause() : null;
        return new LockClause
        {
            Span = SourceSpan.From(forKeyword, end),
            ForKeyword = forKeyword,
            Modifier = strength.Modifier,
            UpdateKeyword = strength.UpdateKeyword,
            ShareKeyword = strength.ShareKeyword,
            OfKeyword = ofKeyword,
            Columns = columns,
            WaitPolicy = wait,
            NextLock = next,
        };
    }

    private readonly record struct PgLockStrength(
        LockStrengthModifier? Modifier,
        SyntaxToken? UpdateKeyword,
        SyntaxToken? ShareKeyword,
        SyntaxToken Last);

    private PgLockStrength ParsePgLockStrength()
    {
        if (_current.Kind == SyntaxKind.UpdateKeyword)
        {
            var updateKeyword = Advance();
            return new PgLockStrength(null, updateKeyword, null, updateKeyword);
        }

        if (IdentifierEquals(Keyword.Share))
        {
            var shareKeyword = Advance();
            return new PgLockStrength(null, null, shareKeyword, shareKeyword);
        }

        if (IdentifierEquals(Keyword.No))
        {
            var noKeyword = Advance();
            var keyKeyword = ExpectIdent(Keyword.Key);
            var updateKeyword = Expect(SyntaxKind.UpdateKeyword);
            var modifier = new LockStrengthModifier { Span = SourceSpan.From(noKeyword, keyKeyword), NoKeyword = noKeyword, KeyKeyword = keyKeyword };
            return new PgLockStrength(modifier, updateKeyword, null, updateKeyword);
        }

        if (IdentifierEquals(Keyword.Key))
        {
            var keyKeyword = Advance();
            var shareKeyword = ExpectIdent(Keyword.Share);
            var modifier = new LockStrengthModifier { Span = keyKeyword.Span, KeyKeyword = keyKeyword };
            return new PgLockStrength(modifier, null, shareKeyword, shareKeyword);
        }

        throw new SqlParseException("Expected READ, UPDATE, NO KEY UPDATE, SHARE, or KEY SHARE", _current.Position);
    }

    private LockWaitPolicy? ParsePgLockWait()
    {
        if (IdentifierEquals(Keyword.Nowait))
        {
            var nowaitKeyword = Advance();
            return new LockWaitPolicy { Span = nowaitKeyword.Span, NowaitKeyword = nowaitKeyword };
        }

        if (!IdentifierEquals(Keyword.Skip))
        {
            return null;
        }

        var skipKeyword = Advance();
        var lockedKeyword = ExpectIdent(Keyword.Locked);
        return new LockWaitPolicy
        {
            Span = SourceSpan.From(skipKeyword, lockedKeyword),
            SkipKeyword = skipKeyword,
            LockedKeyword = lockedKeyword,
        };
    }
}
