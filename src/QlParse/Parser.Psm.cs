namespace QlParse;

internal sealed partial class Parser
{
    private CompoundStatement ParseCompound()
    {
        ParseBeginningLabel(out var label, out var colon);
        var beginKeyword = ExpectIdent(Keyword.Begin);
        SyntaxToken? notKeyword = null;
        SyntaxToken? atomic = null;
        if (_current.Kind == SyntaxKind.NotKeyword && NextEquals(Keyword.Atomic))
        {
            notKeyword = Advance();
            atomic = Advance();
        }
        else if (IdentifierEquals(Keyword.Atomic))
        {
            atomic = Advance();
        }

        var statements = ParseStatementList(() => _current.Kind == SyntaxKind.EndKeyword);
        var endKeyword = Expect(SyntaxKind.EndKeyword);
        var endLabel = ParseEndingLabel();
        var end = endLabel ?? endKeyword;
        return new CompoundStatement
        {
            Span = SourceSpan.From(label ?? beginKeyword, end),
            Label = label,
            Colon = colon,
            BeginKeyword = beginKeyword,
            NotKeyword = notKeyword,
            AtomicKeyword = atomic,
            Statements = statements,
            EndKeyword = endKeyword,
            EndLabel = endLabel,
        };
    }

    private Query ParseDeclareVariable()
    {
        var declareKeyword = Advance();
        var names = ParseNameList();
        if (IdentifierEquals(Keyword.Condition))
        {
            if (names.Skip(1).Any())
            {
                throw new SqlParseException($"Expected condition name, found {_current.Kind}", _current.Position);
            }

            var conditionName = names[^1];
            var conditionKeyword = Advance();
            SyntaxToken? forKeyword = null;
            SyntaxToken? sqlState = null;
            SyntaxToken? valueKeyword = null;
            SyntaxToken? state = null;
            var end = conditionKeyword.Span;
            if (_current.Kind == SyntaxKind.ForKeyword)
            {
                forKeyword = Advance();
                sqlState = ExpectIdent(Keyword.SqlState);
                if (IdentifierEquals(Keyword.Value))
                {
                    valueKeyword = Advance();
                }

                var stateToken = ParseSqlStateValue();
                state = stateToken;
                end = stateToken.Span;
            }

            return new DeclareConditionStatement
            {
                Span = SourceSpan.From(declareKeyword, end),
                DeclareKeyword = declareKeyword,
                Name = conditionName,
                ConditionKeyword = conditionKeyword,
                ForKeyword = forKeyword,
                SqlStateKeyword = sqlState,
                ValueKeyword = valueKeyword,
                State = state,
            };
        }

        var type = ParseDataType();
        DefaultClause? defaultClause = null;
        var typeEnd = type.Span;
        if (IdentifierEquals(Keyword.Default))
        {
            defaultClause = ParseDefaultClause();
            typeEnd = defaultClause.Span;
        }

        return new DeclareVariableStatement
        {
            Span = SourceSpan.From(declareKeyword, typeEnd),
            DeclareKeyword = declareKeyword,
            Names = names,
            Type = type,
            Default = defaultClause,
        };
    }

    private DeclareHandlerStatement ParseDeclareHandler()
    {
        var declareKeyword = Advance();
        var handlerType = Advance();
        var handlerKeyword = ExpectIdent(Keyword.Handler);
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var conditions = new List<ConditionValue> { ParseConditionValue() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            conditions.Add(ParseConditionValue());
        }

        var action = ParseStatement();
        return new DeclareHandlerStatement
        {
            Span = SourceSpan.From(declareKeyword, action.Span),
            DeclareKeyword = declareKeyword,
            HandlerType = handlerType,
            HandlerKeyword = handlerKeyword,
            ForKeyword = forKeyword,
            Conditions = conditions,
            Action = action,
        };
    }

    private SetAssignmentStatement ParseSetAssignment()
    {
        var setKeyword = Advance();
        SyntaxToken? openParen = null;
        SyntaxToken? closeParen = null;
        List<IReadOnlyList<SyntaxToken>> targets;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            openParen = Advance();
            targets = [];
            targets.Add(ParseAssignmentTarget());
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                targets.Add(ParseAssignmentTarget());
            }

