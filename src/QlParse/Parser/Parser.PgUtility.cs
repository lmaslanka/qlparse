namespace QlParse;

internal sealed partial class Parser
{
    // Workstream F: utility, session & security. Dispatches on the first token's kind and then its
    // length, so a statement that isn't ours returns null after a couple of integer compares.
    internal Query? TryParsePgUtilityStatement() => _current.Kind switch
    {
        SyntaxKind.Identifier => TryParsePgUtilityWord(_current.TextOf(_source)),
        SyntaxKind.SetKeyword => TryParsePgSet(),
        SyntaxKind.EndKeyword => TryParsePgEnd(),
        _ => null,
    };

    private Query? TryParsePgUtilityWord(ReadOnlySpan<char> text) => text.Length switch
    {
        Len4 => TryParsePgUtilityLen4(text),
        Len5 => TryParsePgUtilityLen5(text),
        Len6 => TryParsePgUtilityLen6(text),
        Len7 => TryParsePgUtilityLen7(text),
        Len8 => TryParsePgUtilityLen8(text),
        Len10 => TryParsePgUtilityLen10(text),
        _ => null,
    };

    private Query? TryParsePgUtilityLen4(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.UtilLock))
        {
            return ParseLockTable();
        }

        if (TextEquals(text, Keyword.Show))
        {
            return ParseShow();
        }

        if (TextEquals(text, Keyword.UtilLoad))
        {
            return ParseLoad();
        }

        if (TextEquals(text, Keyword.Drop))
        {
            return TryParsePgDrop();
        }

        return null;
    }

    private Query? TryParsePgUtilityLen5(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Begin))
        {
            return TryParsePgBegin();
        }

        if (TextEquals(text, Keyword.Alter))
        {
            return TryParsePgAlter();
        }

        if (TextEquals(text, Keyword.UtilReset))
        {
            return ParseReset();
        }

        if (TextEquals(text, Keyword.UtilAbort))
        {
            return ParseRollback();
        }

        return null;
    }

    private Query? TryParsePgUtilityLen6(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Create))
        {
            return TryParsePgCreate();
        }

        if (TextEquals(text, Keyword.Commit))
        {
            return TryParseCommitPrepared();
        }

        if (TextEquals(text, Keyword.UtilVacuum))
        {
            return ParseVacuum();
        }

        if (TextEquals(text, Keyword.UtilListen))
        {
            return ParseListen();
        }

        if (TextEquals(text, Keyword.UtilNotify))
        {
            return ParseNotify();
        }

        return null;
    }

    private Query? TryParsePgUtilityLen7(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.UtilExplain))
        {
            return ParseExplain();
        }

        if (TextEquals(text, Keyword.UtilAnalyze) || TextEquals(text, Keyword.UtilAnalyse))
        {
            return ParseAnalyze();
        }

        if (TextEquals(text, Keyword.Prepare))
        {
            return TryParsePgPrepare();
        }

        if (TextEquals(text, Keyword.Execute))
        {
            return TryParsePgExecute();
        }

        if (TextEquals(text, Keyword.Comment))
        {
            return ParsePgComment();
        }

        if (TextEquals(text, Keyword.UtilCluster))
        {
            return ParseCluster();
        }

        if (TextEquals(text, Keyword.UtilDiscard))
        {
            return ParseDiscard();
        }

        return null;
    }

    private Query? TryParsePgUtilityLen8(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Rollback))
        {
            return TryParseRollbackPrepared();
        }

        if (TextEquals(text, Keyword.UtilUnlisten))
        {
            return ParseUnlisten();
        }

        if (TextEquals(text, Keyword.UtilSecurity))
        {
            return NextEquals(Keyword.Label) ? ParseSecurityLabel() : null;
        }

        if (TextEquals(text, Keyword.UtilReassign))
        {
            return ParseReassignOwned();
        }

        return null;
    }

    private Query? TryParsePgUtilityLen10(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.UtilCheckpoint))
        {
            var checkpoint = Advance();
            return new CheckpointStatement { Span = checkpoint.Span, CheckpointKeyword = checkpoint };
        }

        if (TextEquals(text, Keyword.Deallocate))
        {
            return TryParsePgDeallocate();
        }

        return null;
    }

    private Query? TryParsePgCreate()
    {
        var next = NextKind;
        if (next is SyntaxKind.UserKeyword or SyntaxKind.GroupKeyword)
        {
            return IsKeywordAt(_index + 1, Keyword.UtilMapping) ? null : ParsePgCreateRole();
        }

        if (next != SyntaxKind.Identifier)
        {
            return null;
        }

        if (NextEquals(Keyword.Role))
        {
            return ParsePgCreateRole();
        }

        if (NextEquals(Keyword.UtilPolicy))
        {
            return ParseCreatePolicy();
        }

        return NextEquals(Keyword.UtilDatabase) ? ParseCreateDatabase() : null;
    }

    private Query? TryParsePgAlter()
    {
        var next = NextKind;
        if (next == SyntaxKind.UserKeyword)
        {
            return IsKeywordAt(_index + 1, Keyword.UtilMapping) ? null : ParseAlterRole();
        }

        if (next != SyntaxKind.Identifier)
        {
            return null;
        }

        return TryParsePgAlterRoleOrPolicy() ?? TryParsePgAlterSystemOrDatabase();
    }

    private Query? TryParsePgAlterRoleOrPolicy()
    {
        if (NextEquals(Keyword.Role))
        {
            return ParseAlterRole();
        }

        if (NextEquals(Keyword.UtilPolicy))
        {
            return ParseAlterPolicy();
        }

        return NextEquals(Keyword.Default) && IsKeywordAt(_index + 1, Keyword.Privileges)
            ? ParseAlterDefaultPrivileges()
            : null;
    }

    private Query? TryParsePgAlterSystemOrDatabase()
    {
        if (NextEquals(Keyword.System))
        {
            return ParseAlterSystem();
        }

        return NextEquals(Keyword.UtilDatabase) ? ParseAlterDatabase() : null;
    }

    private Query? TryParsePgDrop()
    {
        var next = NextKind;
        if (next is SyntaxKind.UserKeyword or SyntaxKind.GroupKeyword)
        {
            return IsKeywordAt(_index + 1, Keyword.UtilMapping) ? null : ParsePgDropRole();
        }

        if (next != SyntaxKind.Identifier)
        {
            return null;
        }

        if (NextEquals(Keyword.Role))
        {
            return ParsePgDropRole();
        }

        if (NextEquals(Keyword.UtilPolicy))
        {
            return ParseDropPolicy();
        }

        if (NextEquals(Keyword.UtilOwned))
        {
            return ParseDropOwned();
        }

        return NextEquals(Keyword.UtilDatabase) ? ParseDropDatabase() : null;
    }

    private LockTableStatement ParseLockTable()
    {
        var lockKeyword = Advance();
        var tableKeyword = IdentifierEquals(Keyword.Table) ? Advance() : (SyntaxToken?)null;
        var tables = new List<LockTarget> { ParseLockTarget() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            tables.Add(ParseLockTarget());
        }

        var end = tables[^1].Span;
        SyntaxToken? inKeyword = null;
        IReadOnlyList<SyntaxToken> mode = [];
        SyntaxToken? modeKeyword = null;
        if (_current.Kind == SyntaxKind.InKeyword)
        {
            inKeyword = Advance();
            mode = ParseLockMode();
            var modeToken = ExpectIdent(Keyword.UtilMode);
            modeKeyword = modeToken;
            end = modeToken.Span;
        }

        SyntaxToken? nowait = null;
        if (IdentifierEquals(Keyword.UtilNowait))
        {
            var nowaitToken = Advance();
            nowait = nowaitToken;
            end = nowaitToken.Span;
        }

        return new LockTableStatement
        {
            Span = SourceSpan.From(lockKeyword, end),
            LockKeyword = lockKeyword,
            TableKeyword = tableKeyword,
            Tables = tables,
            InKeyword = inKeyword,
            LockMode = mode,
            ModeKeyword = modeKeyword,
            NowaitKeyword = nowait,
        };
    }

    private LockTarget ParseLockTarget()
    {
        var only = _current.Kind == SyntaxKind.OnlyKeyword ? Advance() : (SyntaxToken?)null;
        var name = ParseQualifiedName();
        var star = _current.Kind == SyntaxKind.Star ? Advance() : (SyntaxToken?)null;
        var start = only ?? name[0];
        var end = star ?? name[^1];
        return new LockTarget
        {
            Span = SourceSpan.From(start, end),
            OnlyKeyword = only,
            Name = name,
            Star = star,
        };
    }

    // ACCESS SHARE | ROW SHARE | ROW EXCLUSIVE | SHARE UPDATE EXCLUSIVE | SHARE | SHARE ROW EXCLUSIVE
    // | EXCLUSIVE | ACCESS EXCLUSIVE
    private List<SyntaxToken> ParseLockMode()
    {
        if (IdentifierEquals(Keyword.UtilAccess) || IdentifierEquals(Keyword.Row))
        {
            var first = Advance();
            return [first, ExpectShareOrExclusive()];
        }

        if (IdentifierEquals(Keyword.UtilExclusive))
        {
            return [Advance()];
        }

        var share = ExpectIdent(Keyword.Share);
        if (_current.Kind == SyntaxKind.UpdateKeyword || IdentifierEquals(Keyword.Row))
        {
            var second = Advance();
            return [share, second, ExpectIdent(Keyword.UtilExclusive)];
        }

        return [share];
    }

    private SyntaxToken ExpectShareOrExclusive()
    {
        if (!IdentifierEquals(Keyword.Share) && !IdentifierEquals(Keyword.UtilExclusive))
        {
            throw new SqlParseException($"Expected SHARE or EXCLUSIVE, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    private ListenStatement ParseListen()
    {
        var listenKeyword = Advance();
        var channel = Expect(SyntaxKind.Identifier);
        return new ListenStatement
        {
            Span = SourceSpan.From(listenKeyword, channel),
            ListenKeyword = listenKeyword,
            Channel = channel,
        };
    }

    private NotifyStatement ParseNotify()
    {
        var notifyKeyword = Advance();
        var channel = Expect(SyntaxKind.Identifier);
        SyntaxToken? comma = null;
        SyntaxToken? payload = null;
        var end = channel;
        if (_current.Kind == SyntaxKind.Comma)
        {
            comma = Advance();
            var payloadToken = Expect(SyntaxKind.String);
            payload = payloadToken;
            end = payloadToken;
        }

        return new NotifyStatement
        {
            Span = SourceSpan.From(notifyKeyword, end),
            NotifyKeyword = notifyKeyword,
            Channel = channel,
            Comma = comma,
            Payload = payload,
        };
    }

    private UnlistenStatement ParseUnlisten()
    {
        var unlistenKeyword = Advance();
        var channel = _current.Kind == SyntaxKind.Star ? Advance() : Expect(SyntaxKind.Identifier);
        return new UnlistenStatement
        {
            Span = SourceSpan.From(unlistenKeyword, channel),
            UnlistenKeyword = unlistenKeyword,
            Channel = channel,
        };
    }

    private LoadStatement ParseLoad()
    {
        var loadKeyword = Advance();
        var fileName = Expect(SyntaxKind.String);
        return new LoadStatement
        {
            Span = SourceSpan.From(loadKeyword, fileName),
            LoadKeyword = loadKeyword,
            FileName = fileName,
        };
    }

    private DiscardStatement ParseDiscard()
    {
        var discardKeyword = Advance();
        if (!IsDiscardTarget())
        {
            throw new SqlParseException($"Expected ALL, PLANS, SEQUENCES, or TEMP, found {_current.Kind}", _current.Position);
        }

        var target = Advance();
        return new DiscardStatement
        {
            Span = SourceSpan.From(discardKeyword, target),
            DiscardKeyword = discardKeyword,
            Target = target,
        };
    }

    private bool IsDiscardTarget() =>
        _current.Kind == SyntaxKind.AllKeyword || IdentifierEquals(Keyword.UtilPlans)
        || IdentifierEquals(Keyword.UtilSequences) || IdentifierEquals(Keyword.UtilTemp)
        || IdentifierEquals(Keyword.Temporary);

    internal bool IsStatementEndAt(int index) =>
        index >= _tokens.Count || _tokens[index].Kind is SyntaxKind.Semicolon or SyntaxKind.EndOfFile;

    internal (SyntaxToken? IfKeyword, SyntaxToken? ExistsKeyword) ParseIfExists()
    {
        if (!IdentifierEquals(Keyword.If) || NextKind != SyntaxKind.ExistsKeyword)
        {
            return (null, null);
        }

        var ifKeyword = Advance();
        return (ifKeyword, Advance());
    }
}
