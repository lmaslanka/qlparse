namespace QlParse;

// PL/pgSQL FOR loops. After FOR targets IN the form is decided by lookahead up to the LOOP keyword:
// a `..` before LOOP (or REVERSE) makes an integer range loop, EXECUTE a dynamic query loop, a name
// followed by `(` or LOOP a cursor loop, and anything else a query loop.
internal sealed partial class Parser
{
    private Query ParsePlpgsqlFor(PlpgsqlLabel? label)
    {
        var forKeyword = Advance();
        var targets = ParsePlpgsqlTargets();
        var inKeyword = Expect(SyntaxKind.InKeyword);
        var loopIndex = FindPlpgsqlLoopKeyword();
        if (IdentifierEquals(Keyword.PlpgsqlReverse) || HasPlpgsqlRangeBefore(loopIndex))
        {
            return ParsePlpgsqlForRange(label, forKeyword, PlpgsqlLoopVariable(targets), inKeyword);
        }

        if (IsPlpgsqlCursorLoopStart())
        {
            return ParsePlpgsqlForCursor(label, forKeyword, PlpgsqlLoopVariable(targets), inKeyword);
        }

        var query = IdentifierEquals(Keyword.Execute) ? ParsePlpgsqlExecute() : ParsePlpgsqlBoundedQuery(loopIndex);
        var body = ParsePlpgsqlLoopBody();
        return new PlpgsqlForQueryStatement
        {
            Span = PlpgsqlLoopSpan(label, forKeyword, body),
            Label = label,
            ForKeyword = forKeyword,
            Targets = targets,
            InKeyword = inKeyword,
            Query = query,
            Body = body,
        };
    }

    private bool IsPlpgsqlCursorLoopStart() =>
        _current.Kind == SyntaxKind.Identifier && !IdentifierEquals(Keyword.Execute) && !IdentifierEquals(Keyword.Table)
        && IsPlpgsqlCursorLoopNext();

    private bool IsPlpgsqlCursorLoopNext() => NextKind == SyntaxKind.OpenParen || NextEquals(Keyword.Loop);

    private static SyntaxToken PlpgsqlLoopVariable(List<Expression> targets) =>
        targets is [IdentifierExpression { Identifier: var variable }]
            ? variable
            : throw new SqlParseException("Expected a single loop variable", targets[0].Span.Position);

    // The index of the LOOP keyword that ends the FOR header (parenthesis depth 0), or of the token
    // that stops the search.
    private int FindPlpgsqlLoopKeyword()
    {
        var depth = 0;
        var index = _index - 1;
        while (index < _tokens.Count)
        {
            var token = _tokens[index];
            if (token.Kind is SyntaxKind.Semicolon or SyntaxKind.EndOfFile
                || (depth == 0 && TokenEquals(token, Keyword.Loop)))
            {
                return index;
            }

            depth += PlpgsqlDepthChange(token.Kind);
            index++;
        }

        return _tokens.Count - 1;
    }

    private bool HasPlpgsqlRangeBefore(int end)
    {
        for (var index = _index - 1; index < end; index++)
        {
            if (_tokens[index].Kind == SyntaxKind.DotDotToken)
            {
                return true;
            }
        }

        return false;
    }

    // FOR i IN [REVERSE] lower .. upper [BY step] LOOP ... END LOOP
    private PlpgsqlForRangeStatement ParsePlpgsqlForRange(PlpgsqlLabel? label, SyntaxToken forKeyword, SyntaxToken variable, SyntaxToken inKeyword)
    {
        SyntaxToken? reverseKeyword = IdentifierEquals(Keyword.PlpgsqlReverse) ? Advance() : null;
        var lower = ParseExpression();
        var dotDot = Expect(SyntaxKind.DotDotToken);
        var upper = ParseExpression();
        SyntaxToken? byKeyword = _current.Kind == SyntaxKind.ByKeyword ? Advance() : null;
        var step = byKeyword is null ? null : ParseExpression();
        var body = ParsePlpgsqlLoopBody();
        return new PlpgsqlForRangeStatement
        {
            Span = PlpgsqlLoopSpan(label, forKeyword, body),
            Label = label,
            ForKeyword = forKeyword,
            Variable = variable,
            InKeyword = inKeyword,
            ReverseKeyword = reverseKeyword,
            Lower = lower,
            DotDot = dotDot,
            Upper = upper,
            ByKeyword = byKeyword,
            Step = step,
            Body = body,
        };
    }

    // FOR record IN cursor [(arguments)] LOOP ... END LOOP
    private PlpgsqlForCursorStatement ParsePlpgsqlForCursor(PlpgsqlLabel? label, SyntaxToken forKeyword, SyntaxToken variable, SyntaxToken inKeyword)
    {
        var cursor = Advance();
        SyntaxToken? openParen = _current.Kind == SyntaxKind.OpenParen ? Advance() : null;
        IReadOnlyList<Expression> arguments = openParen is null ? [] : ParsePgFunctionArguments();
        SyntaxToken? closeParen = openParen is null ? null : Expect(SyntaxKind.CloseParen);
        var body = ParsePlpgsqlLoopBody();
        return new PlpgsqlForCursorStatement
        {
            Span = PlpgsqlLoopSpan(label, forKeyword, body),
            Label = label,
            ForKeyword = forKeyword,
            Variable = variable,
            InKeyword = inKeyword,
            Cursor = cursor,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
            Body = body,
        };
    }
}
