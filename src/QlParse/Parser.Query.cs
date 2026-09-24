namespace QlParse;

internal sealed partial class Parser
{
    private Query ParseQuery(int minBindingPower = 0)
    {
        if (minBindingPower == 0 && _current.Kind == SyntaxKind.WithKeyword)
        {
            return ParseWithQuery();
        }

        return ParseSetOp(minBindingPower);
    }

    private WithQuery ParseWithQuery()
    {
        var withKeyword = Expect(SyntaxKind.WithKeyword);
        var recursive = _current.Kind == SyntaxKind.RecursiveKeyword ? Advance() : (SyntaxToken?)null;
        SyntaxToken? recursionLimit = null;
        if (recursive is not null && (IdentifierEquals(Keyword.Linear) || IdentifierEquals(Keyword.General)))
        {
            recursionLimit = Advance();
        }

        var ctes = new List<CommonTableExpression> { ParseCte() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            ctes.Add(ParseCte());
        }

        var query = ParseQuery(1);
        return new WithQuery
        {
            Span = SourceSpan.From(withKeyword, query.Span),
            WithKeyword = withKeyword,
            RecursiveKeyword = recursive,
            RecursionLimit = recursionLimit,
            Ctes = ctes,
            Query = query,
        };
    }

    private CommonTableExpression ParseCte()
    {
        var name = Expect(SyntaxKind.Identifier);
        SyntaxToken? openParen = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        SyntaxToken? closeParen = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            openParen = Advance();
            var names = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                names.Add(Expect(SyntaxKind.Identifier));
            }

