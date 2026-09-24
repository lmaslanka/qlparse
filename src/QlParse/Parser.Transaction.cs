namespace QlParse;

internal sealed partial class Parser
{
    private bool IsStartTransaction() =>
        IdentifierEquals(Keyword.Start) && NextEquals(Keyword.Transaction);

    private bool IsSetTransaction()
    {
        if (_current.Kind != SyntaxKind.SetKeyword)
        {
            return false;
        }

        if (NextEquals(Keyword.Transaction))
        {
            return true;
        }

        if (!NextEquals(Keyword.Local) || _index + 1 >= _tokens.Count)
        {
            return false;
        }

        return TokenEquals(_tokens[_index + 1], Keyword.Transaction);
    }

    private bool IsCommit() => IdentifierEquals(Keyword.Commit);

    private bool IsRollback() => IdentifierEquals(Keyword.Rollback);

    private bool IsSavepoint() => IdentifierEquals(Keyword.Savepoint);

    private bool IsReleaseSavepoint() =>
        IdentifierEquals(Keyword.Release) && NextEquals(Keyword.Savepoint);

    private bool IsSetConstraints() =>
        _current.Kind == SyntaxKind.SetKeyword && NextEquals(Keyword.Constraints);

    private bool IsSetSession() =>
        _current.Kind == SyntaxKind.SetKeyword && NextEquals(Keyword.Session);

    private bool IsSetTimeZone() =>
        _current.Kind == SyntaxKind.SetKeyword
        && NextKind == SyntaxKind.TimeKeyword
        && _index + 1 < _tokens.Count
        && TokenEquals(_tokens[_index + 1], Keyword.Zone);

    private bool IsSetCatalog() =>
        _current.Kind == SyntaxKind.SetKeyword && NextEquals(Keyword.Catalog);

    private bool IsSetSchema() =>
        _current.Kind == SyntaxKind.SetKeyword && NextEquals(Keyword.Schema);

    private bool IsSetPath() =>
        _current.Kind == SyntaxKind.SetKeyword && NextEquals(Keyword.Path);

    private bool IsSetNames() =>
        _current.Kind == SyntaxKind.SetKeyword && NextEquals(Keyword.Names);

    private bool IsSetCharacterSet() =>
        _current.Kind == SyntaxKind.SetKeyword
        && NextEquals(Keyword.Character)
        && _index + 1 < _tokens.Count
        && _tokens[_index + 1].Kind == SyntaxKind.SetKeyword;

    private bool IsSetCollation() =>
        _current.Kind == SyntaxKind.SetKeyword && NextEquals(Keyword.Collation);

    private StartTransactionStatement ParseStartTransaction()
    {
        var startKeyword = Advance();
        var transactionKeyword = ExpectIdent(Keyword.Transaction);
        IReadOnlyList<TransactionMode> modes = [];
        var end = transactionKeyword.Span;
        if (IsTransactionMode())
        {
            modes = ParseTransactionModes();
            end = modes[^1].Span;
        }

        return new StartTransactionStatement
        {
            Span = SourceSpan.From(startKeyword, end),
            StartKeyword = startKeyword,
            TransactionKeyword = transactionKeyword,
            Modes = modes,
        };
    }

    private SetTransactionStatement ParseSetTransaction()
    {
        var setKeyword = Advance();
        SyntaxToken? localKeyword = null;
        if (IdentifierEquals(Keyword.Local))
        {
            localKeyword = Advance();
        }

        var transactionKeyword = ExpectIdent(Keyword.Transaction);
        var modes = ParseTransactionModes();
        return new SetTransactionStatement
        {
            Span = SourceSpan.From(setKeyword, modes[^1].Span),
            SetKeyword = setKeyword,
            LocalKeyword = localKeyword,
            TransactionKeyword = transactionKeyword,
            Modes = modes,
        };
    }

    private CommitStatement ParseCommit()
    {
        var commitKeyword = Advance();
        ParseWorkAndChain(out var work, out var andKeyword, out var noKeyword, out var chain);
        var end = chain?.Span ?? work?.Span ?? commitKeyword.Span;
        return new CommitStatement
        {
            Span = SourceSpan.From(commitKeyword, end),
            CommitKeyword = commitKeyword,
            WorkKeyword = work,
            AndKeyword = andKeyword,
            NoKeyword = noKeyword,
            ChainKeyword = chain,
        };
    }

