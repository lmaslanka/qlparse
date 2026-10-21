namespace QlParse;

// PL/pgSQL commands: RETURN [NEXT | QUERY], RAISE, ASSERT, PERFORM, EXECUTE, GET DIAGNOSTICS and the
// cursor commands OPEN, FETCH and MOVE.
internal sealed partial class Parser
{
    // RETURN [expr] | RETURN NEXT [expr] | RETURN QUERY query | RETURN QUERY EXECUTE command [USING ...]
    private PlpgsqlReturnStatement ParsePlpgsqlReturn()
    {
        var returnKeyword = Advance();
        if (IdentifierEquals(Keyword.PlpgsqlQuery))
        {
            var queryKeyword = Advance();
            var query = IdentifierEquals(Keyword.Execute) ? ParsePlpgsqlExecute() : ParseQuery();
            return new PlpgsqlReturnStatement
            {
                Span = SourceSpan.From(returnKeyword, query.Span),
                ReturnKeyword = returnKeyword,
                Modifier = queryKeyword,
                Query = query,
            };
        }

        SyntaxToken? nextKeyword = IdentifierEquals(Keyword.Next) ? Advance() : null;
        var value = _current.Kind == SyntaxKind.Semicolon ? null : ParseExpression();
        return new PlpgsqlReturnStatement
        {
            Span = SourceSpan.From(returnKeyword, PgPreviousToken()),
            ReturnKeyword = returnKeyword,
            Modifier = nextKeyword,
            Value = value,
        };
    }

    // RAISE [level] 'format' [, expr ...] [USING option = expr, ...]
    // RAISE [level] {condition | SQLSTATE 'code'} [USING ...] | RAISE [level] USING ... | RAISE
    private PlpgsqlRaiseStatement ParsePlpgsqlRaise()
    {
        var raiseKeyword = Advance();
        SyntaxToken? level = IsPlpgsqlRaiseLevel() ? Advance() : null;
        SyntaxToken? format = _current.Kind == SyntaxKind.String ? Advance() : null;
        IReadOnlyList<Expression> arguments = format is not null && _current.Kind == SyntaxKind.Comma ? ParsePlpgsqlRaiseArguments() : [];
        var condition = format is null ? ParsePlpgsqlRaiseCondition() : default;
        SyntaxToken? usingKeyword = _current.Kind == SyntaxKind.UsingKeyword ? Advance() : null;
        IReadOnlyList<PlpgsqlRaiseOption> options = usingKeyword is null ? [] : ParsePlpgsqlRaiseOptions();
        return new PlpgsqlRaiseStatement
        {
            Span = SourceSpan.From(raiseKeyword, PgPreviousToken()),
            RaiseKeyword = raiseKeyword,
            Level = level,
            Format = format,
            Arguments = arguments,
            SqlStateKeyword = condition.SqlStateKeyword,
            Condition = condition.Condition,
            UsingKeyword = usingKeyword,
            Options = options,
        };
    }

    private readonly record struct PlpgsqlRaiseCondition(SyntaxToken? SqlStateKeyword, SyntaxToken? Condition);

    // SQLSTATE 'code' or a condition name; neither when the statement goes straight to USING or ends.
    private PlpgsqlRaiseCondition ParsePlpgsqlRaiseCondition()
    {
        if (IdentifierEquals(Keyword.SqlState))
        {
            var sqlState = Advance();
            return new PlpgsqlRaiseCondition(sqlState, Expect(SyntaxKind.String));
        }

        return _current.Kind == SyntaxKind.Identifier ? new PlpgsqlRaiseCondition(null, Advance()) : default;
    }

    private bool IsPlpgsqlRaiseLevel() =>
        IdentifierEquals(Keyword.Exception) || IdentifierEquals(Keyword.PlpgsqlNotice) || IdentifierEquals(Keyword.PlpgsqlWarning)
        || IdentifierEquals(Keyword.PlpgsqlInfo) || IdentifierEquals(Keyword.PlpgsqlLog) || IdentifierEquals(Keyword.PlpgsqlDebug);

    private List<Expression> ParsePlpgsqlRaiseArguments()
    {
        var arguments = new List<Expression>();
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            arguments.Add(ParseExpression());
        }

