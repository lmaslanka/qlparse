namespace QlParse;

internal sealed partial class Parser
{
    private readonly record struct ConfigurationValues(
        SyntaxToken? DefaultKeyword,
        IReadOnlyList<OptionValue> Values,
        SourceSpan End);

    // BEGIN [WORK | TRANSACTION] [mode ...]. A BEGIN followed by anything else is a PSM block.
    private BeginTransactionStatement? TryParsePgBegin()
    {
        if (!IsPgBeginTransaction())
        {
            return null;
        }

        var beginKeyword = Advance();
        var transactionKeyword = IdentifierEquals(Keyword.Work) || IdentifierEquals(Keyword.Transaction)
            ? Advance()
            : (SyntaxToken?)null;
        IReadOnlyList<TransactionMode> modes = [];
        var end = transactionKeyword?.Span ?? beginKeyword.Span;
        if (IsTransactionMode())
        {
            modes = ParseTransactionModes();
            end = modes[^1].Span;
        }

        return new BeginTransactionStatement
        {
            Span = SourceSpan.From(beginKeyword, end),
            BeginKeyword = beginKeyword,
            TransactionKeyword = transactionKeyword,
            Modes = modes,
        };
    }

    private bool IsPgBeginTransaction()
    {
        var next = NextKind;
        if (next is SyntaxKind.Semicolon or SyntaxKind.EndOfFile or SyntaxKind.ReadKeyword)
        {
            return true;
        }

        if (next == SyntaxKind.NotKeyword)
        {
            return IsKeywordAt(_index + 1, Keyword.Deferrable);
        }

        return NextEquals(Keyword.Work) || NextEquals(Keyword.Transaction)
            || NextEquals(Keyword.Isolation) || NextEquals(Keyword.Deferrable);
    }

    // END [WORK | TRANSACTION] [AND [NO] CHAIN] is Postgres's alias for COMMIT.
    private CommitStatement? TryParsePgEnd()
    {
        if (NextKind != SyntaxKind.AndKeyword && !IsStatementEndAt(_index)
            && !NextEquals(Keyword.Work) && !NextEquals(Keyword.Transaction))
        {
            return null;
        }

        return ParseCommit();
    }

    private TransactionMode ParsePgDeferrableMode()
    {
        var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
        var deferrable = ExpectIdent(Keyword.Deferrable);
        return new TransactionMode
        {
            Span = SourceSpan.From(notKeyword ?? deferrable, deferrable),
            NotKeyword = notKeyword,
            DeferrableKeyword = deferrable,
        };
    }

    internal bool IsPgDeferrableMode() =>
        IdentifierEquals(Keyword.Deferrable)
        || (_current.Kind == SyntaxKind.NotKeyword && NextEquals(Keyword.Deferrable));

    private CommitPreparedStatement? TryParseCommitPrepared()
    {
        if (!NextEquals(Keyword.UtilPrepared))
        {
            return null;
        }

        var commitKeyword = Advance();
        var preparedKeyword = Advance();
        var transactionId = Expect(SyntaxKind.String);
        return new CommitPreparedStatement
        {
            Span = SourceSpan.From(commitKeyword, transactionId),
            CommitKeyword = commitKeyword,
            PreparedKeyword = preparedKeyword,
            TransactionId = transactionId,
        };
    }

    private RollbackPreparedStatement? TryParseRollbackPrepared()
    {
        if (!NextEquals(Keyword.UtilPrepared))
        {
            return null;
        }

        var rollbackKeyword = Advance();
        var preparedKeyword = Advance();
        var transactionId = Expect(SyntaxKind.String);
        return new RollbackPreparedStatement
        {
            Span = SourceSpan.From(rollbackKeyword, transactionId),
            RollbackKeyword = rollbackKeyword,
            PreparedKeyword = preparedKeyword,
            TransactionId = transactionId,
        };
    }