    private RollbackStatement ParseRollback()
    {
        var rollbackKeyword = Advance();
        var work = IdentifierEquals(Keyword.Work) ? Advance() : (SyntaxToken?)null;
        if (IdentifierEquals(Keyword.To))
        {
            var toKeyword = Advance();
            var savepointKeyword = ExpectIdent(Keyword.Savepoint);
            var savepoint = ParseSavepointName();
            return new RollbackStatement
            {
                Span = SourceSpan.From(rollbackKeyword, savepoint),
                RollbackKeyword = rollbackKeyword,
                WorkKeyword = work,
                ToKeyword = toKeyword,
                SavepointKeyword = savepointKeyword,
                Savepoint = savepoint,
            };
        }

        SyntaxToken? andKeyword = null;
        SyntaxToken? noKeyword = null;
        SyntaxToken? chain = null;
        if (_current.Kind == SyntaxKind.AndKeyword)
        {
            andKeyword = Advance();
            if (IdentifierEquals(Keyword.No))
            {
                noKeyword = Advance();
            }

            chain = ExpectIdent(Keyword.Chain);
        }

        var end = chain?.Span ?? work?.Span ?? rollbackKeyword.Span;
        return new RollbackStatement
        {
            Span = SourceSpan.From(rollbackKeyword, end),
            RollbackKeyword = rollbackKeyword,
            WorkKeyword = work,
            AndKeyword = andKeyword,
            NoKeyword = noKeyword,
            ChainKeyword = chain,
        };
    }

    private SavepointStatement ParseSavepoint()
    {
        var savepointKeyword = Advance();
        var name = ParseSavepointName();
        return new SavepointStatement
        {
            Span = SourceSpan.From(savepointKeyword, name),
            SavepointKeyword = savepointKeyword,
            Name = name,
        };
    }

    private ReleaseSavepointStatement ParseReleaseSavepoint()
    {
        var releaseKeyword = Advance();
        var savepointKeyword = ExpectIdent(Keyword.Savepoint);
        var name = ParseSavepointName();
        return new ReleaseSavepointStatement
        {
            Span = SourceSpan.From(releaseKeyword, name),
            ReleaseKeyword = releaseKeyword,
            SavepointKeyword = savepointKeyword,
            Name = name,
        };
    }

    private SetConstraintsStatement ParseSetConstraints()
    {
        var setKeyword = Advance();
        var constraintsKeyword = ExpectIdent(Keyword.Constraints);
        SyntaxToken? allKeyword = null;
        IReadOnlyList<IReadOnlyList<SyntaxToken>>? names = null;
        if (_current.Kind == SyntaxKind.AllKeyword)
        {
            allKeyword = Advance();
        }
        else
        {
            names = ParseSchemaNameList();
        }

        if (!IdentifierEquals(Keyword.Deferred) && !IdentifierEquals(Keyword.Immediate))
        {
            throw new SqlParseException($"Expected DEFERRED or IMMEDIATE, found {_current.Kind}", _current.Position);
        }

        var mode = Advance();
        return new SetConstraintsStatement
        {
            Span = SourceSpan.From(setKeyword, mode),
            SetKeyword = setKeyword,
            ConstraintsKeyword = constraintsKeyword,
            AllKeyword = allKeyword,
            Names = names,
            Mode = mode,
        };
    }

    private Query ParseSetSession()
    {
        var setKeyword = Advance();
        var sessionKeyword = Advance();
        if (IdentifierEquals(Keyword.Authorization))
        {
            var authorizationKeyword = Advance();
            var value = ParseSessionValue();
            return new SetSessionAuthorizationStatement
            {
                Span = SourceSpan.From(setKeyword, value),
                SetKeyword = setKeyword,
                SessionKeyword = sessionKeyword,
                AuthorizationKeyword = authorizationKeyword,
                Value = value,
            };
        }

        var characteristicsKeyword = ExpectIdent(Keyword.Characteristics);
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var modes = ParseTransactionModes();
        return new SetSessionCharacteristicsStatement
        {
            Span = SourceSpan.From(setKeyword, modes[^1].Span),
            SetKeyword = setKeyword,
            SessionKeyword = sessionKeyword,
            CharacteristicsKeyword = characteristicsKeyword,
            AsKeyword = asKeyword,
            Modes = modes,
        };
    }

    private SetNamesStatement ParseSetNames()
    {
        var setKeyword = Advance();
        var namesKeyword = ExpectIdent(Keyword.Names);
        var value = ParseSessionValue();
        SyntaxToken? collateKeyword = null;
        IReadOnlyList<SyntaxToken>? collation = null;
        var end = value.Span;
        if (_current.Kind == SyntaxKind.CollateKeyword)
        {
            collateKeyword = Advance();
            collation = ParseQualifiedName();
            end = collation[^1].Span;
        }

        return new SetNamesStatement
        {
            Span = SourceSpan.From(setKeyword, end),
            SetKeyword = setKeyword,
            NamesKeyword = namesKeyword,
            Value = value,
            CollateKeyword = collateKeyword,
            Collation = collation,
        };
    }

