namespace QlParse;

// Workstream E: procedural T-SQL, modules, cursors, triggers, transactions and session statements.
// Every statement here starts with a word that stays an ordinary identifier (or SET / FETCH), so the hook
// dispatches on the word's length and text and returns null for anything it does not own; the ISO
// dispatch, or a later workstream's hook, then gets the statement. Bodies (BEGIN ... END, TRY/CATCH,
// procedure and trigger bodies) are parsed eagerly with ParseTsqlStatementList.
internal sealed partial class Parser
{
    internal Query? TryParseTsqlProcStatement() => _current.Kind switch
    {
        SyntaxKind.Identifier => TryParseTsqlProcWord(),
        SyntaxKind.SetKeyword => TryParseTsqlSet(),
        SyntaxKind.FetchKeyword => ParseTsqlFetch(),
        _ => null,
    };

    // UPDATE(column) inside a trigger is a function call on the UPDATE keyword.
    internal FunctionCallExpression? TryParseTsqlProcPrefix() =>
        _current.Kind == SyntaxKind.UpdateKeyword && NextKind == SyntaxKind.OpenParen ? ParseTsqlUpdateFunction() : null;

    private Query? TryParseTsqlProcWord()
    {
        if (NextKind == SyntaxKind.ColonToken)
        {
            return ParseTsqlLabel();
        }

        var text = _current.TextOf(_source);
        return text.Length switch
        {
            Len2 => TextEquals(text, Keyword.If) ? ParseTsqlIf() : null,
            Len3 => TextEquals(text, Keyword.ProcUse) ? ParseTsqlUse() : null,
            Len4 => TryParseTsqlProcLen4(text),
            Len5 => TryParseTsqlProcLen5(text),
            Len6 => TryParseTsqlProcLen6(text),
            Len7 => TryParseTsqlProcLen7(text),
            Len8 => TryParseTsqlProcLen8(text),
            Len9 => TextEquals(text, Keyword.ProcRaiserror) ? ParseTsqlRaiserror() : null,
            Len10 => TextEquals(text, Keyword.Deallocate) ? ParseTsqlCursorStatement() : null,
            _ => null,
        };
    }