            columns = names;
            closeParen = Expect(SyntaxKind.CloseParen);
        }

        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var openQuery = Expect(SyntaxKind.OpenParen);
        var query = ParseQuery();
        var closeQuery = Expect(SyntaxKind.CloseParen);
        var search = _current.Kind == SyntaxKind.SearchKeyword ? ParseSearchClause() : null;
        var cycle = _current.Kind == SyntaxKind.CycleKeyword ? ParseCycleClause() : null;
        var end = cycle?.Span ?? search?.Span ?? closeQuery.Span;
        return new CommonTableExpression
        {
            Span = SourceSpan.From(name, end),
            Name = name,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = closeParen,
            AsKeyword = asKeyword,
            OpenQuery = openQuery,
            Query = query,
            CloseQuery = closeQuery,
            Search = search,
            Cycle = cycle,
        };
    }

    private SearchClause ParseSearchClause()
    {
        var searchKeyword = Expect(SyntaxKind.SearchKeyword);
        if (!IdentifierEquals(Keyword.Depth) && !IdentifierEquals(Keyword.Breadth))
        {
            throw new SqlParseException($"Expected DEPTH or BREADTH, found {_current.Kind}", _current.Position);
        }

        var orderKeyword = Advance();
        if (!IdentifierEquals(Keyword.First))
        {
            throw new SqlParseException($"Expected FIRST, found {_current.Kind}", _current.Position);
        }

        var firstKeyword = Advance();
        var byKeyword = Expect(SyntaxKind.ByKeyword);
        var columns = ParseNameList();
        var setKeyword = Expect(SyntaxKind.SetKeyword);
        var sequenceColumn = Expect(SyntaxKind.Identifier);
        return new SearchClause
        {
            Span = SourceSpan.From(searchKeyword, sequenceColumn),
            SearchKeyword = searchKeyword,
            OrderKeyword = orderKeyword,
            FirstKeyword = firstKeyword,
            ByKeyword = byKeyword,
            Columns = columns,
            SetKeyword = setKeyword,
            SequenceColumn = sequenceColumn,
        };
    }

    private CycleClause ParseCycleClause()
    {
        var cycleKeyword = Expect(SyntaxKind.CycleKeyword);
        var columns = ParseNameList();
        var setKeyword = Expect(SyntaxKind.SetKeyword);
        var markColumn = Expect(SyntaxKind.Identifier);
        if (!IdentifierEquals(Keyword.To))
        {
            throw new SqlParseException($"Expected TO, found {_current.Kind}", _current.Position);
        }

        var toKeyword = Advance();
        var markValue = ParseExpression();
        if (!IdentifierEquals(Keyword.Default))
        {
            throw new SqlParseException($"Expected DEFAULT, found {_current.Kind}", _current.Position);
        }

        var defaultKeyword = Advance();
        var defaultValue = ParseExpression();
        var usingKeyword = Expect(SyntaxKind.UsingKeyword);
        var pathColumn = Expect(SyntaxKind.Identifier);
        return new CycleClause
        {
            Span = SourceSpan.From(cycleKeyword, pathColumn),
            CycleKeyword = cycleKeyword,
            Columns = columns,
            SetKeyword = setKeyword,
            MarkColumn = markColumn,
            ToKeyword = toKeyword,
            MarkValue = markValue,
            DefaultKeyword = defaultKeyword,
            DefaultValue = defaultValue,
            UsingKeyword = usingKeyword,
            PathColumn = pathColumn,
        };
    }

    private IReadOnlyList<SyntaxToken> ParseNameList()
    {
        var names = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            names.Add(Expect(SyntaxKind.Identifier));
        }

        return names;
    }

    private Query ParseSetOp(int minBindingPower = 0)
    {
        Query left = ParseSetPrimary();
        while (true)
        {
            var bindingPower = SetOpBindingPower(_current.Kind);
            if (bindingPower == 0 || bindingPower < minBindingPower)
            {
                break;
            }

            var op = Advance();
            var all = _current.Kind == SyntaxKind.AllKeyword ? Advance() : (SyntaxToken?)null;
            var corresponding = _current.Kind == SyntaxKind.CorrespondingKeyword ? Advance() : (SyntaxToken?)null;
            SyntaxToken? byKeyword = null;
            SyntaxToken? openParen = null;
            IReadOnlyList<SyntaxToken>? columns = null;
            SyntaxToken? closeParen = null;
            if (corresponding is not null && _current.Kind == SyntaxKind.ByKeyword)
            {
                byKeyword = Advance();
                openParen = Expect(SyntaxKind.OpenParen);
                var names = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
                while (_current.Kind == SyntaxKind.Comma)
                {
                    Advance();
                    names.Add(Expect(SyntaxKind.Identifier));
                }

                columns = names;
                closeParen = Expect(SyntaxKind.CloseParen);
            }

            var right = ParseQuery(bindingPower + 1);
            left = new SetOperation
            {
                Span = SourceSpan.From(left.Span, right.Span),
                Left = left,
                Operator = op,
                AllKeyword = all,
                CorrespondingKeyword = corresponding,
                ByKeyword = byKeyword,
                OpenParen = openParen,
                Columns = columns,
                CloseParen = closeParen,
                Right = right,
            };
        }

        return left;
    }

    private Query ParseSetPrimary()
    {
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            return ParseParenQuery();
        }

        if (_current.Kind == SyntaxKind.ValuesKeyword)
        {
            return ParseValuesQuery();
        }

        return ParseSelectStatement();
    }

    private ValuesQuery ParseValuesQuery()
    {
        var valuesKeyword = Advance();
        var rows = new List<ValuesRow> { ParseValuesRow() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            rows.Add(ParseValuesRow());
        }

        return new ValuesQuery
        {
            Span = SourceSpan.From(valuesKeyword, rows[^1].Span),
            ValuesKeyword = valuesKeyword,
            Rows = rows,
        };
    }

    private ValuesRow ParseValuesRow()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var values = ParseExpressionList();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new ValuesRow
        {
            Span = SourceSpan.From(openParen, closeParen),
            OpenParen = openParen,
            Values = values,
            CloseParen = closeParen,
        };
    }

    private ParenQuery ParseParenQuery()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var inner = ParseQuery();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new ParenQuery
        {
            Span = SourceSpan.From(openParen, closeParen),
            OpenParen = openParen,
            Inner = inner,
            CloseParen = closeParen,
        };
    }

    private const int UnionBindingPower = 1;
    private const int IntersectBindingPower = 2;

    private static bool IsQueryStart(SyntaxKind kind) =>
        kind is SyntaxKind.SelectKeyword or SyntaxKind.WithKeyword or SyntaxKind.ValuesKeyword;

    private static int SetOpBindingPower(SyntaxKind kind) => kind switch
    {
        SyntaxKind.UnionKeyword or SyntaxKind.ExceptKeyword => UnionBindingPower,
        SyntaxKind.IntersectKeyword => IntersectBindingPower,
        _ => 0,
    };
}
