namespace QlParse;

// T-SQL variables and cursors: DECLARE @v type [= value] (including TABLE and CURSOR variables),
// SET @v [op]= value, T-SQL cursor declarations, SET @c = CURSOR ..., OPEN / FETCH / CLOSE / DEALLOCATE.
internal sealed partial class Parser
{
    // DECLARE @... is always T-SQL; DECLARE name CURSOR is T-SQL only with options between CURSOR and FOR
    // (the ISO form stays an ISO DeclareCursorStatement).
    private Query? TryParseTsqlDeclare()
    {
        if (NextKind == SyntaxKind.EmbeddedHost)
        {
            return ParseTsqlDeclareVariables();
        }

        var cursorIndex = _index + 1;
        if (NextKind == SyntaxKind.Identifier
            && IsKeywordAt(cursorIndex, Keyword.Cursor)
            && cursorIndex + 1 < _tokens.Count
            && _tokens[cursorIndex + 1].Kind == SyntaxKind.Identifier
            && Keyword.IsTsqlCursorOption(_tokens[cursorIndex + 1].TextOf(_source)))
        {
            return ParseTsqlDeclareCursor();
        }

        return null;
    }

    private TsqlDeclareStatement ParseTsqlDeclareVariables()
    {
        var declareKeyword = Advance();
        var variables = new List<TsqlVariableDeclaration> { ParseTsqlVariableDeclaration() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            variables.Add(ParseTsqlVariableDeclaration());
        }

        return new TsqlDeclareStatement
        {
            Span = SourceSpan.From(declareKeyword.Span, variables[^1].Span),
            DeclareKeyword = declareKeyword,
            Variables = variables,
        };
    }

    // @name [AS] {type [= value] | CURSOR | TABLE (...)}
    private TsqlVariableDeclaration ParseTsqlVariableDeclaration()
    {
        var name = Expect(SyntaxKind.EmbeddedHost);
        var asKeyword = _current.Kind == SyntaxKind.AsKeyword ? Advance() : (SyntaxToken?)null;
        if (IdentifierEquals(Keyword.Cursor))
        {
            var cursorKeyword = Advance();
            return new TsqlVariableDeclaration
            {
                Span = SourceSpan.From(name, cursorKeyword),
                Name = name,
                AsKeyword = asKeyword,
                CursorKeyword = cursorKeyword,
            };
        }

        if (IdentifierEquals(Keyword.Table) && NextKind == SyntaxKind.OpenParen)
        {
            var table = ParseTsqlTableDefinition();
            return new TsqlVariableDeclaration { Span = SourceSpan.From(name, table.Span), Name = name, AsKeyword = asKeyword, Table = table };
        }

        var dataType = ParseDataType();
        if (_current.Kind != SyntaxKind.EqualsToken)
        {
            return new TsqlVariableDeclaration { Span = SourceSpan.From(name, dataType.Span), Name = name, AsKeyword = asKeyword, DataType = dataType };
        }

        var equalsToken = Advance();
        var value = ParseExpression();
        return new TsqlVariableDeclaration
        {
            Span = SourceSpan.From(name, value.Span),
            Name = name,
            AsKeyword = asKeyword,
            DataType = dataType,
            EqualsToken = equalsToken,
            Value = value,
        };
    }

    // TABLE (column and constraint definitions)
    internal TsqlTableDefinition ParseTsqlTableDefinition()
    {
        var tableKeyword = ExpectIdent(Keyword.Table);
        ParseTableElements(out var openParen, out var elements, out var closeParen);
        return new TsqlTableDefinition
        {
            Span = SourceSpan.From(tableKeyword, closeParen),
            TableKeyword = tableKeyword,
            OpenParen = openParen,
            Elements = elements,
            CloseParen = closeParen,
        };
    }

    // The first part of a table name when it is not an identifier: a T-SQL table variable (`FROM @t`,
    // `INSERT INTO @t`); anything else is the usual "Expected Identifier" error.
    internal SyntaxToken ExpectTsqlTableVariable() =>
        _current.Kind == SyntaxKind.EmbeddedHost && IsSqlServer ? Advance() : Expect(SyntaxKind.Identifier);

    private TsqlDeclareCursorStatement ParseTsqlDeclareCursor()
    {
        var declareKeyword = Advance();
        var name = Advance();
        var cursor = ParseTsqlCursorDefinition();
        return new TsqlDeclareCursorStatement
        {
            Span = SourceSpan.From(declareKeyword.Span, cursor.Span),
            DeclareKeyword = declareKeyword,
            Name = name,
            Cursor = cursor,
        };
    }

