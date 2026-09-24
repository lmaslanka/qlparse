namespace QlParse;

internal sealed partial class Parser
{
    private TableSource ParseTableSource()
    {
        var table = ParseTablePrimary();
        while (true)
        {
            if (_current.Kind == SyntaxKind.TablesampleKeyword)
            {
                table = ParseSample(table);
                continue;
            }

            if (TokenEquals(_current, Keyword.MatchRecognize))
            {
                table = ParseMatchRecognize(table);
                continue;
            }

            return table;
        }
    }

    private TableSource ParseTablePrimary()
    {
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            return IsQueryStart(NextKind) ? ParseDerivedTable() : ParseJoinedTable();
        }

        if (_current.Kind == SyntaxKind.UnnestKeyword)
        {
            return ParseUnnest();
        }

        if (_current.Kind == SyntaxKind.LateralKeyword)
        {
            var lateral = Advance();
            return ParseDerivedTable(lateral);
        }

        if (_current.Kind == SyntaxKind.OnlyKeyword)
        {
            return ParseOnly();
        }

        if (IdentifierEquals(Keyword.Table) && NextKind == SyntaxKind.OpenParen)
        {
            return ParseTableFunction(Advance());
        }

        if (IsMarkupTable())
        {
            return ParseMarkupTable();
        }

        if (IsGraphTable())
        {
            return ParseGraphTable();
        }

        if (IsPtfInvocation())
        {
            return ParsePtf();
        }

