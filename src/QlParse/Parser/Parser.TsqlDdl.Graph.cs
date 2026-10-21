namespace QlParse;

// SQL Server graph queries (workstream D): the MATCH predicate with node/edge chains, SHORTEST_PATH with
// `+` / `{1,n}` quantified groups and LAST_NODE(), `FOR PATH` FROM items, and `WITHIN GROUP (GRAPH PATH)`.
internal sealed partial class Parser
{
    // `MATCH (pattern [AND pattern ...])`.
    private TsqlMatchExpression ParseTsqlMatch()
    {
        var matchKeyword = Advance();
        var openParen = Advance();
        var patterns = new List<TsqlGraphPattern> { ParseTsqlGraphPattern() };
        var andKeywords = new List<SyntaxToken>();
        while (_current.Kind == SyntaxKind.AndKeyword)
        {
            andKeywords.Add(Advance());
            patterns.Add(ParseTsqlGraphPattern());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlMatchExpression
        {
            Span = SourceSpan.From(matchKeyword, closeParen),
            MatchKeyword = matchKeyword,
            OpenParen = openParen,
            Patterns = patterns,
            AndKeywords = andKeywords,
            CloseParen = closeParen,
        };
    }

    private TsqlGraphPattern ParseTsqlGraphPattern()
    {
        if (!IdentifierEquals(Keyword.TsqlDdlShortestPath) || NextKind != SyntaxKind.OpenParen)
        {
            var chain = ParseTsqlGraphElements();
            return new TsqlGraphPattern { Span = SourceSpan.From(chain[0].Span, chain[^1].Span), Elements = chain };
        }

        var shortestPathKeyword = Advance();
        var openParen = Advance();
        var elements = ParseTsqlGraphElements();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlGraphPattern
        {
            Span = SourceSpan.From(shortestPathKeyword, closeParen),
            ShortestPathKeyword = shortestPathKeyword,
            OpenParen = openParen,
            Elements = elements,
            CloseParen = closeParen,
        };
    }

    private List<TsqlGraphElement> ParseTsqlGraphElements()
    {
        var elements = new List<TsqlGraphElement>();
        while (TryParseTsqlGraphElement() is { } element)
        {
            elements.Add(element);
        }

        if (elements.Count == 0)
        {
            throw new SqlParseException($"Expected graph pattern, found {_current.Kind}", _current.Position);
        }

        return elements;
    }

    private TsqlGraphElement? TryParseTsqlGraphElement()
    {
        if (_current.Kind == SyntaxKind.MinusToken && NextKind == SyntaxKind.OpenParen)
        {
            return ParseTsqlGraphEdge(null);
        }

        if (_current.Kind == SyntaxKind.LessThan && NextKind == SyntaxKind.MinusToken)
        {
            return ParseTsqlGraphEdge(Advance());
        }

        if (_current.Kind == SyntaxKind.OpenParen)
        {
            return ParseTsqlGraphGroup();
        }

        return _current.Kind == SyntaxKind.Identifier ? ParseTsqlGraphNode() : null;
    }

    // `alias` or `LAST_NODE(alias)`.
    private TsqlGraphNode ParseTsqlGraphNode()
    {
        if (!IdentifierEquals(Keyword.TsqlDdlLastNode) || NextKind != SyntaxKind.OpenParen)
        {
            var name = Advance();
            return new TsqlGraphNode { Span = name.Span, Name = name };
        }

        var lastNodeKeyword = Advance();
        var openParen = Advance();
        var alias = Expect(SyntaxKind.Identifier);
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlGraphNode
        {
            Span = SourceSpan.From(lastNodeKeyword, closeParen),
            LastNodeKeyword = lastNodeKeyword,
            OpenParen = openParen,
            Name = alias,
            CloseParen = closeParen,
        };
    }

    // `-(edge)->` or, after a consumed `<`, `-(edge)-`.
    private TsqlGraphEdge ParseTsqlGraphEdge(SyntaxToken? lessThan)
    {
        var leadingDash = Expect(SyntaxKind.MinusToken);
        var openParen = Expect(SyntaxKind.OpenParen);
        var name = Expect(SyntaxKind.Identifier);
        var closeParen = Expect(SyntaxKind.CloseParen);
        var arrow = Expect(lessThan is null ? SyntaxKind.JsonArrowToken : SyntaxKind.MinusToken);
        return new TsqlGraphEdge
        {
            Span = SourceSpan.From(lessThan ?? leadingDash, arrow),
            LessThan = lessThan,
            LeadingDash = leadingDash,
            OpenParen = openParen,
            Name = name,
            CloseParen = closeParen,
            Arrow = arrow,
        };
    }

    // `(elements)+` or `(elements){1,n}` inside SHORTEST_PATH.
    private TsqlGraphGroup ParseTsqlGraphGroup()
    {
        var openParen = Advance();
        var elements = ParseTsqlGraphElements();
        var closeParen = Expect(SyntaxKind.CloseParen);
        IReadOnlyList<SyntaxToken> quantifier;
        if (_current.Kind == SyntaxKind.PlusToken)
        {
            quantifier = [Advance()];
        }
        else
        {
            var openBrace = Expect(SyntaxKind.OpenBrace);
            quantifier = [openBrace, Expect(SyntaxKind.Number), Expect(SyntaxKind.Comma), Expect(SyntaxKind.Number), Expect(SyntaxKind.CloseBrace)];
        }

        return new TsqlGraphGroup
        {
            Span = SourceSpan.From(openParen, quantifier[^1]),
            OpenParen = openParen,
            Elements = elements,
            CloseParen = closeParen,
            Quantifier = quantifier,
        };
    }

    // `table FOR PATH [AS] alias` in FROM; FOR is current.
    internal TsqlGraphPathTable ParseTsqlGraphPathTable(IReadOnlyList<SyntaxToken> nameParts)
    {
        var forKeyword = Advance();
        var pathKeyword = Advance();
        var asKeyword = _current.Kind == SyntaxKind.AsKeyword ? Advance() : (SyntaxToken?)null;
        var alias = asKeyword is not null || IsBareTableAlias() ? Expect(SyntaxKind.Identifier) : (SyntaxToken?)null;
        return new TsqlGraphPathTable
        {
            Span = SourceSpan.From(nameParts[0], alias ?? pathKeyword),
            NameParts = nameParts,
            ForKeyword = forKeyword,
            PathKeyword = pathKeyword,
            AsKeyword = asKeyword,
            Alias = alias,
        };
    }

    internal bool IsTsqlGraphPathWithinGroup() => IsWithinGroup() && IsKeywordAt(_index + Len2, Keyword.Graph);

    // `function(args) WITHIN GROUP (GRAPH PATH)`; WITHIN is current.
    internal TsqlGraphPathAggregateExpression ParseTsqlGraphPathAggregate(
        SyntaxToken name,
        SyntaxToken openParen,
        IReadOnlyList<Expression> arguments,
        SyntaxToken closeParen)
    {
        var call = new FunctionCallExpression
        {
            Span = SourceSpan.From(name, closeParen),
            Name = name,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
        };
        var withinKeyword = Advance();
        var groupKeyword = Advance();
        var groupOpen = Advance();
        var graphKeyword = Advance();
        var pathKeyword = ExpectIdent(Keyword.Path);
        var groupClose = Expect(SyntaxKind.CloseParen);
        return new TsqlGraphPathAggregateExpression
        {
            Span = SourceSpan.From(name, groupClose),
            Call = call,
            WithinKeyword = withinKeyword,
            GroupKeyword = groupKeyword,
            OpenParen = groupOpen,
            GraphKeyword = graphKeyword,
            PathKeyword = pathKeyword,
            CloseParen = groupClose,
        };
    }
}