    // CURSOR [option ...] FOR select
    private TsqlCursorDefinition ParseTsqlCursorDefinition()
    {
        var cursorKeyword = Advance();
        var options = new List<SyntaxToken>();
        while (_current.Kind == SyntaxKind.Identifier && Keyword.IsTsqlCursorOption(_current.TextOf(_source)))
        {
            options.Add(Advance());
        }

        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var query = ParseQuery();
        return new TsqlCursorDefinition
        {
            Span = SourceSpan.From(cursorKeyword.Span, query.Span),
            CursorKeyword = cursorKeyword,
            Options = options,
            ForKeyword = forKeyword,
            Query = query,
        };
    }

    // SET @v ..., SET TRANSACTION ISOLATION LEVEL, SET IDENTITY_INSERT, SET STATISTICS and the session options.
    // Every other SET is left to the ISO parser.
    private Query? TryParseTsqlSet()
    {
        var next = NextKind;
        if (next == SyntaxKind.EmbeddedHost)
        {
            return ParseTsqlSetVariable();
        }

        if (next != SyntaxKind.Identifier)
        {
            return null;
        }

        var text = _tokens[_index].TextOf(_source);
        if (TextEquals(text, Keyword.Transaction))
        {
            return IsKeywordAt(_index + 1, Keyword.Isolation) ? ParseTsqlSetIsolationLevel() : null;
        }

        if (TextEquals(text, Keyword.ProcIdentityInsert))
        {
            return ParseTsqlSetIdentityInsert();
        }

        return TextEquals(text, Keyword.ProcStatistics) || Keyword.IsTsqlSessionOption(text) ? ParseTsqlSetOption() : null;
    }

    private TsqlSetVariableStatement ParseTsqlSetVariable()
    {
        var setKeyword = Advance();
        var variable = Advance();
        if (_current.Kind is not (SyntaxKind.EqualsToken or SyntaxKind.CompoundAssignmentToken))
        {
            throw new SqlParseException($"Expected = or a compound assignment, found {_current.Kind}", _current.Position);
        }

        var operatorToken = Advance();
        if (operatorToken.Kind == SyntaxKind.EqualsToken && IsTsqlCursorValue())
        {
            var cursor = ParseTsqlCursorDefinition();
            return new TsqlSetVariableStatement
            {
                Span = SourceSpan.From(setKeyword.Span, cursor.Span),
                SetKeyword = setKeyword,
                Variable = variable,
                OperatorToken = operatorToken,
                Cursor = cursor,
            };
        }

        var value = ParseExpression();
        return new TsqlSetVariableStatement
        {
            Span = SourceSpan.From(setKeyword.Span, value.Span),
            SetKeyword = setKeyword,
            Variable = variable,
            OperatorToken = operatorToken,
            Value = value,
        };
    }

    private bool IsTsqlCursorValue()
    {
        if (!IdentifierEquals(Keyword.Cursor))
        {
            return false;
        }

        var next = _index < _tokens.Count ? _tokens[_index] : default;
        return next.Kind == SyntaxKind.ForKeyword
            || (next.Kind == SyntaxKind.Identifier && Keyword.IsTsqlCursorOption(next.TextOf(_source)));
    }

    // SET option[, option ...] {ON | OFF | value}, and SET STATISTICS {IO | TIME | XML | PROFILE}[, ...] {ON | OFF}
    private TsqlSetOptionStatement ParseTsqlSetOption()
    {
        var setKeyword = Advance();
        var statisticsKeyword = AcceptTsqlWord(Keyword.ProcStatistics);
        var options = new List<SyntaxToken> { ExpectTsqlOptionWord() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            options.Add(ExpectTsqlOptionWord());
        }

        if (_current.Kind == SyntaxKind.OnKeyword || IdentifierEquals(Keyword.ProcOff))
        {
            var toggle = Advance();
            return new TsqlSetOptionStatement
            {
                Span = SourceSpan.From(setKeyword, toggle),
                SetKeyword = setKeyword,
                StatisticsKeyword = statisticsKeyword,
                Options = options,
                Toggle = toggle,
            };
        }

        var value = ParseExpression();
        return new TsqlSetOptionStatement
        {
            Span = SourceSpan.From(setKeyword.Span, value.Span),
            SetKeyword = setKeyword,
            StatisticsKeyword = statisticsKeyword,
            Options = options,
            Value = value,
        };
    }

    // An option name; STATISTICS TIME is the only one that is an ISO keyword.
    private SyntaxToken ExpectTsqlOptionWord() =>
        _current.Kind == SyntaxKind.TimeKeyword ? Advance() : Expect(SyntaxKind.Identifier);