    private SetCharacterSetStatement ParseSetCharacterSet()
    {
        var setKeyword = Advance();
        var characterKeyword = ExpectIdent(Keyword.Character);
        var characterSetKeyword = Expect(SyntaxKind.SetKeyword);
        var value = ParseSessionValue();
        return new SetCharacterSetStatement
        {
            Span = SourceSpan.From(setKeyword, value),
            SetKeyword = setKeyword,
            CharacterKeyword = characterKeyword,
            CharacterSetKeyword = characterSetKeyword,
            Value = value,
        };
    }

    private SetCollationStatement ParseSetCollation()
    {
        var setKeyword = Advance();
        var collationKeyword = ExpectIdent(Keyword.Collation);
        var end = ParseNameOrValue(out var value, out var name);
        return new SetCollationStatement
        {
            Span = SourceSpan.From(setKeyword, end),
            SetKeyword = setKeyword,
            CollationKeyword = collationKeyword,
            Value = value,
            Name = name,
        };
    }

    private SetTimeZoneStatement ParseSetTimeZone()
    {
        var setKeyword = Advance();
        var timeKeyword = Expect(SyntaxKind.TimeKeyword);
        var zoneKeyword = ExpectIdent(Keyword.Zone);
        if (IdentifierEquals(Keyword.Local))
        {
            var localKeyword = Advance();
            return new SetTimeZoneStatement
            {
                Span = SourceSpan.From(setKeyword, localKeyword),
                SetKeyword = setKeyword,
                TimeKeyword = timeKeyword,
                ZoneKeyword = zoneKeyword,
                LocalKeyword = localKeyword,
            };
        }

        var value = ParseExpression();
        return new SetTimeZoneStatement
        {
            Span = SourceSpan.From(setKeyword, value.Span),
            SetKeyword = setKeyword,
            TimeKeyword = timeKeyword,
            ZoneKeyword = zoneKeyword,
            Value = value,
        };
    }

    private SetCatalogStatement ParseSetCatalog()
    {
        var setKeyword = Advance();
        var catalogKeyword = ExpectIdent(Keyword.Catalog);
        var end = ParseNameOrValue(out var value, out var name);
        return new SetCatalogStatement
        {
            Span = SourceSpan.From(setKeyword, end),
            SetKeyword = setKeyword,
            CatalogKeyword = catalogKeyword,
            Value = value,
            Name = name,
        };
    }

    private SetSchemaStatement ParseSetSchema()
    {
        var setKeyword = Advance();
        var schemaKeyword = ExpectIdent(Keyword.Schema);
        var end = ParseNameOrValue(out var value, out var name);
        return new SetSchemaStatement
        {
            Span = SourceSpan.From(setKeyword, end),
            SetKeyword = setKeyword,
            SchemaKeyword = schemaKeyword,
            Value = value,
            Name = name,
        };
    }

    private SetPathStatement ParseSetPath()
    {
        var setKeyword = Advance();
        var pathKeyword = ExpectIdent(Keyword.Path);
        SyntaxToken? value = null;
        IReadOnlyList<IReadOnlyList<SyntaxToken>>? names = null;
        SourceSpan end;
        if (_current.Kind == SyntaxKind.Identifier)
        {
            names = ParseSchemaNameList();
            end = names[^1][^1].Span;
        }
        else
        {
            var token = ParseSessionValue();
            value = token;
            end = token.Span;
        }

        return new SetPathStatement
        {
            Span = SourceSpan.From(setKeyword, end),
            SetKeyword = setKeyword,
            PathKeyword = pathKeyword,
            Value = value,
            Names = names,
        };
    }

    private List<TransactionMode> ParseTransactionModes()
    {
        var modes = new List<TransactionMode> { ParseTransactionMode() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            modes.Add(ParseTransactionMode());
        }

        return modes;
    }

