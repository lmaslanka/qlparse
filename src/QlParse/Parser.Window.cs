namespace QlParse;

internal sealed partial class Parser
{
    private WindowSpecification ParseOver()
    {
        var overKeyword = Expect(SyntaxKind.OverKeyword);
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            var name = Expect(SyntaxKind.Identifier);
            return new WindowSpecification
            {
                Span = SourceSpan.From(overKeyword, name),
                OverKeyword = overKeyword,
                Name = name,
            };
        }

        var specification = ParseWindowSpecification();
        return new WindowSpecification
        {
            Span = SourceSpan.From(overKeyword, specification.Span),
            OverKeyword = overKeyword,
            OpenParen = specification.OpenParen,
            Name = specification.Name,
            PartitionKeyword = specification.PartitionKeyword,
            PartitionByKeyword = specification.PartitionByKeyword,
            PartitionBy = specification.PartitionBy,
            OrderBy = specification.OrderBy,
            Frame = specification.Frame,
            CloseParen = specification.CloseParen,
        };
    }

    private WindowSpecification ParseWindowSpecification()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        SyntaxToken? name = null;
        if (_current.Kind == SyntaxKind.Identifier && !IsPartitionStart() && !IsFrameUnits())
        {
            name = Advance();
        }

        SyntaxToken? partitionKeyword = null;
        SyntaxToken? partitionBy = null;
        IReadOnlyList<Expression> partition = [];
        if (IsPartitionStart())
        {
            partitionKeyword = Advance();
            partitionBy = Expect(SyntaxKind.ByKeyword);
            partition = ParseExpressionList();
        }

        OrderByClause? orderBy = _current.Kind == SyntaxKind.OrderKeyword ? ParseOrderBy() : null;
        WindowFrame? frame = IsFrameUnits() ? ParseWindowFrame() : null;
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new WindowSpecification
        {
            Span = SourceSpan.From(openParen, closeParen),
            OpenParen = openParen,
            Name = name,
            PartitionKeyword = partitionKeyword,
            PartitionByKeyword = partitionBy,
            PartitionBy = partition,
            OrderBy = orderBy,
            Frame = frame,
            CloseParen = closeParen,
        };
    }

    private bool IsPartitionStart() =>
        IdentifierEquals(Keyword.Partition) && NextKind == SyntaxKind.ByKeyword;

    private bool IsFrameUnits() =>
        IdentifierEquals(Keyword.Rows) || IdentifierEquals(Keyword.Range) || IdentifierEquals(Keyword.Groups);

    private WindowFrame ParseWindowFrame()
    {
        var units = Advance();
        SyntaxToken? between = null;
        SyntaxToken? andKeyword = null;
        WindowFrameBound? end = null;
        WindowFrameBound start;
        if (_current.Kind == SyntaxKind.BetweenKeyword)
        {
            between = Advance();
            start = ParseFrameBound();
            andKeyword = Expect(SyntaxKind.AndKeyword);
            end = ParseFrameBound();
        }
        else
        {
            start = ParseFrameBound();
        }

        SyntaxToken? exclude = null;
        SyntaxToken? exclusion = null;
        SyntaxToken? exclusionTail = null;
        if (IdentifierEquals(Keyword.Exclude))
        {
            exclude = Advance();
            (exclusion, exclusionTail) = ParseFrameExclusion();
        }

        var endToken = exclusionTail ?? exclusion ?? end?.Endpoint ?? start.Endpoint;
        return new WindowFrame
        {
            Span = SourceSpan.From(units, endToken),
            Units = units,
            BetweenKeyword = between,
            Start = start,
            AndKeyword = andKeyword,
            End = end,
            ExcludeKeyword = exclude,
            Exclusion = exclusion,
            ExclusionTail = exclusionTail,
        };
    }

    private (SyntaxToken Exclusion, SyntaxToken? Tail) ParseFrameExclusion()
    {
        if (IdentifierEquals(Keyword.Current))
        {
            var current = Advance();
            if (!IdentifierEquals(Keyword.Row))
            {
                throw new SqlParseException($"Expected ROW, found {_current.Kind}", _current.Position);
            }

            return (current, Advance());
        }

        if (_current.Kind == SyntaxKind.GroupKeyword || IdentifierEquals(Keyword.Ties))
        {
            return (Advance(), null);
        }

        if (IdentifierEquals(Keyword.No))
        {
            var no = Advance();
            if (!IdentifierEquals(Keyword.Others))
            {
                throw new SqlParseException($"Expected OTHERS, found {_current.Kind}", _current.Position);
            }

            return (no, Advance());
        }

        throw new SqlParseException($"Expected frame exclusion, found {_current.Kind}", _current.Position);
    }

    private WindowFrameBound ParseFrameBound()
    {
        if (IdentifierEquals(Keyword.Unbounded))
        {
            var unbounded = Advance();
            var endpoint = ParseFrameEndpoint();
            return new WindowFrameBound
            {
                Span = SourceSpan.From(unbounded, endpoint),
                UnboundedKeyword = unbounded,
                Endpoint = endpoint,
            };
        }

        if (IdentifierEquals(Keyword.Current))
        {
            var current = Advance();
            if (!IdentifierEquals(Keyword.Row))
            {
                throw new SqlParseException($"Expected ROW, found {_current.Kind}", _current.Position);
            }

            var row = Advance();
            return new WindowFrameBound
            {
                Span = SourceSpan.From(current, row),
                CurrentKeyword = current,
                Endpoint = row,
            };
        }

        var offset = ParseExpression(ComparisonBindingPower + 1);
        var direction = ParseFrameEndpoint();
        return new WindowFrameBound
        {
            Span = SourceSpan.From(offset.Span, direction),
            Offset = offset,
            Endpoint = direction,
        };
    }

    private SyntaxToken ParseFrameEndpoint()
    {
        if (!IdentifierEquals(Keyword.Preceding) && !IdentifierEquals(Keyword.Following))
        {
            throw new SqlParseException($"Expected PRECEDING or FOLLOWING, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    private WindowClause ParseWindowClause()
    {
        var windowKeyword = Expect(SyntaxKind.WindowKeyword);
        var windows = new List<WindowDefinition> { ParseWindowDefinition() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            windows.Add(ParseWindowDefinition());
        }

        return new WindowClause
        {
            Span = SourceSpan.From(windowKeyword, windows[^1].Span),
            WindowKeyword = windowKeyword,
            Windows = windows,
        };
    }

    private WindowDefinition ParseWindowDefinition()
    {
        var name = Expect(SyntaxKind.Identifier);
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var specification = ParseWindowSpecification();
        return new WindowDefinition
        {
            Span = SourceSpan.From(name, specification.Span),
            Name = name,
            AsKeyword = asKeyword,
            Specification = specification,
        };
    }
}