    // PREPARE TRANSACTION 'gid' | PREPARE name [(type, ...)] AS statement. ISO PREPARE name FROM … is left alone.
    private Query? TryParsePgPrepare()
    {
        if (NextEquals(Keyword.Transaction))
        {
            var prepareKeyword = Advance();
            var transactionKeyword = Advance();
            var transactionId = Expect(SyntaxKind.String);
            return new PrepareTransactionStatement
            {
                Span = SourceSpan.From(prepareKeyword, transactionId),
                PrepareKeyword = prepareKeyword,
                TransactionKeyword = transactionKeyword,
                TransactionId = transactionId,
            };
        }

        var afterName = _index + 1;
        if (NextKind != SyntaxKind.Identifier || afterName >= _tokens.Count
            || _tokens[afterName].Kind is not (SyntaxKind.OpenParen or SyntaxKind.AsKeyword))
        {
            return null;
        }

        return ParsePrepareAs();
    }

    private PrepareAsStatement ParsePrepareAs()
    {
        var prepareKeyword = Advance();
        var name = Advance();
        SyntaxToken? openParen = null;
        IReadOnlyList<DataType> types = [];
        SyntaxToken? closeParen = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            openParen = Advance();
            types = ParseDataTypeList();
            closeParen = Expect(SyntaxKind.CloseParen);
        }

        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var statement = ParseStatement();
        return new PrepareAsStatement
        {
            Span = SourceSpan.From(prepareKeyword, statement.Span),
            PrepareKeyword = prepareKeyword,
            Name = name,
            OpenParen = openParen,
            ParameterTypes = types,
            CloseParen = closeParen,
            AsKeyword = asKeyword,
            Statement = statement,
        };
    }

    internal List<DataType> ParseDataTypeList()
    {
        var types = new List<DataType> { ParseDataType() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            types.Add(ParseDataType());
        }

        return types;
    }

    // EXECUTE name [(arg, ...)]; ISO EXECUTE name INTO/USING … keeps its own parser.
    private ExecutePreparedStatement? TryParsePgExecute()
    {
        var afterName = _index + 1;
        if (NextKind != SyntaxKind.Identifier
            || (!IsStatementEndAt(afterName) && _tokens[afterName].Kind != SyntaxKind.OpenParen))
        {
            return null;
        }

        var executeKeyword = Advance();
        var name = Advance();
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new ExecutePreparedStatement
            {
                Span = SourceSpan.From(executeKeyword, name),
                ExecuteKeyword = executeKeyword,
                Name = name,
            };
        }

        var openParen = Advance();
        var arguments = new List<Expression> { ParseExpression() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            arguments.Add(ParseExpression());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new ExecutePreparedStatement
        {
            Span = SourceSpan.From(executeKeyword, closeParen),
            ExecuteKeyword = executeKeyword,
            Name = name,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
        };
    }

    // DEALLOCATE [PREPARE] {name | ALL}; ISO DEALLOCATE DESCRIPTOR keeps its own parser.
    private DeallocatePreparedStatement? TryParsePgDeallocate()
    {
        if (NextEquals(Keyword.Descriptor) || NextEquals(Keyword.Sql))
        {
            return null;
        }

        var deallocateKeyword = Advance();
        var prepareKeyword = IdentifierEquals(Keyword.Prepare) ? Advance() : (SyntaxToken?)null;
        if (_current.Kind == SyntaxKind.AllKeyword)
        {
            var all = Advance();
            return new DeallocatePreparedStatement
            {
                Span = SourceSpan.From(deallocateKeyword, all),
                DeallocateKeyword = deallocateKeyword,
                PrepareKeyword = prepareKeyword,
                AllKeyword = all,
            };
        }

        var name = Expect(SyntaxKind.Identifier);
        return new DeallocatePreparedStatement
        {
            Span = SourceSpan.From(deallocateKeyword, name),
            DeallocateKeyword = deallocateKeyword,
            PrepareKeyword = prepareKeyword,
            Name = name,
        };
    }

    // SET [SESSION | LOCAL] name {= | TO} value [, ...] | DEFAULT, plus TIME ZONE and scoped ROLE.
    // Other SET forms (TRANSACTION, CONSTRAINTS, SESSION AUTHORIZATION, PSM targets, …) return null.
    private Query? TryParsePgSet()
    {
        var index = _index;
        if (IsKeywordAt(index, Keyword.Session) || IsKeywordAt(index, Keyword.Local))
        {
            index++;
        }

        if (index >= _tokens.Count)
        {
            return null;
        }

        var token = _tokens[index];
        if (token.Kind == SyntaxKind.TimeKeyword)
        {
            return IsKeywordAt(index + 1, Keyword.Zone) ? ParsePgSetTimeZone() : null;
        }

        if (index != _index && TokenEquals(token, Keyword.Role))
        {
            return ParsePgScopedSetRole();
        }

        return token.Kind == SyntaxKind.Identifier && IsConfigurationAssignmentAt(index + 1)
            ? ParseSetConfiguration()
            : null;
    }

    private bool IsConfigurationAssignmentAt(int index)
    {
        while (index + 1 < _tokens.Count && _tokens[index].Kind == SyntaxKind.Dot
            && _tokens[index + 1].Kind == SyntaxKind.Identifier)
        {
            index += Len2;
        }

        return index < _tokens.Count
            && (_tokens[index].Kind == SyntaxKind.EqualsToken || TokenEquals(_tokens[index], Keyword.To));
    }

    internal SetConfigurationStatement ParseSetConfiguration()
    {
        var setKeyword = Expect(SyntaxKind.SetKeyword);
        var scope = IdentifierEquals(Keyword.Session) || IdentifierEquals(Keyword.Local) ? Advance() : (SyntaxToken?)null;
        var name = ParseQualifiedName();
        var assignment = _current.Kind == SyntaxKind.EqualsToken ? Advance() : ExpectIdent(Keyword.To);
        var values = ParseConfigurationValues();
        return new SetConfigurationStatement
        {
            Span = SourceSpan.From(setKeyword, values.End),
            SetKeyword = setKeyword,
            ScopeKeyword = scope,
            Name = name,
            AssignmentToken = assignment,
            DefaultKeyword = values.DefaultKeyword,
            Values = values.Values,
        };
    }

    private ConfigurationValues ParseConfigurationValues()
    {
        if (IdentifierEquals(Keyword.Default))
        {
            var defaultKeyword = Advance();
            return new ConfigurationValues(defaultKeyword, [], defaultKeyword.Span);
        }

        var values = new List<OptionValue> { ParseOptionValue() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            values.Add(ParseOptionValue());
        }

        return new ConfigurationValues(null, values, values[^1].Span);
    }

    internal OptionValue ParseOptionValue()
    {
        SyntaxToken? sign = null;
        if (_current.Kind is SyntaxKind.MinusToken or SyntaxKind.PlusToken && NextKind == SyntaxKind.Number)
        {
            sign = Advance();
        }

        if (_current.Kind is not (SyntaxKind.Identifier or SyntaxKind.String or SyntaxKind.Number
            or SyntaxKind.OnKeyword or SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword or SyntaxKind.NullKeyword))
        {
            throw new SqlParseException($"Expected value, found {_current.Kind}", _current.Position);
        }

        var value = Advance();
        return new OptionValue
        {
            Span = SourceSpan.From(sign ?? value, value),
            Sign = sign,
            Value = value,
        };
    }

    private SetTimeZoneStatement ParsePgSetTimeZone()
    {
        var setKeyword = Advance();
        var scope = IdentifierEquals(Keyword.Session) || IdentifierEquals(Keyword.Local) ? Advance() : (SyntaxToken?)null;
        var timeKeyword = Expect(SyntaxKind.TimeKeyword);
        var zoneKeyword = ExpectIdent(Keyword.Zone);
        if (IdentifierEquals(Keyword.Local) || IdentifierEquals(Keyword.Default))
        {
            var word = Advance();
            var isLocal = TokenEquals(word, Keyword.Local);
            return new SetTimeZoneStatement
            {
                Span = SourceSpan.From(setKeyword, word),
                SetKeyword = setKeyword,
                ScopeKeyword = scope,
                TimeKeyword = timeKeyword,
                ZoneKeyword = zoneKeyword,
                LocalKeyword = isLocal ? word : null,
                DefaultKeyword = isLocal ? null : word,
            };
        }

        var value = ParseExpression();
        return new SetTimeZoneStatement
        {
            Span = SourceSpan.From(setKeyword, value.Span),
            SetKeyword = setKeyword,
            ScopeKeyword = scope,
            TimeKeyword = timeKeyword,
            ZoneKeyword = zoneKeyword,
            Value = value,
        };
    }

    private SetRoleStatement ParsePgScopedSetRole()
    {
        var setKeyword = Advance();
        var scope = Advance();
        var roleKeyword = ExpectIdent(Keyword.Role);
        var value = ParseRoleSpecification();
        return new SetRoleStatement
        {
            Span = SourceSpan.From(setKeyword, value),
            SetKeyword = setKeyword,
            ScopeKeyword = scope,
            RoleKeyword = roleKeyword,
            Value = value,
        };
    }

    private ShowStatement ParseShow()
    {
        var showKeyword = Advance();
        if (_current.Kind == SyntaxKind.AllKeyword)
        {
            var all = Advance();
            return new ShowStatement { Span = SourceSpan.From(showKeyword, all), ShowKeyword = showKeyword, AllKeyword = all };
        }

        var name = ParseSettingName();
        return new ShowStatement
        {
            Span = SourceSpan.From(showKeyword, name[^1]),
            ShowKeyword = showKeyword,
            Name = name,
        };
    }

    internal ResetStatement ParseReset()
    {
        var resetKeyword = ExpectIdent(Keyword.UtilReset);
        if (_current.Kind == SyntaxKind.AllKeyword)
        {
            var all = Advance();
            return new ResetStatement { Span = SourceSpan.From(resetKeyword, all), ResetKeyword = resetKeyword, AllKeyword = all };
        }

        var name = ParseSettingName();
        return new ResetStatement
        {
            Span = SourceSpan.From(resetKeyword, name[^1]),
            ResetKeyword = resetKeyword,
            Name = name,
        };
    }

    // A run-time parameter name: dotted identifiers, or one of the multi-word forms
    // TIME ZONE, TRANSACTION ISOLATION LEVEL, SESSION AUTHORIZATION.
    private List<SyntaxToken> ParseSettingName()
    {
        if (_current.Kind == SyntaxKind.TimeKeyword)
        {
            var time = Advance();
            return [time, ExpectIdent(Keyword.Zone)];
        }

        if (IdentifierEquals(Keyword.Transaction) && NextEquals(Keyword.Isolation))
        {
            var transaction = Advance();
            var isolation = Advance();
            return [transaction, isolation, ExpectIdent(Keyword.Level)];
        }

        if (IdentifierEquals(Keyword.Session) && NextEquals(Keyword.Authorization))
        {
            var session = Advance();
            return [session, Advance()];
        }

        return ParseQualifiedName();
    }

    private AlterSystemStatement ParseAlterSystem()
    {
        var alterKeyword = Advance();
        var systemKeyword = Advance();
        Query action = _current.Kind == SyntaxKind.SetKeyword ? ParseSetConfiguration() : ParseReset();
        return new AlterSystemStatement
        {
            Span = SourceSpan.From(alterKeyword, action.Span),
            AlterKeyword = alterKeyword,
            SystemKeyword = systemKeyword,
            Action = action,
        };
    }

    // SET … or RESET … tail shared by ALTER ROLE and ALTER DATABASE; null when neither starts here.
    internal Query? TryParseSettingAction()
    {
        if (_current.Kind == SyntaxKind.SetKeyword)
        {
            return ParseSetConfiguration();
        }

        return IdentifierEquals(Keyword.UtilReset) ? ParseReset() : null;
    }
}
