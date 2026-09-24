namespace QlParse;

internal sealed partial class Parser
{
    private ArrayQueryExpression ParseArrayQuery()
    {
        var arrayKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var query = ParseCollectionSubquery();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new ArrayQueryExpression
        {
            Span = SourceSpan.From(arrayKeyword, closeParen),
            ArrayKeyword = arrayKeyword,
            OpenParen = openParen,
            Query = query,
            CloseParen = closeParen,
        };
    }

    private MultisetExpression ParseMultiset()
    {
        var multisetKeyword = Advance();
        var openBracket = Expect(SyntaxKind.OpenBracket);
        IReadOnlyList<Expression> elements = _current.Kind == SyntaxKind.CloseBracket
            ? []
            : ParseExpressionList();
        var closeBracket = Expect(SyntaxKind.CloseBracket);
        return new MultisetExpression
        {
            Span = SourceSpan.From(multisetKeyword, closeBracket),
            MultisetKeyword = multisetKeyword,
            OpenBracket = openBracket,
            Elements = elements,
            CloseBracket = closeBracket,
        };
    }

    private MultisetQueryExpression ParseMultisetQuery()
    {
        var keyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var query = ParseCollectionSubquery();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new MultisetQueryExpression
        {
            Span = SourceSpan.From(keyword, closeParen),
            Keyword = keyword,
            OpenParen = openParen,
            Query = query,
            CloseParen = closeParen,
        };
    }

    private Query ParseCollectionSubquery()
    {
        if (!IsQueryStart(_current.Kind))
        {
            throw new SqlParseException($"Expected query, found {_current.Kind}", _current.Position);
        }

        return ParseQuery();
    }

    private bool IsTableConstructor() =>
        IdentifierEquals(Keyword.Table)
        && NextKind == SyntaxKind.OpenParen
        && _index + 1 < _tokens.Count
        && IsQueryStart(_tokens[_index + 1].Kind);

    private MultisetSetExpression ParseMultisetSet()
    {
        var setKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var expression = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new MultisetSetExpression
        {
            Span = SourceSpan.From(setKeyword, closeParen),
            SetKeyword = setKeyword,
            OpenParen = openParen,
            Expression = expression,
            CloseParen = closeParen,
        };
    }

    private SpecialFormExpression ParseCollectionFunction()
    {
        var name = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var argument = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new SpecialFormExpression
        {
            Span = SourceSpan.From(name, closeParen),
            Name = name,
            OpenParen = openParen,
            Arguments = [argument],
            CloseParen = closeParen,
        };
    }

    private AbsentOnNullExpression ParseAbsentOnNull()
    {
        var absentKeyword = Advance();
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var nullKeyword = Expect(SyntaxKind.NullKeyword);
        return new AbsentOnNullExpression
        {
            Span = SourceSpan.From(absentKeyword, nullKeyword),
            AbsentKeyword = absentKeyword,
            OnKeyword = onKeyword,
            NullKeyword = nullKeyword,
        };
    }

    private int MultisetOpBindingPower()
    {
        if (_current.Kind != SyntaxKind.MultisetKeyword)
        {
            return 0;
        }

        return NextKind switch
        {
            SyntaxKind.UnionKeyword or SyntaxKind.ExceptKeyword => MultisetUnionBindingPower,
            SyntaxKind.IntersectKeyword => MultisetIntersectBindingPower,
            _ => 0,
        };
    }

    private MultisetOperationExpression ParseMultisetOp(Expression left, int bindingPower)
    {
        var multisetKeyword = Advance();
        var op = Advance();
        var quantifier = _current.Kind is SyntaxKind.AllKeyword or SyntaxKind.DistinctKeyword
            ? Advance()
            : (SyntaxToken?)null;
        var right = ParseExpression(bindingPower + 1);
        return new MultisetOperationExpression
        {
            Span = SourceSpan.From(left.Span, right.Span),
            Left = left,
            MultisetKeyword = multisetKeyword,
            Operator = op,
            Quantifier = quantifier,
            Right = right,
        };
    }

    private UnnestTable ParseUnnest()
    {
        var unnestKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var expressions = ParseExpressionList();
        var closeParen = Expect(SyntaxKind.CloseParen);
        SyntaxToken? withKeyword = null;
        SyntaxToken? ordinality = null;
        if (_current.Kind == SyntaxKind.WithKeyword && NextIsOrdinality())
        {
            withKeyword = Advance();
            ordinality = Advance();
        }

        SyntaxToken? asKeyword = null;
        SyntaxToken? alias = null;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            alias = Expect(SyntaxKind.Identifier);
        }
        else if (_current.Kind == SyntaxKind.Identifier)
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

        var end = columnClose ?? alias ?? ordinality ?? closeParen;
        return new UnnestTable
        {
            Span = SourceSpan.From(unnestKeyword, end),
            UnnestKeyword = unnestKeyword,
            OpenParen = openParen,
            Expressions = expressions,
            CloseParen = closeParen,
            WithKeyword = withKeyword,
            OrdinalityKeyword = ordinality,
            AsKeyword = asKeyword,
            Alias = alias,
            ColumnOpenParen = columnOpen,
            Columns = columns,
            ColumnCloseParen = columnClose,
        };
    }

    private bool NextIsOrdinality() =>
        NextKind == SyntaxKind.Identifier
        && _index < _tokens.Count
        && TokenEquals(_tokens[_index], Keyword.Ordinality);
}
