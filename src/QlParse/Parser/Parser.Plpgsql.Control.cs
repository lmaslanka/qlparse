namespace QlParse;

// PL/pgSQL control flow: IF, CASE, LOOP, WHILE, FOR (integer range, query, EXECUTE, cursor), FOREACH,
// EXIT and CONTINUE. IF and CASE reuse the ISO SQL/PSM nodes; the loops have their own nodes because a
// PL/pgSQL label is written <<label>> and every loop body is LOOP ... END LOOP [label].
internal sealed partial class Parser
{
    private IfStatement ParsePlpgsqlIf()
    {
        var ifKeyword = Advance();
        var condition = ParseExpression();
        var thenKeyword = Expect(SyntaxKind.ThenKeyword);
        var thenStatements = ParsePlpgsqlStatements();
        var elseIfs = new List<ElseIfClause>();
        while (IdentifierEquals(Keyword.PlpgsqlElsif) || IdentifierEquals(Keyword.ElseIf))
        {
            var elseIfKeyword = Advance();
            var elseIfCondition = ParseExpression();
            var elseIfThen = Expect(SyntaxKind.ThenKeyword);
            var statements = ParsePlpgsqlStatements();
            elseIfs.Add(new ElseIfClause
            {
                Span = SourceSpan.From(elseIfKeyword, PgPreviousToken()),
                ElseIfKeyword = elseIfKeyword,
                Condition = elseIfCondition,
                ThenKeyword = elseIfThen,
                Statements = statements,
            });
        }

        SyntaxToken? elseKeyword = _current.Kind == SyntaxKind.ElseKeyword ? Advance() : null;
        IReadOnlyList<Query> elseStatements = elseKeyword is null ? [] : ParsePlpgsqlStatements();
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

    // CASE [operand] WHEN expr [, expr ...] THEN statements ... [ELSE statements] END CASE
    private CaseStatement ParsePlpgsqlCase()
    {
        var caseKeyword = Advance();
        var operand = _current.Kind == SyntaxKind.WhenKeyword ? null : ParseExpression();
        var whens = new List<CaseStatementWhen>();
        do
        {
            var whenKeyword = Expect(SyntaxKind.WhenKeyword);
            var whenOperand = ParseExpression();
            var additional = new List<Expression>();
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                additional.Add(ParseExpression());
            }

            var thenKeyword = Expect(SyntaxKind.ThenKeyword);
            var statements = ParsePlpgsqlStatements();
            whens.Add(new CaseStatementWhen
            {
                Span = SourceSpan.From(whenKeyword, PgPreviousToken()),
                WhenKeyword = whenKeyword,
                Operand = whenOperand,
                AdditionalOperands = additional,
                ThenKeyword = thenKeyword,
                Statements = statements,
            });
        }
        while (_current.Kind == SyntaxKind.WhenKeyword);

        SyntaxToken? elseKeyword = _current.Kind == SyntaxKind.ElseKeyword ? Advance() : null;
        IReadOnlyList<Query> elseStatements = elseKeyword is null ? [] : ParsePlpgsqlStatements();
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

    // LOOP statements END LOOP [label]
    private PlpgsqlLoopBody ParsePlpgsqlLoopBody()
    {
        var loopKeyword = ExpectIdent(Keyword.Loop);
        var statements = ParsePlpgsqlStatements();
        var endKeyword = Expect(SyntaxKind.EndKeyword);
        var endLoopKeyword = ExpectIdent(Keyword.Loop);
        var endLabel = TryParsePlpgsqlEndLabel();
        return new PlpgsqlLoopBody
        {
            Span = SourceSpan.From(loopKeyword, PgPreviousToken()),
            LoopKeyword = loopKeyword,
            Statements = statements,
            EndKeyword = endKeyword,
            EndLoopKeyword = endLoopKeyword,
            EndLabel = endLabel,
        };
    }

    private SourceSpan PlpgsqlLoopSpan(PlpgsqlLabel? label, SyntaxToken start, PlpgsqlLoopBody body) =>
        label is null ? SourceSpan.From(start, body.Span) : SourceSpan.From(label.Span, body.Span);

    private PlpgsqlLoopStatement ParsePlpgsqlLoop(PlpgsqlLabel? label)
    {
        var start = _current;
        var body = ParsePlpgsqlLoopBody();
        return new PlpgsqlLoopStatement { Span = PlpgsqlLoopSpan(label, start, body), Label = label, Body = body };
    }

    private PlpgsqlWhileStatement ParsePlpgsqlWhile(PlpgsqlLabel? label)
    {
        var whileKeyword = Advance();
        var condition = ParseExpression();
        var body = ParsePlpgsqlLoopBody();
        return new PlpgsqlWhileStatement
        {
            Span = PlpgsqlLoopSpan(label, whileKeyword, body),
            Label = label,
            WhileKeyword = whileKeyword,
            Condition = condition,
            Body = body,
        };
    }

    // FOREACH target [SLICE n] IN ARRAY expr LOOP ... END LOOP
    private PlpgsqlForeachStatement ParsePlpgsqlForeach(PlpgsqlLabel? label)
    {
        var foreachKeyword = Advance();
        var targets = ParsePlpgsqlTargets();
        SyntaxToken? sliceKeyword = IdentifierEquals(Keyword.PlpgsqlSlice) ? Advance() : null;
        SyntaxToken? slice = sliceKeyword is null ? null : Expect(SyntaxKind.Number);
        var inKeyword = Expect(SyntaxKind.InKeyword);
        var arrayKeyword = Expect(SyntaxKind.ArrayKeyword);
        var array = ParseExpression();
        var body = ParsePlpgsqlLoopBody();
        return new PlpgsqlForeachStatement
        {
            Span = PlpgsqlLoopSpan(label, foreachKeyword, body),
            Label = label,
            ForeachKeyword = foreachKeyword,
            Targets = targets,
            SliceKeyword = sliceKeyword,
            Slice = slice,
            InKeyword = inKeyword,
            ArrayKeyword = arrayKeyword,
            Array = array,
            Body = body,
        };
    }

    // EXIT | CONTINUE [label] [WHEN condition]
    private PlpgsqlExitStatement ParsePlpgsqlExit()
    {
        var keyword = Advance();
        var label = TryParsePlpgsqlEndLabel();
        SyntaxToken? whenKeyword = _current.Kind == SyntaxKind.WhenKeyword ? Advance() : null;
        var condition = whenKeyword is null ? null : ParseExpression();
        return new PlpgsqlExitStatement
        {
            Span = SourceSpan.From(keyword, PgPreviousToken()),
            Keyword = keyword,
            Label = label,
            WhenKeyword = whenKeyword,
            Condition = condition,
        };
    }
}