        return arguments;
    }

    private List<PlpgsqlRaiseOption> ParsePlpgsqlRaiseOptions()
    {
        var options = new List<PlpgsqlRaiseOption>();
        do
        {
            if (options.Count > 0)
            {
                Advance();
            }

            var name = ExpectPgWord();
            var assignment = _current.Kind is SyntaxKind.EqualsToken or SyntaxKind.ColonEqualsToken
                ? Advance()
                : throw new SqlParseException($"Expected = or :=, found {_current.Kind}", _current.Position);
            var value = ParseExpression();
            options.Add(new PlpgsqlRaiseOption
            {
                Span = SourceSpan.From(name, value.Span),
                Name = name,
                AssignmentToken = assignment,
                Value = value,
            });
        }
        while (_current.Kind == SyntaxKind.Comma);

        return options;
    }

    // ASSERT condition [, message]
    private PlpgsqlAssertStatement ParsePlpgsqlAssert()
    {
        var assertKeyword = Advance();
        var condition = ParseExpression();
        Expression? message = null;
        if (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            message = ParseExpression();
        }

        return new PlpgsqlAssertStatement
        {
            Span = SourceSpan.From(assertKeyword, PgPreviousToken()),
            AssertKeyword = assertKeyword,
            Condition = condition,
            Message = message,
        };
    }

    // PERFORM query: a SELECT whose SELECT keyword is replaced by PERFORM. The PERFORM token is presented
    // to the query parser as a SELECT keyword for the duration of the call, then restored, so the
    // SelectStatement's SelectKeyword spans the PERFORM word.
    private PlpgsqlPerformStatement ParsePlpgsqlPerform()
    {
        var performKeyword = _current;
        var tokens = (List<SyntaxToken>)_tokens;
        var at = _index - 1;
        var select = performKeyword with { Kind = SyntaxKind.SelectKeyword };
        tokens[at] = select;
        _current = select;
        var query = ParseQuery();
        tokens[at] = performKeyword;
        return new PlpgsqlPerformStatement
        {
            Span = SourceSpan.From(performKeyword, query.Span),
            PerformKeyword = performKeyword,
            Query = query,
        };
    }

    // EXECUTE command [INTO [STRICT] targets] [USING expr, ...] (INTO and USING in either order)
    private PlpgsqlExecuteStatement ParsePlpgsqlExecute()
    {
        var executeKeyword = Advance();
        var command = ParseExpression();
        var into = TryParsePlpgsqlInto();
        SyntaxToken? usingKeyword = _current.Kind == SyntaxKind.UsingKeyword ? Advance() : null;
        IReadOnlyList<Expression> arguments = usingKeyword is null ? [] : ParseExpressionList();
        if (into.IntoKeyword is null)
        {
            into = TryParsePlpgsqlInto();
        }

        return new PlpgsqlExecuteStatement
        {
            Span = SourceSpan.From(executeKeyword, PgPreviousToken()),
            ExecuteKeyword = executeKeyword,
            Command = command,
            IntoKeyword = into.IntoKeyword,
            StrictKeyword = into.StrictKeyword,
            Targets = into.Targets ?? [],
            UsingKeyword = usingKeyword,
            Arguments = arguments,
        };
    }

    private readonly record struct PlpgsqlInto(SyntaxToken? IntoKeyword, SyntaxToken? StrictKeyword, IReadOnlyList<Expression>? Targets);

    private PlpgsqlInto TryParsePlpgsqlInto()
    {
        if (!IdentifierEquals(Keyword.Into))
        {
            return default;
        }

        var intoKeyword = Advance();
        SyntaxToken? strictKeyword = IdentifierEquals(Keyword.RoutineStrict) ? Advance() : null;
        return new PlpgsqlInto(intoKeyword, strictKeyword, ParsePlpgsqlTargets());
    }

    // GET [CURRENT | STACKED] DIAGNOSTICS target {= | :=} item [, ...]
    private GetDiagnosticsStatement ParsePlpgsqlGetDiagnostics()
    {
        var getKeyword = Advance();
        SyntaxToken? scope = IdentifierEquals(Keyword.Current) || IdentifierEquals(Keyword.PlpgsqlStacked) ? Advance() : null;
        var diagnosticsKeyword = ExpectIdent(Keyword.Diagnostics);
        var items = new List<DiagnosticsItem>();
        do
        {
            if (items.Count > 0)
            {
                Advance();
            }

            var target = Expect(SyntaxKind.Identifier);
            var assignment = _current.Kind is SyntaxKind.EqualsToken or SyntaxKind.ColonEqualsToken
                ? Advance()
                : throw new SqlParseException($"Expected = or :=, found {_current.Kind}", _current.Position);
            var name = ExpectPgWord();
            items.Add(new DiagnosticsItem { Span = SourceSpan.From(target, name), Target = target, EqualsToken = assignment, Name = name });
        }
        while (_current.Kind == SyntaxKind.Comma);

        return new GetDiagnosticsStatement
        {
            Span = SourceSpan.From(getKeyword, items[^1].Span),
            GetKeyword = getKeyword,
            ScopeKeyword = scope,
            DiagnosticsKeyword = diagnosticsKeyword,
            Items = items,
        };
    }
}
