namespace QlParse;

// T-SQL control flow: BEGIN ... END, BEGIN ATOMIC, TRY/CATCH, IF/ELSE, WHILE, GOTO and labels, RETURN,
// WAITFOR, THROW, RAISERROR and PRINT. IF, ELSE and WHILE bodies are one statement each; blocks reuse the
// batch statement list, which stops at END and leaves it to the block.
internal sealed partial class Parser
{
    // BEGIN starts a block, TRY/CATCH, a transaction or an atomic block; BEGIN DIALOG and BEGIN CONVERSATION
    // TIMER belong to Service Broker (workstream G).
    private Query? TryParseTsqlBegin()
    {
        if (NextEquals(Keyword.ProcTry))
        {
            return ParseTsqlTryCatch();
        }

        if (NextEquals(Keyword.ProcTran) || NextEquals(Keyword.Transaction) || NextEquals(Keyword.ProcDistributed))
        {
            return ParseTsqlTransaction();
        }

        if (NextEquals(Keyword.Atomic))
        {
            return ParseTsqlAtomicBlock();
        }

        return IsTsqlBrokerBegin() ? null : ParseTsqlBlock();
    }

    internal TsqlBlockStatement ParseTsqlBlock()
    {
        var beginKeyword = Advance();
        var statements = ParseTsqlStatementList().Statements;
        var endKeyword = Expect(SyntaxKind.EndKeyword);
        return new TsqlBlockStatement
        {
            Span = SourceSpan.From(beginKeyword, endKeyword),
            BeginKeyword = beginKeyword,
            Statements = statements,
            EndKeyword = endKeyword,
        };
    }