    private TsqlSetTransactionIsolationLevelStatement ParseTsqlSetIsolationLevel()
    {
        var setKeyword = Advance();
        var transactionKeyword = Advance();
        var isolationKeyword = Advance();
        var levelKeyword = ExpectIdent(Keyword.Level);
        var level = ParseTsqlIsolationLevel();
        return new TsqlSetTransactionIsolationLevelStatement
        {
            Span = SourceSpan.From(setKeyword, level[^1]),
            SetKeyword = setKeyword,
            TransactionKeyword = transactionKeyword,
            IsolationKeyword = isolationKeyword,
            LevelKeyword = levelKeyword,
            Level = level,
        };
    }

    // READ UNCOMMITTED | READ COMMITTED | REPEATABLE READ | SNAPSHOT | SERIALIZABLE
    private List<SyntaxToken> ParseTsqlIsolationLevel()
    {
        if (_current.Kind == SyntaxKind.ReadKeyword)
        {
            var read = Advance();
            var mode = IdentifierEquals(Keyword.Committed) ? Advance() : ExpectIdent(Keyword.Uncommitted);
            return [read, mode];
        }

        if (IdentifierEquals(Keyword.Repeatable))
        {
            var repeatable = Advance();
            return [repeatable, Expect(SyntaxKind.ReadKeyword)];
        }

        return [IdentifierEquals(Keyword.ProcSnapshot) ? Advance() : ExpectIdent(Keyword.Serializable)];
    }

    private TsqlSetIdentityInsertStatement ParseTsqlSetIdentityInsert()
    {
        var setKeyword = Advance();
        var identityInsertKeyword = Advance();
        var table = ParseQualifiedName();
        var toggle = _current.Kind == SyntaxKind.OnKeyword ? Advance() : ExpectIdent(Keyword.ProcOff);
        return new TsqlSetIdentityInsertStatement
        {
            Span = SourceSpan.From(setKeyword, toggle),
            SetKeyword = setKeyword,
            IdentityInsertKeyword = identityInsertKeyword,
            Table = table,
            Toggle = toggle,
        };
    }

    // OPEN / CLOSE / DEALLOCATE [GLOBAL] cursor
    private TsqlCursorStatement ParseTsqlCursorStatement()
    {
        var verbKeyword = Advance();
        var globalKeyword = IsTsqlGlobalCursor() ? Advance() : (SyntaxToken?)null;
        var cursor = ExpectTsqlName();
        return new TsqlCursorStatement
        {
            Span = SourceSpan.From(verbKeyword, cursor),
            VerbKeyword = verbKeyword,
            GlobalKeyword = globalKeyword,
            Cursor = cursor,
        };
    }

    // GLOBAL followed by the cursor name (a cursor may itself be called global).
    private bool IsTsqlGlobalCursor() => IdentifierEquals(Keyword.Global) && NextKind == SyntaxKind.Identifier;

    // FETCH [[NEXT|PRIOR|FIRST|LAST|ABSOLUTE n|RELATIVE n] FROM] [GLOBAL] cursor [INTO @a, ...]
    private TsqlFetchStatement ParseTsqlFetch()
    {
        var fetchKeyword = Advance();
        SyntaxToken? orientation = null;
        Expression? offset = null;
        if (IsOrientationFrom())
        {
            orientation = Advance();
        }
        else
        {
            if (IdentifierEquals(Keyword.Absolute) || IdentifierEquals(Keyword.Relative))
            {
                orientation = Advance();
                offset = ParseExpression();
            }
        }

        var fromKeyword = orientation is not null ? Expect(SyntaxKind.FromKeyword) : AcceptFrom();
        var globalKeyword = IsTsqlGlobalCursor() ? Advance() : (SyntaxToken?)null;
        var cursor = ExpectTsqlName();
        var intoKeyword = AcceptTsqlWord(Keyword.Into);
        var variables = new List<SyntaxToken>();
        if (intoKeyword is not null)
        {
            variables.Add(Expect(SyntaxKind.EmbeddedHost));
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                variables.Add(Expect(SyntaxKind.EmbeddedHost));
            }
        }

        return new TsqlFetchStatement
        {
            Span = SourceSpan.From(fetchKeyword, variables.Count > 0 ? variables[^1] : cursor),
            FetchKeyword = fetchKeyword,
            Orientation = orientation,
            Offset = offset,
            FromKeyword = fromKeyword,
            GlobalKeyword = globalKeyword,
            Cursor = cursor,
            IntoKeyword = intoKeyword,
            Variables = variables,
        };
    }

    private SyntaxToken? AcceptFrom() => _current.Kind == SyntaxKind.FromKeyword ? Advance() : null;
}
