namespace QlParse;

internal sealed partial class Parser
{
    private MatchRecognizeTable ParseMatchRecognize(TableSource input)
    {
        var keyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        SyntaxToken? partition = null;
        SyntaxToken? partitionBy = null;
        IReadOnlyList<Expression> partitionList = [];
        if (IdentifierEquals(Keyword.Partition))
        {
            partition = Advance();
            partitionBy = Expect(SyntaxKind.ByKeyword);
            partitionList = ParseExpressionList();
        }

        OrderByClause? orderBy = _current.Kind == SyntaxKind.OrderKeyword ? ParseOrderBy() : null;
        SyntaxToken? measuresKeyword = null;
        IReadOnlyList<PatternMeasure> measures = [];
        if (IdentifierEquals(Keyword.Measures))
        {
            measuresKeyword = Advance();
            measures = ParseMeasures();
        }

        ParseRowsPerMatch(out var rowsKind, out var rowOrRows, out var perKeyword, out var matchKeyword, out var emptyHandling);
        ParseSkip(out var afterKeyword, out var skipKeyword, out var skipTo, out var skipPosition, out var skipTarget);
        var patternKeyword = ExpectIdent(Keyword.Pattern);
        var patternOpen = Expect(SyntaxKind.OpenParen);
        var pattern = ParseRowPattern();
        var patternClose = Expect(SyntaxKind.CloseParen);
        SyntaxToken? subsetKeyword = null;
        IReadOnlyList<PatternSubset> subsets = [];
        if (IdentifierEquals(Keyword.Subset))
        {
            subsetKeyword = Advance();
            subsets = ParseSubsets();
        }

        var defineKeyword = ExpectIdent(Keyword.Define);
        var definitions = ParseDefinitions();
        var closeParen = Expect(SyntaxKind.CloseParen);
        ParseCorrelation(required: false, out var asKeyword, out var alias, out var columnOpen, out var columns, out var columnClose);
        var end = columnClose?.Span ?? alias?.Span ?? closeParen.Span;
        return new MatchRecognizeTable
        {
            Span = SourceSpan.From(input.Span, end),
            Input = input,
            MatchRecognizeKeyword = keyword,
            OpenParen = openParen,
            CloseParen = closeParen,
            PartitionKeyword = partition,
            PartitionBy = partitionBy,
            PartitionByList = partitionList,
            OrderBy = orderBy,
            MeasuresKeyword = measuresKeyword,
            Measures = measures,
            RowsKind = rowsKind,
            RowOrRows = rowOrRows,
            PerKeyword = perKeyword,
            MatchKeyword = matchKeyword,
            EmptyHandling = emptyHandling,
            AfterKeyword = afterKeyword,
            SkipKeyword = skipKeyword,
            SkipTo = skipTo,
            SkipPosition = skipPosition,
            SkipTarget = skipTarget,
            PatternKeyword = patternKeyword,
            PatternOpen = patternOpen,
            Pattern = pattern,
            PatternClose = patternClose,
            SubsetKeyword = subsetKeyword,
            Subsets = subsets,
            DefineKeyword = defineKeyword,
            Definitions = definitions,
            AsKeyword = asKeyword,
            Alias = alias,
            ColumnOpenParen = columnOpen,
            Columns = columns,
            ColumnCloseParen = columnClose,
        };
    }

    private List<PatternMeasure> ParseMeasures()
    {
        var measures = new List<PatternMeasure> { ParseMeasure() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            measures.Add(ParseMeasure());
        }

        return measures;
    }

    private PatternMeasure ParseMeasure()
    {
        SyntaxToken? semantics = null;
        if (IdentifierEquals(Keyword.Running) || IdentifierEquals(Keyword.Final))
        {
            semantics = Advance();
        }

        var expression = ParseExpression();
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var name = Expect(SyntaxKind.Identifier);
        var span = semantics is SyntaxToken semanticsToken
            ? SourceSpan.From(semanticsToken, name)
            : SourceSpan.From(expression.Span, name);
        return new PatternMeasure
        {
            Span = span,
            Semantics = semantics,
            Expression = expression,
            AsKeyword = asKeyword,
            Name = name,
        };
    }