    private TransactionMode ParseTransactionMode()
    {
        if (IdentifierEquals(Keyword.Isolation))
        {
            return ParseIsolationLevel();
        }

        if (_current.Kind == SyntaxKind.ReadKeyword)
        {
            return ParseAccessMode();
        }

        if (!IdentifierEquals(Keyword.Diagnostics))
        {
            throw new SqlParseException($"Expected transaction mode, found {_current.Kind}", _current.Position);
        }

        var diagnostics = Advance();
        var sizeKeyword = ExpectIdent(Keyword.Size);
        var size = ParseExpression();
        return new TransactionMode
        {
            Span = SourceSpan.From(diagnostics, size.Span),
            DiagnosticsKeyword = diagnostics,
            SizeKeyword = sizeKeyword,
            Size = size,
        };
    }

    private TransactionMode ParseIsolationLevel()
    {
        var isolation = Advance();
        var levelKeyword = ExpectIdent(Keyword.Level);
        if (_current.Kind == SyntaxKind.ReadKeyword)
        {
            var read = Advance();
            if (!IdentifierEquals(Keyword.Uncommitted) && !IdentifierEquals(Keyword.Committed))
            {
                throw new SqlParseException($"Expected UNCOMMITTED or COMMITTED, found {_current.Kind}", _current.Position);
            }

            var level = Advance();
            return new TransactionMode
            {
                Span = SourceSpan.From(isolation, level),
                IsolationKeyword = isolation,
                LevelKeyword = levelKeyword,
                Access = read,
                Level = level,
            };
        }

        if (IdentifierEquals(Keyword.Repeatable))
        {
            var repeatable = Advance();
            var read = Expect(SyntaxKind.ReadKeyword);
            return new TransactionMode
            {
                Span = SourceSpan.From(isolation, read),
                IsolationKeyword = isolation,
                LevelKeyword = levelKeyword,
                Level = repeatable,
                ReadKeyword = read,
            };
        }

        if (!IdentifierEquals(Keyword.Serializable))
        {
            throw new SqlParseException($"Expected isolation level, found {_current.Kind}", _current.Position);
        }

        var serializable = Advance();
        return new TransactionMode
        {
            Span = SourceSpan.From(isolation, serializable),
            IsolationKeyword = isolation,
            LevelKeyword = levelKeyword,
            Level = serializable,
        };
    }

    private TransactionMode ParseAccessMode()
    {
        var read = Advance();
        SyntaxToken level;
        if (_current.Kind == SyntaxKind.OnlyKeyword)
        {
            level = Advance();
        }
        else
        {
            level = ExpectIdent(Keyword.Write);
        }

        return new TransactionMode
        {
            Span = SourceSpan.From(read, level),
            Access = read,
            Level = level,
        };
    }

    private void ParseWorkAndChain(
        out SyntaxToken? work,
        out SyntaxToken? andKeyword,
        out SyntaxToken? noKeyword,
        out SyntaxToken? chain)
    {
        work = IdentifierEquals(Keyword.Work) ? Advance() : null;
        andKeyword = null;
        noKeyword = null;
        chain = null;
        if (_current.Kind != SyntaxKind.AndKeyword)
        {
            return;
        }

        andKeyword = Advance();
        if (IdentifierEquals(Keyword.No))
        {
            noKeyword = Advance();
        }

        chain = ExpectIdent(Keyword.Chain);
    }

    private SyntaxToken ParseSavepointName()
    {
        if (_current.Kind is SyntaxKind.Identifier or SyntaxKind.String or SyntaxKind.QuestionMark or SyntaxKind.EmbeddedHost)
        {
            return Advance();
        }

        throw new SqlParseException($"Expected savepoint, found {_current.Kind}", _current.Position);
    }

    private SyntaxToken ParseSessionValue()
    {
        if (!IsSessionValue())
        {
            throw new SqlParseException($"Expected value, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    private SourceSpan ParseNameOrValue(out SyntaxToken? value, out IReadOnlyList<SyntaxToken>? name)
    {
        if (_current.Kind == SyntaxKind.Identifier)
        {
            name = ParseQualifiedName();
            value = null;
            return name[^1].Span;
        }

        var token = ParseSessionValue();
        value = token;
        name = null;
        return token.Span;
    }

    private bool IsTransactionMode() =>
        IdentifierEquals(Keyword.Isolation)
        || _current.Kind == SyntaxKind.ReadKeyword
        || IdentifierEquals(Keyword.Diagnostics);

    private bool IsSessionValue() =>
        _current.Kind is SyntaxKind.Identifier or SyntaxKind.String or SyntaxKind.QuestionMark or SyntaxKind.EmbeddedHost
            or SyntaxKind.CurrentUserKeyword or SyntaxKind.CurrentRoleKeyword or SyntaxKind.SessionUserKeyword
            or SyntaxKind.SystemUserKeyword or SyntaxKind.UserKeyword;
}