            closeParen = Expect(SyntaxKind.CloseParen);
        }
        else
        {
            targets = [ParseAssignmentTarget()];
        }

        var equals = Expect(SyntaxKind.EqualsToken);
        var value = ParseExpression();
        return new SetAssignmentStatement
        {
            Span = SourceSpan.From(setKeyword, value.Span),
            SetKeyword = setKeyword,
            OpenParen = openParen,
            Targets = targets,
            CloseParen = closeParen,
            EqualsToken = equals,
            Value = value,
        };
    }

    private IfStatement ParseIf()
    {
        var ifKeyword = Advance();
        var condition = ParseExpression();
        var thenKeyword = Expect(SyntaxKind.ThenKeyword);
        var thenStatements = ParseStatementList(IsIfListEnd);
        var elseIfs = new List<ElseIfClause>();
        while (IdentifierEquals(Keyword.ElseIf))
        {
            var elseIfKeyword = Advance();
            var elseIfCondition = ParseExpression();
            var elseIfThen = Expect(SyntaxKind.ThenKeyword);
            var statements = ParseStatementList(IsIfListEnd);
            var end = statements.Count > 0 ? statements[^1].Span : elseIfThen.Span;
            elseIfs.Add(new ElseIfClause
            {
                Span = SourceSpan.From(elseIfKeyword, end),
                ElseIfKeyword = elseIfKeyword,
                Condition = elseIfCondition,
                ThenKeyword = elseIfThen,
                Statements = statements,
            });
        }

        SyntaxToken? elseKeyword = null;
        IReadOnlyList<Query> elseStatements = [];
        if (_current.Kind == SyntaxKind.ElseKeyword)
        {
            elseKeyword = Advance();
            elseStatements = ParseStatementList(() => _current.Kind == SyntaxKind.EndKeyword);
        }

        var endKeyword = Expect(SyntaxKind.EndKeyword);
        var endIfKeyword = ExpectIdent(Keyword.If);
        return new IfStatement
        {
            Span = SourceSpan.From(ifKeyword, endIfKeyword),
            IfKeyword = ifKeyword,
            Condition = condition,
            ThenKeyword = thenKeyword,
            ThenStatements = thenStatements,
            ElseIfs = elseIfs,
            ElseKeyword = elseKeyword,
            ElseStatements = elseStatements,
            EndKeyword = endKeyword,
            EndIfKeyword = endIfKeyword,
        };
    }

    private CaseStatement ParseCaseStatement()
    {
        var caseKeyword = Advance();
        Expression? operand = null;
        if (_current.Kind != SyntaxKind.WhenKeyword)
        {
            operand = ParseExpression();
        }

        var whens = new List<CaseStatementWhen>();
        while (_current.Kind == SyntaxKind.WhenKeyword)
        {
            var whenKeyword = Advance();
            var whenOperand = ParseExpression();
            var thenKeyword = Expect(SyntaxKind.ThenKeyword);
            var statements = ParseStatementList(IsCaseWhenEnd);
            var end = statements.Count > 0 ? statements[^1].Span : thenKeyword.Span;
            whens.Add(new CaseStatementWhen
            {
                Span = SourceSpan.From(whenKeyword, end),
                WhenKeyword = whenKeyword,
                Operand = whenOperand,
                ThenKeyword = thenKeyword,
                Statements = statements,
            });
        }

        SyntaxToken? elseKeyword = null;
        IReadOnlyList<Query> elseStatements = [];
        if (_current.Kind == SyntaxKind.ElseKeyword)
        {
            elseKeyword = Advance();
            elseStatements = ParseStatementList(() => _current.Kind == SyntaxKind.EndKeyword);
        }

        var endKeyword = Expect(SyntaxKind.EndKeyword);
        var endCaseKeyword = Expect(SyntaxKind.CaseKeyword);
        return new CaseStatement
        {
            Span = SourceSpan.From(caseKeyword, endCaseKeyword),
            CaseKeyword = caseKeyword,
            Operand = operand,
            Whens = whens,
            ElseKeyword = elseKeyword,
            ElseStatements = elseStatements,
            EndKeyword = endKeyword,
            EndCaseKeyword = endCaseKeyword,
        };
    }

    private LoopStatement ParseLoop()
    {
        ParseBeginningLabel(out var label, out var colon);
        var loopKeyword = ExpectIdent(Keyword.Loop);
        var statements = ParseStatementList(() => _current.Kind == SyntaxKind.EndKeyword);
        var endKeyword = Expect(SyntaxKind.EndKeyword);
        var endLoopKeyword = ExpectIdent(Keyword.Loop);
        var endLabel = ParseEndingLabel();
        var end = endLabel ?? endLoopKeyword;
        return new LoopStatement
        {
            Span = SourceSpan.From(label ?? loopKeyword, end),
            Label = label,
            Colon = colon,
            LoopKeyword = loopKeyword,
            Statements = statements,
            EndKeyword = endKeyword,
            EndLoopKeyword = endLoopKeyword,
            EndLabel = endLabel,
        };
    }

    private WhileStatement ParseWhile()
    {
        ParseBeginningLabel(out var label, out var colon);
        var whileKeyword = ExpectIdent(Keyword.While);
        var condition = ParseExpression();
        var doKeyword = ExpectIdent(Keyword.Do);
        var statements = ParseStatementList(() => _current.Kind == SyntaxKind.EndKeyword);
        var endKeyword = Expect(SyntaxKind.EndKeyword);
        var endWhileKeyword = ExpectIdent(Keyword.While);
        var endLabel = ParseEndingLabel();
        var end = endLabel ?? endWhileKeyword;
        return new WhileStatement
        {
            Span = SourceSpan.From(label ?? whileKeyword, end),
            Label = label,
            Colon = colon,
            WhileKeyword = whileKeyword,
            Condition = condition,
            DoKeyword = doKeyword,
            Statements = statements,
            EndKeyword = endKeyword,
            EndWhileKeyword = endWhileKeyword,
            EndLabel = endLabel,
        };
    }

    private RepeatStatement ParseRepeatStatement()
    {
        ParseBeginningLabel(out var label, out var colon);
        var repeatKeyword = ExpectIdent(Keyword.Repeat);
        var statements = ParseStatementList(() => IdentifierEquals(Keyword.Until));
        var untilKeyword = ExpectIdent(Keyword.Until);
        var condition = ParseExpression();
        var endKeyword = Expect(SyntaxKind.EndKeyword);
        var endRepeatKeyword = ExpectIdent(Keyword.Repeat);
        var endLabel = ParseEndingLabel();
        var end = endLabel ?? endRepeatKeyword;
        return new RepeatStatement
        {
            Span = SourceSpan.From(label ?? repeatKeyword, end),
            Label = label,
            Colon = colon,
            RepeatKeyword = repeatKeyword,
            Statements = statements,
            UntilKeyword = untilKeyword,
            Condition = condition,
            EndKeyword = endKeyword,
            EndRepeatKeyword = endRepeatKeyword,
            EndLabel = endLabel,
        };
    }

    private ForStatement ParseForStatement()
    {
        ParseBeginningLabel(out var label, out var colon);
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var variable = Expect(SyntaxKind.Identifier);
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        SyntaxToken? cursorName = null;
        SyntaxToken? cursorKeyword = null;
        SyntaxToken? cursorFor = null;
        if (_current.Kind == SyntaxKind.Identifier && NextEquals(Keyword.Cursor))
        {
            cursorName = Advance();
            cursorKeyword = Advance();
            cursorFor = Expect(SyntaxKind.ForKeyword);
        }

        var query = ParseQuery();
        var doKeyword = ExpectIdent(Keyword.Do);
        var statements = ParseStatementList(() => _current.Kind == SyntaxKind.EndKeyword);
        var endKeyword = Expect(SyntaxKind.EndKeyword);
        var endForKeyword = Expect(SyntaxKind.ForKeyword);
        var endLabel = ParseEndingLabel();
        var end = endLabel ?? endForKeyword;
        return new ForStatement
        {
            Span = SourceSpan.From(label ?? forKeyword, end),
            Label = label,
            Colon = colon,
            ForKeyword = forKeyword,
            Variable = variable,
            AsKeyword = asKeyword,
            CursorName = cursorName,
            CursorKeyword = cursorKeyword,
            CursorForKeyword = cursorFor,
            CursorQuery = query,
            DoKeyword = doKeyword,
            Statements = statements,
            EndKeyword = endKeyword,
            EndForKeyword = endForKeyword,
            EndLabel = endLabel,
        };
    }

    private LeaveStatement ParseLeave()
    {
        var leaveKeyword = Advance();
        var label = Expect(SyntaxKind.Identifier);
        return new LeaveStatement
        {
            Span = SourceSpan.From(leaveKeyword, label),
            LeaveKeyword = leaveKeyword,
            Label = label,
        };
    }

    private IterateStatement ParseIterate()
    {
        var iterateKeyword = Advance();
        var label = Expect(SyntaxKind.Identifier);
        return new IterateStatement
        {
            Span = SourceSpan.From(iterateKeyword, label),
            IterateKeyword = iterateKeyword,
            Label = label,
        };
    }

    private List<Query> ParseStatementList(Func<bool> stop)
    {
        var statements = new List<Query>();
        while (_current.Kind != SyntaxKind.EndOfFile && !stop())
        {
            statements.Add(ParseStatement());
            if (_current.Kind != SyntaxKind.Semicolon)
            {
                break;
            }

            Advance();
        }

        return statements;
    }

    private ConditionValue ParseConditionValue()
    {
        if (IdentifierEquals(Keyword.SqlState))
        {
            var sqlState = Advance();
            SyntaxToken? valueKeyword = null;
            if (IdentifierEquals(Keyword.Value))
            {
                valueKeyword = Advance();
            }

            var state = ParseSqlStateValue();
            return new ConditionValue
            {
                Span = SourceSpan.From(sqlState, state),
                SqlStateKeyword = sqlState,
                ValueKeyword = valueKeyword,
                State = state,
                Name = sqlState,
            };
        }

        if (_current.Kind == SyntaxKind.NotKeyword && NextEquals(Keyword.Found))
        {
            var notKeyword = Advance();
            var found = Advance();
            return new ConditionValue
            {
                Span = SourceSpan.From(notKeyword, found),
                NotKeyword = notKeyword,
                Name = found,
            };
        }

        if (!IdentifierEquals(Keyword.Sqlexception) && !IdentifierEquals(Keyword.Sqlwarning)
            && _current.Kind != SyntaxKind.Identifier)
        {
            throw new SqlParseException($"Expected condition, found {_current.Kind}", _current.Position);
        }

        var name = Advance();
        return new ConditionValue { Span = name.Span, Name = name };
    }

    private SyntaxToken ParseSqlStateValue()
    {
        if (_current.Kind is SyntaxKind.String or SyntaxKind.QuestionMark or SyntaxKind.EmbeddedHost)
        {
            return Advance();
        }

        throw new SqlParseException($"Expected SQLSTATE value, found {_current.Kind}", _current.Position);
    }

    private List<SyntaxToken> ParseAssignmentTarget()
    {
        var target = new List<SyntaxToken> { ParseIntoTarget() };
        while (_current.Kind == SyntaxKind.Dot)
        {
            Advance();
            target.Add(Expect(SyntaxKind.Identifier));
        }

        return target;
    }

    private void ParseBeginningLabel(out SyntaxToken? label, out SyntaxToken? colon)
    {
        label = null;
        colon = null;
        if (_current.Kind == SyntaxKind.Identifier && NextKind == SyntaxKind.ColonToken)
        {
            label = Advance();
            colon = Advance();
        }
    }

    private SyntaxToken? ParseEndingLabel() =>
        _current.Kind == SyntaxKind.Identifier ? Advance() : null;

    private bool IsIfListEnd() =>
        IdentifierEquals(Keyword.ElseIf) || _current.Kind is SyntaxKind.ElseKeyword or SyntaxKind.EndKeyword;

    private bool IsCaseWhenEnd() =>
        _current.Kind is SyntaxKind.WhenKeyword or SyntaxKind.ElseKeyword or SyntaxKind.EndKeyword;
}