    private void ParseRowsPerMatch(
        out SyntaxToken? rowsKind,
        out SyntaxToken? rowOrRows,
        out SyntaxToken? perKeyword,
        out SyntaxToken? matchKeyword,
        out SyntaxToken? emptyHandling)
    {
        rowsKind = null;
        rowOrRows = null;
        perKeyword = null;
        matchKeyword = null;
        emptyHandling = null;
        if (!IdentifierEquals(Keyword.One) && _current.Kind != SyntaxKind.AllKeyword)
        {
            return;
        }

        rowsKind = Advance();
        rowOrRows = rowsKind.Value.Kind == SyntaxKind.AllKeyword
            ? ExpectIdent(Keyword.Rows)
            : ExpectIdent(Keyword.Row);
        perKeyword = ExpectIdent(Keyword.Per);
        matchKeyword = Expect(SyntaxKind.MatchKeyword);
        if (IdentifierEquals(Keyword.Show) || IdentifierEquals(Keyword.Omit))
        {
            emptyHandling = Advance();
            ExpectIdent(Keyword.Empty);
            ExpectIdent(Keyword.Matches);
        }
        else if (_current.Kind == SyntaxKind.WithKeyword)
        {
            emptyHandling = Advance();
            ExpectIdent(Keyword.Unmatched);
            ExpectIdent(Keyword.Rows);
        }
    }

    private void ParseSkip(
        out SyntaxToken? afterKeyword,
        out SyntaxToken? skipKeyword,
        out SyntaxToken? skipTo,
        out SyntaxToken? skipPosition,
        out SyntaxToken? skipTarget)
    {
        afterKeyword = null;
        skipKeyword = null;
        skipTo = null;
        skipPosition = null;
        skipTarget = null;
        if (!IdentifierEquals(Keyword.After))
        {
            return;
        }

        afterKeyword = Advance();
        Expect(SyntaxKind.MatchKeyword);
        skipKeyword = ExpectIdent(Keyword.Skip);
        if (IdentifierEquals(Keyword.Past))
        {
            skipTo = Advance();
            skipPosition = ExpectIdent(Keyword.Last);
            skipTarget = ExpectIdent(Keyword.Row);
            return;
        }

        if (!IdentifierEquals(Keyword.To))
        {
            throw new SqlParseException($"Expected PAST or TO, found {_current.Kind}", _current.Position);
        }

        skipTo = Advance();
        if (IdentifierEquals(Keyword.Next))
        {
            skipPosition = Advance();
            skipTarget = ExpectIdent(Keyword.Row);
            return;
        }

        if (IdentifierEquals(Keyword.First) || IdentifierEquals(Keyword.Last))
        {
            skipPosition = Advance();
        }

        skipTarget = Expect(SyntaxKind.Identifier);
    }

    private List<PatternSubset> ParseSubsets()
    {
        var subsets = new List<PatternSubset> { ParseSubset() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            subsets.Add(ParseSubset());
        }

        return subsets;
    }

    private PatternSubset ParseSubset()
    {
        var name = Expect(SyntaxKind.Identifier);
        var equals = Expect(SyntaxKind.EqualsToken);
        var openParen = Expect(SyntaxKind.OpenParen);
        var variables = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            variables.Add(Expect(SyntaxKind.Identifier));
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PatternSubset
        {
            Span = SourceSpan.From(name, closeParen),
            Name = name,
            EqualsToken = equals,
            OpenParen = openParen,
            Variables = variables,
            CloseParen = closeParen,
        };
    }

    private List<PatternDefinition> ParseDefinitions()
    {
        var definitions = new List<PatternDefinition> { ParseDefinition() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            definitions.Add(ParseDefinition());
        }

        return definitions;
    }

    private PatternDefinition ParseDefinition()
    {
        var name = Expect(SyntaxKind.Identifier);
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var condition = ParseExpression();
        return new PatternDefinition
        {
            Span = SourceSpan.From(name, condition.Span),
            Name = name,
            AsKeyword = asKeyword,
            Condition = condition,
        };
    }

    private RowPattern ParseRowPattern()
    {
        var first = ParsePatternTerm();
        if (_current.Kind != SyntaxKind.BarToken)
        {
            return first;
        }

        var terms = new List<RowPattern> { first };
        while (_current.Kind == SyntaxKind.BarToken)
        {
            Advance();
            terms.Add(ParsePatternTerm());
        }

        return new PatternAlternation
        {
            Span = SourceSpan.From(first.Span, terms[^1].Span),
            Terms = terms,
        };
    }