    private Query? TryParseTsqlProcLen4(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Exec))
        {
            return TryParseTsqlExecute();
        }

        if (TextEquals(text, Keyword.Goto))
        {
            return ParseTsqlGoto();
        }

        if (TextEquals(text, Keyword.Open))
        {
            return IsTsqlCursorVerb() ? ParseTsqlCursorStatement() : null;
        }

        return TextEquals(text, Keyword.ProcSave) ? ParseTsqlTransaction() : null;
    }

    private Query? TryParseTsqlProcLen5(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Begin))
        {
            return TryParseTsqlBegin();
        }

        if (TextEquals(text, Keyword.While))
        {
            return ParseTsqlWhile();
        }

        if (TextEquals(text, Keyword.ProcPrint))
        {
            return ParseTsqlPrint();
        }

        if (TextEquals(text, Keyword.TsqlThrow))
        {
            return ParseTsqlThrow();
        }

        return TryParseTsqlProcLen5Tail(text);
    }

    private Query? TryParseTsqlProcLen5Tail(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.ProcBreak))
        {
            var breakKeyword = Advance();
            return new TsqlBreakStatement { Span = breakKeyword.Span, BreakKeyword = breakKeyword };
        }

        if (TextEquals(text, Keyword.Close))
        {
            return IsTsqlCursorVerb() ? ParseTsqlCursorStatement() : null;
        }

        return TextEquals(text, Keyword.Alter) ? TryParseTsqlAlterModule() : null;
    }

    private Query? TryParseTsqlProcLen6(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Return))
        {
            return ParseTsqlReturn();
        }

        if (TextEquals(text, Keyword.Create))
        {
            return TryParseTsqlCreateModule();
        }

        if (TextEquals(text, Keyword.Commit))
        {
            return ParseTsqlTransaction();
        }

        return TextEquals(text, Keyword.ProcEnable) && NextEquals(Keyword.Trigger) ? ParseTsqlToggleTrigger() : null;
    }

    private Query? TryParseTsqlProcLen7(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Declare))
        {
            return TryParseTsqlDeclare();
        }

        if (TextEquals(text, Keyword.Execute))
        {
            return TryParseTsqlExecute();
        }

        if (TextEquals(text, Keyword.ProcWaitfor))
        {
            return ParseTsqlWaitfor();
        }

        return TextEquals(text, Keyword.ProcDisable) && NextEquals(Keyword.Trigger) ? ParseTsqlToggleTrigger() : null;
    }

    private Query? TryParseTsqlProcLen8(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Continue))
        {
            var continueKeyword = Advance();
            return new TsqlContinueStatement { Span = continueKeyword.Span, ContinueKeyword = continueKeyword };
        }

        return TextEquals(text, Keyword.Rollback) ? ParseTsqlTransaction() : null;
    }

    // OPEN / CLOSE of a cursor, not OPEN|CLOSE {MASTER|SYMMETRIC} KEY or CLOSE ALL SYMMETRIC KEYS (workstream F).
    private bool IsTsqlCursorVerb() =>
        NextKind is SyntaxKind.Identifier or SyntaxKind.EmbeddedHost && !IsKeywordAt(_index + 1, Keyword.Key);

    // True when the current token can begin an optional trailing expression (RETURN value, THROW and
    // EXEC arguments) rather than the next statement.
    internal bool IsTsqlExpressionStart() => _current.Kind switch
    {
        SyntaxKind.Identifier => !IsTsqlStatementWord(),
        SyntaxKind.EndOfFile or SyntaxKind.Semicolon or SyntaxKind.BatchSeparator or SyntaxKind.CloseParen
            or SyntaxKind.Comma or SyntaxKind.SelectKeyword or SyntaxKind.WithKeyword or SyntaxKind.SetKeyword
            or SyntaxKind.UpdateKeyword or SyntaxKind.FetchKeyword or SyntaxKind.EndKeyword or SyntaxKind.ElseKeyword
            => false,
        _ => true,
    };

    // True when the current token can be an optional trailing name (BEGIN TRAN name, COMMIT TRAN @name).
    internal bool IsTsqlOptionalName() =>
        _current.Kind == SyntaxKind.EmbeddedHost
        || (_current.Kind == SyntaxKind.Identifier && !IsTsqlStatementWord());

    // A reserved word (other than a function name before `(`), an unreserved statement verb, or a label.
    private bool IsTsqlStatementWord()
    {
        var text = _current.TextOf(_source);
        if (Keyword.IsTsqlReserved(text))
        {
            return NextKind != SyntaxKind.OpenParen;
        }

        return Keyword.IsTsqlUnreservedVerb(text) || NextKind == SyntaxKind.ColonToken;
    }

    // An identifier or @variable naming a cursor, transaction or similar object.
    internal SyntaxToken ExpectTsqlName()
    {
        if (_current.Kind is not (SyntaxKind.Identifier or SyntaxKind.EmbeddedHost))
        {
            throw new SqlParseException($"Expected name, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    // A one-statement body (IF / ELSE / WHILE).
    internal Query ParseTsqlEmbeddedStatement()
    {
        if (!IsTsqlStatementStart())
        {
            throw new SqlParseException($"Expected statement, found {_current.Kind}", _current.Position);
        }

        return ParseStatement();
    }

    // A statement list that must not be empty (procedure and trigger bodies).
    internal IReadOnlyList<Query> ParseTsqlBody()
    {
        var statements = ParseTsqlStatementList().Statements;
        if (statements.Count == 0)
        {
            throw new SqlParseException($"Expected statement, found {_current.Kind}", _current.Position);
        }

        return statements;
    }

    internal SyntaxToken? AcceptTsqlWord(string keyword) => IdentifierEquals(keyword) ? Advance() : null;

    // The token before the current one: the end of a construct whose last part is optional.
    internal SyntaxToken TsqlPreviousToken() => _tokens[_index - Len2];

    private FunctionCallExpression ParseTsqlUpdateFunction()
    {
        var updateKeyword = Advance();
        var openParen = Advance();
        var column = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new FunctionCallExpression
        {
            Span = SourceSpan.From(updateKeyword, closeParen),
            Name = updateKeyword,
            OpenParen = openParen,
            Arguments = [column],
            CloseParen = closeParen,
        };
    }
}