    // BEGIN ATOMIC WITH (option = value, ...) statements END
    internal TsqlAtomicBlockStatement ParseTsqlAtomicBlock()
    {
        var beginKeyword = Advance();
        var atomicKeyword = Advance();
        var withKeyword = Expect(SyntaxKind.WithKeyword);
        var openParen = Expect(SyntaxKind.OpenParen);
        var options = new List<TsqlOption> { ParseTsqlAssignedOption() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            options.Add(ParseTsqlAssignedOption());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        var statements = ParseTsqlStatementList().Statements;
        var endKeyword = Expect(SyntaxKind.EndKeyword);
        return new TsqlAtomicBlockStatement
        {
            Span = SourceSpan.From(beginKeyword, endKeyword),
            BeginKeyword = beginKeyword,
            AtomicKeyword = atomicKeyword,
            WithKeyword = withKeyword,
            OpenParen = openParen,
            Options = options,
            CloseParen = closeParen,
            Statements = statements,
            EndKeyword = endKeyword,
        };
    }

    // `words = value words` inside a parenthesized option list (TRANSACTION ISOLATION LEVEL = SNAPSHOT,
    // LANGUAGE = N'us_english', DELAYED_DURABILITY = ON).
    internal TsqlOption ParseTsqlAssignedOption()
    {
        var name = new List<SyntaxToken> { Advance() };
        while (_current.Kind is not (SyntaxKind.EqualsToken or SyntaxKind.Comma or SyntaxKind.CloseParen or SyntaxKind.EndOfFile))
        {
            name.Add(Advance());
        }

        var equalsToken = Expect(SyntaxKind.EqualsToken);
        var value = new List<SyntaxToken> { Advance() };
        while (_current.Kind is not (SyntaxKind.Comma or SyntaxKind.CloseParen or SyntaxKind.EndOfFile))
        {
            value.Add(Advance());
        }

        return new TsqlOption
        {
            Span = SourceSpan.From(name[0], value[^1]),
            Name = name,
            EqualsToken = equalsToken,
            Value = value,
        };
    }

    private TsqlTryCatchStatement ParseTsqlTryCatch()
    {
        var tryBegin = Advance();
        var tryKeyword = Advance();
        var tryStatements = ParseTsqlStatementList().Statements;
        var tryEnd = Expect(SyntaxKind.EndKeyword);
        var tryEndTry = ExpectIdent(Keyword.ProcTry);
        var catchBegin = ExpectIdent(Keyword.Begin);
        var catchKeyword = ExpectIdent(Keyword.ProcCatch);
        var catchStatements = ParseTsqlStatementList().Statements;
        var catchEnd = Expect(SyntaxKind.EndKeyword);
        var catchEndCatch = ExpectIdent(Keyword.ProcCatch);
        return new TsqlTryCatchStatement
        {
            Span = SourceSpan.From(tryBegin, catchEndCatch),
            TryBeginKeyword = tryBegin,
            TryKeyword = tryKeyword,
            TryStatements = tryStatements,
            TryEndKeyword = tryEnd,
            TryEndTryKeyword = tryEndTry,
            CatchBeginKeyword = catchBegin,
            CatchKeyword = catchKeyword,
            CatchStatements = catchStatements,
            CatchEndKeyword = catchEnd,
            CatchEndCatchKeyword = catchEndCatch,
        };
    }

    private TsqlIfStatement ParseTsqlIf()
    {
        var ifKeyword = Advance();
        var condition = ParseExpression();
        var then = ParseTsqlEmbeddedStatement();
        SkipTsqlTerminatorsBeforeElse();
        if (_current.Kind != SyntaxKind.ElseKeyword)
        {
            return new TsqlIfStatement
            {
                Span = SourceSpan.From(ifKeyword.Span, then.Span),
                IfKeyword = ifKeyword,
                Condition = condition,
                Then = then,
            };
        }

        var elseKeyword = Advance();
        var otherwise = ParseTsqlEmbeddedStatement();
        return new TsqlIfStatement
        {
            Span = SourceSpan.From(ifKeyword.Span, otherwise.Span),
            IfKeyword = ifKeyword,
            Condition = condition,
            Then = then,
            ElseKeyword = elseKeyword,
            Else = otherwise,
        };
    }

    // `IF c stmt; ELSE stmt` is legal: terminators are skipped only when ELSE follows them.
    private void SkipTsqlTerminatorsBeforeElse()
    {
        var index = _index - 1;
        while (index < _tokens.Count && _tokens[index].Kind == SyntaxKind.Semicolon)
        {
            index++;
        }

        if (index < _tokens.Count && _tokens[index].Kind == SyntaxKind.ElseKeyword)
        {
            SkipTsqlTerminators();
        }
    }

    private TsqlWhileStatement ParseTsqlWhile()
    {
        var whileKeyword = Advance();
        var condition = ParseExpression();
        var body = ParseTsqlEmbeddedStatement();
        return new TsqlWhileStatement
        {
            Span = SourceSpan.From(whileKeyword.Span, body.Span),
            WhileKeyword = whileKeyword,
            Condition = condition,
            Body = body,
        };
    }

    private TsqlGotoStatement ParseTsqlGoto()
    {
        var gotoKeyword = Advance();
        var label = Expect(SyntaxKind.Identifier);
        return new TsqlGotoStatement { Span = SourceSpan.From(gotoKeyword, label), GotoKeyword = gotoKeyword, Label = label };
    }

    private TsqlLabelStatement ParseTsqlLabel()
    {
        var label = Advance();
        var colon = Advance();
        return new TsqlLabelStatement { Span = SourceSpan.From(label, colon), Label = label, Colon = colon };
    }

    private TsqlReturnStatement ParseTsqlReturn()
    {
        var returnKeyword = Advance();
        if (!IsTsqlExpressionStart())
        {
            return new TsqlReturnStatement { Span = returnKeyword.Span, ReturnKeyword = returnKeyword };
        }

        var value = ParseExpression();
        return new TsqlReturnStatement
        {
            Span = SourceSpan.From(returnKeyword.Span, value.Span),
            ReturnKeyword = returnKeyword,
            Value = value,
        };
    }

    // The body of an inline table-valued function: RETURN [(] select [)].
    internal TsqlReturnStatement ParseTsqlInlineReturn()
    {
        var returnKeyword = Advance();
        var query = ParseQuery();
        return new TsqlReturnStatement
        {
            Span = SourceSpan.From(returnKeyword.Span, query.Span),
            ReturnKeyword = returnKeyword,
            Query = query,
        };
    }

    // WAITFOR {DELAY | TIME} value | WAITFOR (statement) [, TIMEOUT value]
    private TsqlWaitforStatement ParseTsqlWaitfor()
    {
        var waitforKeyword = Advance();
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            var delayOrTime = _current.Kind == SyntaxKind.TimeKeyword ? Advance() : ExpectIdent(Keyword.ProcDelay);
            var value = ParseExpression();
            return new TsqlWaitforStatement
            {
                Span = SourceSpan.From(waitforKeyword.Span, value.Span),
                WaitforKeyword = waitforKeyword,
                DelayOrTimeKeyword = delayOrTime,
                Value = value,
            };
        }

        var openParen = Advance();
        var statement = ParseTsqlWaitforStatement();
        var closeParen = Expect(SyntaxKind.CloseParen);
        if (_current.Kind != SyntaxKind.Comma)
        {
            return new TsqlWaitforStatement
            {
                Span = SourceSpan.From(waitforKeyword, closeParen),
                WaitforKeyword = waitforKeyword,
                OpenParen = openParen,
                Statement = statement,
                CloseParen = closeParen,
            };
        }

        var comma = Advance();
        var timeoutKeyword = ExpectIdent(Keyword.ProcTimeout);
        var timeout = ParseExpression();
        return new TsqlWaitforStatement
        {
            Span = SourceSpan.From(waitforKeyword.Span, timeout.Span),
            WaitforKeyword = waitforKeyword,
            OpenParen = openParen,
            Statement = statement,
            CloseParen = closeParen,
            Comma = comma,
            TimeoutKeyword = timeoutKeyword,
            Timeout = timeout,
        };
    }