    private RowPattern ParsePatternTerm()
    {
        var first = ParsePatternFactor();
        if (IsPatternEnd() || _current.Kind == SyntaxKind.BarToken)
        {
            return first;
        }

        var factors = new List<RowPattern> { first };
        while (!IsPatternEnd() && _current.Kind != SyntaxKind.BarToken)
        {
            factors.Add(ParsePatternFactor());
        }

        return new PatternConcatenation
        {
            Span = SourceSpan.From(first.Span, factors[^1].Span),
            Factors = factors,
        };
    }

    private RowPattern ParsePatternFactor()
    {
        var primary = ParsePatternPrimary();
        if (!IsQuantifier())
        {
            return primary;
        }

        var quantifier = Advance();
        SyntaxToken? low = null;
        SyntaxToken? comma = null;
        SyntaxToken? high = null;
        SyntaxToken? closeBrace = null;
        if (quantifier.Kind == SyntaxKind.OpenBrace)
        {
            if (_current.Kind == SyntaxKind.Number)
            {
                low = Advance();
            }

            if (_current.Kind == SyntaxKind.Comma)
            {
                comma = Advance();
            }

            if (_current.Kind == SyntaxKind.Number)
            {
                high = Advance();
            }

            closeBrace = Expect(SyntaxKind.CloseBrace);
        }

        SyntaxToken? reluctant = _current.Kind == SyntaxKind.QuestionMark ? Advance() : null;
        var end = reluctant?.Span ?? closeBrace?.Span ?? quantifier.Span;
        return new PatternQuantified
        {
            Span = SourceSpan.From(primary.Span, end),
            Primary = primary,
            Quantifier = quantifier,
            Low = low,
            Comma = comma,
            High = high,
            CloseBrace = closeBrace,
            Reluctant = reluctant,
        };
    }

    private RowPattern ParsePatternPrimary()
    {
        if (_current.Kind is SyntaxKind.CaretToken or SyntaxKind.DollarToken)
        {
            var token = Advance();
            return new PatternPrimary { Span = token.Span, Token = token };
        }

        if (_current.Kind == SyntaxKind.OpenParen)
        {
            var openParen = Advance();
            var pattern = ParseRowPattern();
            var closeParen = Expect(SyntaxKind.CloseParen);
            return new PatternGroup
            {
                Span = SourceSpan.From(openParen, closeParen),
                OpenParen = openParen,
                Pattern = pattern,
                CloseParen = closeParen,
            };
        }

        if (_current.Kind == SyntaxKind.OpenBrace)
        {
            var openBrace = Advance();
            var minus = Expect(SyntaxKind.MinusToken);
            var pattern = ParseRowPattern();
            var closeMinus = Expect(SyntaxKind.MinusToken);
            var closeBrace = Expect(SyntaxKind.CloseBrace);
            return new PatternExclusion
            {
                Span = SourceSpan.From(openBrace, closeBrace),
                OpenBrace = openBrace,
                Minus = minus,
                Pattern = pattern,
                CloseMinus = closeMinus,
                CloseBrace = closeBrace,
            };
        }

        if (IdentifierEquals(Keyword.Permute))
        {
            var permute = Advance();
            var openParen = Expect(SyntaxKind.OpenParen);
            var patterns = new List<RowPattern> { ParseRowPattern() };
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                patterns.Add(ParseRowPattern());
            }

            var closeParen = Expect(SyntaxKind.CloseParen);
            return new PatternPermute
            {
                Span = SourceSpan.From(permute, closeParen),
                PermuteKeyword = permute,
                OpenParen = openParen,
                Patterns = patterns,
                CloseParen = closeParen,
            };
        }

        if (_current.Kind != SyntaxKind.Identifier)
        {
            throw new SqlParseException($"Expected pattern variable, found {_current.Kind}", _current.Position);
        }

        var name = Advance();
        return new PatternPrimary { Span = name.Span, Token = name };
    }

    private bool IsPatternEnd() =>
        _current.Kind is SyntaxKind.CloseParen or SyntaxKind.Comma or SyntaxKind.EndOfFile
        || (_current.Kind == SyntaxKind.MinusToken && NextKind == SyntaxKind.CloseBrace);

    private bool IsQuantifier() =>
        _current.Kind is SyntaxKind.Star or SyntaxKind.PlusToken or SyntaxKind.QuestionMark or SyntaxKind.OpenBrace;
}
