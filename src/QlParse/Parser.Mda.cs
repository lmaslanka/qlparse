namespace QlParse;

internal sealed partial class Parser
{
    private bool IsMdarrayConstructor() =>
        IdentifierEquals(Keyword.Mdarray)
        && (NextKind == SyntaxKind.OpenBracket
            || (NextKind == SyntaxKind.OpenParen
                && _index + 1 < _tokens.Count
                && IsQueryStart(_tokens[_index + 1].Kind)));

    private bool IsMdarrayAggregate() =>
        NextKind == SyntaxKind.OpenParen
        && (IdentifierEquals(Keyword.MdarrayAgg)
            || IdentifierEquals(Keyword.MdarraySum)
            || IdentifierEquals(Keyword.MdarrayMax)
            || IdentifierEquals(Keyword.MdarrayMin)
            || IdentifierEquals(Keyword.MdarrayAvg));

    private MdarrayConstructorExpression ParseMdarrayConstructor()
    {
        var keyword = Advance();
        SyntaxToken? openBracket = null;
        IReadOnlyList<MdarrayDimension> dimensions = [];
        SyntaxToken? closeBracket = null;
        if (_current.Kind == SyntaxKind.OpenBracket)
        {
            openBracket = Advance();
            var items = new List<MdarrayDimension> { ParseMdarrayDimension() };
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                items.Add(ParseMdarrayDimension());
            }

            dimensions = items;
            closeBracket = Expect(SyntaxKind.CloseBracket);
        }

        var openParen = Expect(SyntaxKind.OpenParen);
        Query? query = null;
        IReadOnlyList<Expression> elements = [];
        if (IsQueryStart(_current.Kind))
        {
            query = ParseQuery();
        }
        else if (_current.Kind != SyntaxKind.CloseParen)
        {
            elements = ParseExpressionList();
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new MdarrayConstructorExpression
        {
            Span = SourceSpan.From(keyword, closeParen),
            MdarrayKeyword = keyword,
            OpenBracket = openBracket,
            Dimensions = dimensions,
            CloseBracket = closeBracket,
            OpenParen = openParen,
            Elements = elements,
            Query = query,
            CloseParen = closeParen,
        };
    }

    private MdarrayAggregateExpression ParseMdarrayAggregate()
    {
        var name = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var argument = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new MdarrayAggregateExpression
        {
            Span = SourceSpan.From(name, closeParen),
            Name = name,
            OpenParen = openParen,
            Argument = argument,
            CloseParen = closeParen,
        };
    }

    private bool IsMdarraySlice()
    {
        var depth = 1;
        for (var index = _index; index < _tokens.Count; index++)
        {
            var kind = _tokens[index].Kind;
            if (kind == SyntaxKind.OpenBracket)
            {
                depth++;
                continue;
            }

            if (kind == SyntaxKind.CloseBracket)
            {
                depth--;
                if (depth == 0)
                {
                    return false;
                }

                continue;
            }

            if (depth == 1 && kind is SyntaxKind.ColonToken or SyntaxKind.Star or SyntaxKind.Comma)
            {
                return true;
            }
        }

        return false;
    }

    private MdarraySliceExpression ParseMdarraySlice(Expression target)
    {
        var openBracket = Advance();
        var axes = new List<MdarrayAxis> { ParseMdarrayAxis() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            axes.Add(ParseMdarrayAxis());
        }

        var closeBracket = Expect(SyntaxKind.CloseBracket);
        return new MdarraySliceExpression
        {
            Span = SourceSpan.From(target.Span, closeBracket),
            Target = target,
            OpenBracket = openBracket,
            Axes = axes,
            CloseBracket = closeBracket,
        };
    }

    private MdarrayAxis ParseMdarrayAxis()
    {
        if (_current.Kind == SyntaxKind.Star)
        {
            var star = Advance();
            SyntaxToken? colon = null;
            Expression? upper = null;
            if (_current.Kind == SyntaxKind.ColonToken)
            {
                colon = Advance();
                upper = _current.Kind == SyntaxKind.Star ? null : ParseExpression(ComparisonBindingPower + 1);
                if (upper is null)
                {
                    Advance();
                }
            }

            var end = upper?.Span ?? colon?.Span ?? star.Span;
            return new MdarrayAxis
            {
                Span = SourceSpan.From(star.Span, end),
                Colon = colon,
                Upper = upper,
                Star = star,
            };
        }

        var lower = ParseExpression(ComparisonBindingPower + 1);
        if (_current.Kind != SyntaxKind.ColonToken)
        {
            return new MdarrayAxis { Span = lower.Span, Lower = lower };
        }

        var colonToken = Advance();
        if (_current.Kind == SyntaxKind.Star)
        {
            var star = Advance();
            return new MdarrayAxis
            {
                Span = SourceSpan.From(lower.Span, star),
                Lower = lower,
                Colon = colonToken,
                Star = star,
            };
        }

        var high = ParseExpression(ComparisonBindingPower + 1);
        return new MdarrayAxis
        {
            Span = SourceSpan.From(lower.Span, high.Span),
            Lower = lower,
            Colon = colonToken,
            Upper = high,
        };
    }
}