    // RECEIVE ... or GET CONVERSATION GROUP ..., parsed by the Service Broker statements (workstream G).
    private Query ParseTsqlWaitforStatement()
    {
        if (IdentifierEquals(Keyword.AdminReceive))
        {
            return ParseTsqlReceive();
        }

        if (IdentifierEquals(Keyword.Get) && NextEquals(Keyword.AdminConversation))
        {
            return ParseTsqlGetConversationGroup();
        }

        throw new SqlParseException($"Expected RECEIVE or GET CONVERSATION GROUP, found {_current.Kind}", _current.Position);
    }

    // THROW [error_number, message, state]
    private TsqlThrowStatement ParseTsqlThrow()
    {
        var throwKeyword = Advance();
        if (!IsTsqlExpressionStart())
        {
            return new TsqlThrowStatement { Span = throwKeyword.Span, ThrowKeyword = throwKeyword };
        }

        var number = ParseExpression();
        Expect(SyntaxKind.Comma);
        var message = ParseExpression();
        Expect(SyntaxKind.Comma);
        var state = ParseExpression();
        return new TsqlThrowStatement
        {
            Span = SourceSpan.From(throwKeyword.Span, state.Span),
            ThrowKeyword = throwKeyword,
            Arguments = [number, message, state],
        };
    }

    // RAISERROR (msg, severity, state [, argument ...]) [WITH option, ...], or legacy RAISERROR number 'message'.
    private TsqlRaiserrorStatement ParseTsqlRaiserror()
    {
        var raiserrorKeyword = Advance();
        SyntaxToken? openParen = null;
        SyntaxToken? closeParen = null;
        IReadOnlyList<Expression> arguments;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            openParen = Advance();
            arguments = ParseExpressionList();
            closeParen = Expect(SyntaxKind.CloseParen);
        }
        else
        {
            var number = ParseExpression();
            arguments = [number, ParseExpression()];
        }

        SyntaxToken? withKeyword = null;
        IReadOnlyList<SyntaxToken> options = [];
        if (_current.Kind == SyntaxKind.WithKeyword)
        {
            withKeyword = Advance();
            options = ParseTsqlWordList();
        }

        return new TsqlRaiserrorStatement
        {
            Span = SourceSpan.From(raiserrorKeyword, options.Count > 0 ? options[^1] : closeParen ?? TsqlPreviousToken()),
            RaiserrorKeyword = raiserrorKeyword,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // word [, word ...]
    internal List<SyntaxToken> ParseTsqlWordList()
    {
        var words = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            words.Add(Expect(SyntaxKind.Identifier));
        }

        return words;
    }

    private TsqlPrintStatement ParseTsqlPrint()
    {
        var printKeyword = Advance();
        var value = ParseExpression();
        return new TsqlPrintStatement
        {
            Span = SourceSpan.From(printKeyword.Span, value.Span),
            PrintKeyword = printKeyword,
            Value = value,
        };
    }
}
