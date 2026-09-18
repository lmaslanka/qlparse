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
        var ctes = new List<CommonTableExpression> { ParseCte() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            ctes.Add(ParseCte());
        }

        return new WithQuery
        {
            WithKeyword = withKeyword,
            RecursiveKeyword = recursive,
            Ctes = ctes,
            Query = ParseSetOp(),
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

        return new CommonTableExpression
        {
            Name = name,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = closeParen,
            AsKeyword = Expect(SyntaxKind.AsKeyword),
            OpenQuery = Expect(SyntaxKind.OpenParen),
            Query = ParseQuery(),
            CloseQuery = Expect(SyntaxKind.CloseParen),
        };
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

            left = new SetOperation
            {
                Left = left,
                Operator = op,
                AllKeyword = all,
                CorrespondingKeyword = corresponding,
                ByKeyword = byKeyword,
                OpenParen = openParen,
                Columns = columns,
                CloseParen = closeParen,
                Right = ParseQuery(bindingPower + 1),
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
            ValuesKeyword = valuesKeyword,
            Rows = rows,
        };
    }

    private ValuesRow ParseValuesRow()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var values = ParseExpressionList();
        return new ValuesRow
        {
            OpenParen = openParen,
            Values = values,
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private ParenQuery ParseParenQuery()
    {
        return new ParenQuery
        {
            OpenParen = Expect(SyntaxKind.OpenParen),
            Inner = ParseQuery(),
            CloseParen = Expect(SyntaxKind.CloseParen),
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