        return ParseTableReference();
    }

    private JoinedTable ParseJoinedTable()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var table = ParseTableSource();
        var joins = ParseJoins();
        if (joins.Count == 0)
        {
            throw new SqlParseException("Expected JOIN in parenthesized table", _current.Position);
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new JoinedTable
        {
            Span = SourceSpan.From(openParen, closeParen),
            OpenParen = openParen,
            Table = table,
            Joins = joins,
            CloseParen = closeParen,
        };
    }

    private DerivedTable ParseDerivedTable(SyntaxToken? lateral = null)
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var query = ParseQuery();
        var closeParen = Expect(SyntaxKind.CloseParen);
        SyntaxToken? asKeyword = null;
        SyntaxToken alias;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            alias = Expect(SyntaxKind.Identifier);
        }
        else if (_current.Kind == SyntaxKind.Identifier)
        {
            alias = Advance();
        }
        else
        {
            throw new SqlParseException("Expected alias after derived table", _current.Position);
        }

        ParseOptionalColumnList(out var columnOpen, out var columns, out var columnClose);
        var end = columnClose ?? alias;
        var start = lateral ?? openParen;
        return new DerivedTable
        {
            Span = SourceSpan.From(start, end),
            LateralKeyword = lateral,
            OpenParen = openParen,
            Query = query,
            CloseParen = closeParen,
            AsKeyword = asKeyword,
            Alias = alias,
            ColumnOpenParen = columnOpen,
            Columns = columns,
            ColumnCloseParen = columnClose,
        };
    }

    private OnlyTable ParseOnly()
    {
        var onlyKeyword = Expect(SyntaxKind.OnlyKeyword);
        var openParen = Expect(SyntaxKind.OpenParen);
        var nameParts = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        while (_current.Kind == SyntaxKind.Dot)
        {
            Advance();
            nameParts.Add(Expect(SyntaxKind.Identifier));
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        var systemTime = ParseSystemTime();
        SyntaxToken? asKeyword = null;
        SyntaxToken? alias = null;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            alias = Expect(SyntaxKind.Identifier);
        }
        else if (_current.Kind == SyntaxKind.Identifier && !TokenEquals(_current, Keyword.MatchRecognize))
        {
            alias = Advance();
        }

        SyntaxToken? columnOpen = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        SyntaxToken? columnClose = null;
        if (alias is not null)
        {
            ParseOptionalColumnList(out columnOpen, out columns, out columnClose);
        }

        var end = columnClose?.Span ?? alias?.Span ?? systemTime?.Span ?? closeParen.Span;
        return new OnlyTable
        {
            Span = SourceSpan.From(onlyKeyword.Span, end),
            OnlyKeyword = onlyKeyword,
            OpenParen = openParen,
            NameParts = nameParts,
            CloseParen = closeParen,
            AsKeyword = asKeyword,
            Alias = alias,
            ColumnOpenParen = columnOpen,
            Columns = columns,
            ColumnCloseParen = columnClose,
            SystemTime = systemTime,
        };
    }

    private TableFunction ParseTableFunction(SyntaxToken tableKeyword)
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        Query? query = null;
        Expression? argument = null;
        if (IsQueryStart(_current.Kind))
        {
            query = ParseQuery();
        }
        else if (_current.Kind != SyntaxKind.CloseParen)
        {
            argument = ParseExpression();
        }
        else
        {
            throw new SqlParseException("Expected table function argument", _current.Position);
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        ParseCorrelation(required: false, out var asKeyword, out var alias, out var columnOpen, out var columns, out var columnClose);
        var end = columnClose ?? alias ?? closeParen;
        return new TableFunction
        {
            Span = SourceSpan.From(tableKeyword, end),
            TableKeyword = tableKeyword,
            OpenParen = openParen,
            Arguments = argument is null ? [] : [argument],
            Query = query,
            CloseParen = closeParen,
            AsKeyword = asKeyword,
            Alias = alias,
            ColumnOpenParen = columnOpen,
            Columns = columns,
            ColumnCloseParen = columnClose,
        };
    }

    private SampledTable ParseSample(TableSource table)
    {
        var tablesample = Expect(SyntaxKind.TablesampleKeyword);
        if (!IdentifierEquals(Keyword.Bernoulli) && !IdentifierEquals(Keyword.System))
        {
            throw new SqlParseException($"Expected BERNOULLI or SYSTEM, found {_current.Kind}", _current.Position);
        }

        var method = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var percentage = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        SyntaxToken? repeatable = null;
        SyntaxToken? repeatOpen = null;
        Expression? repeatArgument = null;
        SyntaxToken? repeatClose = null;
        if (IdentifierEquals(Keyword.Repeatable))
        {
            repeatable = Advance();
            repeatOpen = Expect(SyntaxKind.OpenParen);
            repeatArgument = ParseExpression();
            repeatClose = Expect(SyntaxKind.CloseParen);
        }

        var end = repeatClose ?? closeParen;
        return new SampledTable
        {
            Span = SourceSpan.From(table.Span, end),
            Table = table,
            TablesampleKeyword = tablesample,
            Method = method,
            OpenParen = openParen,
            Percentage = percentage,
            CloseParen = closeParen,
            RepeatableKeyword = repeatable,
            RepeatOpenParen = repeatOpen,
            RepeatArgument = repeatArgument,
            RepeatCloseParen = repeatClose,
        };
    }

    private void ParseCorrelation(
        bool required,
        out SyntaxToken? asKeyword,
        out SyntaxToken? alias,
        out SyntaxToken? columnOpen,
        out IReadOnlyList<SyntaxToken>? columns,
        out SyntaxToken? columnClose)
    {
        asKeyword = null;
        alias = null;
        columnOpen = null;
        columns = null;
        columnClose = null;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            alias = Expect(SyntaxKind.Identifier);
        }
        else if (_current.Kind == SyntaxKind.Identifier)
        {
            alias = Advance();
        }
        else if (required)
        {
            throw new SqlParseException("Expected alias", _current.Position);
        }
        else
        {
            return;
        }

        ParseOptionalColumnList(out columnOpen, out columns, out columnClose);
    }

    private TableSource ParseTableReference()
    {
        var nameParts = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        while (_current.Kind == SyntaxKind.Dot)
        {
            Advance();
            nameParts.Add(Expect(SyntaxKind.Identifier));
        }

        if (_current.Kind == SyntaxKind.OpenParen)
        {
            return ParseRoutineTable(nameParts);
        }

        var systemTime = ParseSystemTime();
        SyntaxToken? asKeyword = null;
        SyntaxToken? alias = null;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            alias = Expect(SyntaxKind.Identifier);
        }
        else if (_current.Kind == SyntaxKind.Identifier && !TokenEquals(_current, Keyword.MatchRecognize))
        {
            alias = Advance();
        }

        SyntaxToken? columnOpen = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        SyntaxToken? columnClose = null;
        if (alias is not null)
        {
            ParseOptionalColumnList(out columnOpen, out columns, out columnClose);
        }

        var end = columnClose?.Span ?? alias?.Span ?? systemTime?.Span ?? nameParts[^1].Span;
        return new TableReference
        {
            Span = SourceSpan.From(nameParts[0].Span, end),
            NameParts = nameParts,
            AsKeyword = asKeyword,
            Alias = alias,
            ColumnOpenParen = columnOpen,
            Columns = columns,
            ColumnCloseParen = columnClose,
            SystemTime = systemTime,
        };
    }

    private SystemTimeClause? ParseSystemTime()
    {
        if (_current.Kind != SyntaxKind.ForKeyword || !NextEquals(Keyword.SystemTime))
        {
            return null;
        }

        var forKeyword = Advance();
        var systemTimeKeyword = Advance();
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            var asKeyword = Advance();
            var ofKeyword = Expect(SyntaxKind.OfKeyword);
            var point = ParseExpression(ComparisonBindingPower + 1);
            return new SystemTimeClause
            {
                Span = SourceSpan.From(forKeyword, point.Span),
                ForKeyword = forKeyword,
                SystemTimeKeyword = systemTimeKeyword,
                AsKeyword = asKeyword,
                OfKeyword = ofKeyword,
                Point = point,
            };
        }

        if (_current.Kind == SyntaxKind.BetweenKeyword)
        {
            var betweenKeyword = Advance();
            SyntaxToken? qualifier = null;
            if (IdentifierEquals(Keyword.Asymmetric) || IdentifierEquals(Keyword.Symmetric))
            {
                qualifier = Advance();
            }

            var start = ParseExpression(ComparisonBindingPower + 1);
            var andKeyword = Expect(SyntaxKind.AndKeyword);
            var end = ParseExpression(ComparisonBindingPower + 1);
            return new SystemTimeClause
            {
                Span = SourceSpan.From(forKeyword, end.Span),
                ForKeyword = forKeyword,
                SystemTimeKeyword = systemTimeKeyword,
                BetweenKeyword = betweenKeyword,
                Qualifier = qualifier,
                Start = start,
                AndKeyword = andKeyword,
                End = end,
            };
        }

        if (_current.Kind == SyntaxKind.FromKeyword)
        {
            var fromKeyword = Advance();
            var start = ParseExpression(ComparisonBindingPower + 1);
            if (!IdentifierEquals(Keyword.To))
            {
                throw new SqlParseException($"Expected TO, found {_current.Kind}", _current.Position);
            }

            var toKeyword = Advance();
            var end = ParseExpression(ComparisonBindingPower + 1);
            return new SystemTimeClause
            {
                Span = SourceSpan.From(forKeyword, end.Span),
                ForKeyword = forKeyword,
                SystemTimeKeyword = systemTimeKeyword,
                FromKeyword = fromKeyword,
                Start = start,
                ToKeyword = toKeyword,
                End = end,
            };
        }

        if (_current.Kind != SyntaxKind.AllKeyword)
        {
            throw new SqlParseException($"Expected AS, BETWEEN, FROM, or ALL, found {_current.Kind}", _current.Position);
        }

        var allKeyword = Advance();
        return new SystemTimeClause
        {
            Span = SourceSpan.From(forKeyword, allKeyword),
            ForKeyword = forKeyword,
            SystemTimeKeyword = systemTimeKeyword,
            AllKeyword = allKeyword,
        };
    }

    private TableFunction ParseRoutineTable(IReadOnlyList<SyntaxToken> nameParts)
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var arguments = _current.Kind == SyntaxKind.CloseParen ? [] : ParseExpressionList();
        var closeParen = Expect(SyntaxKind.CloseParen);
        ParseCorrelation(required: false, out var asKeyword, out var alias, out var columnOpen, out var columns, out var columnClose);
        var end = columnClose ?? alias ?? closeParen;
        return new TableFunction
        {
            Span = SourceSpan.From(nameParts[0], end),
            NameParts = nameParts,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
            AsKeyword = asKeyword,
            Alias = alias,
            ColumnOpenParen = columnOpen,
            Columns = columns,
            ColumnCloseParen = columnClose,
        };
    }

    private void ParseOptionalColumnList(
        out SyntaxToken? openParen,
        out IReadOnlyList<SyntaxToken>? columns,
        out SyntaxToken? closeParen)
    {
        openParen = null;
        columns = null;
        closeParen = null;
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return;
        }

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

    private IReadOnlyList<CommaFrom> ParseCommaFrom()
    {
        if (_current.Kind != SyntaxKind.Comma)
        {
            return [];
        }

        var extras = new List<CommaFrom>();
        while (_current.Kind == SyntaxKind.Comma)
        {
            var comma = Advance();
            var table = ParseTableSource();
            var joins = ParseJoins();
            extras.Add(new CommaFrom
            {
                Span = SourceSpan.From(comma, joins.Count > 0 ? joins[^1].Span : table.Span),
                Comma = comma,
                Table = table,
                Joins = joins,
            });
        }

        return extras;
    }

    private IReadOnlyList<JoinClause> ParseJoins()
    {
        if (!IsJoinStart())
        {
            return [];
        }

        var joins = new List<JoinClause>();
        while (IsJoinStart())
        {
            joins.Add(ParseJoin());
        }

        return joins;
    }

    private JoinClause ParseJoin()
    {
        var natural = _current.Kind == SyntaxKind.NaturalKeyword ? Advance() : (SyntaxToken?)null;
        SyntaxToken? joinType = null;
        if (_current.Kind is SyntaxKind.InnerKeyword or SyntaxKind.LeftKeyword or SyntaxKind.RightKeyword
            or SyntaxKind.FullKeyword or SyntaxKind.CrossKeyword or SyntaxKind.UnionKeyword)
        {
            joinType = Advance();
        }

        SyntaxToken? outer = null;
        if (_current.Kind == SyntaxKind.OuterKeyword)
        {
            if (joinType is not { Kind: SyntaxKind.LeftKeyword or SyntaxKind.RightKeyword or SyntaxKind.FullKeyword })
            {
                throw new SqlParseException("OUTER is only valid after LEFT, RIGHT, or FULL", _current.Position);
            }

            outer = Advance();
        }

        var joinKeyword = Expect(SyntaxKind.JoinKeyword);
        var table = ParseTableSource();
        var constraint = ParseJoinConstraint(natural, joinType);
        var start = natural ?? joinType ?? joinKeyword;
        var end = constraint?.Span ?? table.Span;
        return new JoinClause
        {
            Span = SourceSpan.From(start, end),
            NaturalKeyword = natural,
            JoinType = joinType,
            OuterKeyword = outer,
            JoinKeyword = joinKeyword,
            Table = table,
            Constraint = constraint,
        };
    }

    private JoinConstraint? ParseJoinConstraint(SyntaxToken? natural, SyntaxToken? joinType)
    {
        var isCross = joinType is { Kind: SyntaxKind.CrossKeyword };
        var isUnion = joinType is { Kind: SyntaxKind.UnionKeyword };
        if (_current.Kind == SyntaxKind.OnKeyword)
        {
            if (natural is not null || isCross || isUnion)
            {
                throw new SqlParseException("ON is not valid with NATURAL, CROSS, or UNION JOIN", _current.Position);
            }

            var onKeyword = Advance();
            var condition = ParseExpression();
            return new OnConstraint
            {
                Span = SourceSpan.From(onKeyword, condition.Span),
                OnKeyword = onKeyword,
                Condition = condition,
            };
        }

        if (_current.Kind == SyntaxKind.UsingKeyword)
        {
            if (natural is not null || isCross || isUnion)
            {
                throw new SqlParseException("USING is not valid with NATURAL, CROSS, or UNION JOIN", _current.Position);
            }

            return ParseUsingConstraint();
        }

        if (natural is null && !isCross && !isUnion)
        {
            throw new SqlParseException("Expected ON or USING", _current.Position);
        }

        return null;
    }

    private UsingConstraint ParseUsingConstraint()
    {
        var usingKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var columns = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            columns.Add(Expect(SyntaxKind.Identifier));
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new UsingConstraint
        {
            Span = SourceSpan.From(usingKeyword, closeParen),
            UsingKeyword = usingKeyword,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = closeParen,
        };
    }

    private bool IsJoinStart() =>
        _current.Kind is SyntaxKind.NaturalKeyword or SyntaxKind.InnerKeyword or SyntaxKind.LeftKeyword
            or SyntaxKind.RightKeyword or SyntaxKind.FullKeyword or SyntaxKind.CrossKeyword
            or SyntaxKind.JoinKeyword
        || (_current.Kind == SyntaxKind.UnionKeyword && NextKind == SyntaxKind.JoinKeyword);
}
