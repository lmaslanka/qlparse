namespace QlParse;

internal sealed partial class Parser
{
    private TableSource ParseTableSource()
    {
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            return IsQueryStart(NextKind) ? ParseDerivedTable() : ParseJoinedTable();
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

        return new JoinedTable
        {
            OpenParen = openParen,
            Table = table,
            Joins = joins,
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private DerivedTable ParseDerivedTable()
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
        return new DerivedTable
        {
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

    private TableReference ParseTableReference()
    {
        var nameParts = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        while (_current.Kind == SyntaxKind.Dot)
        {
            Advance();
            nameParts.Add(Expect(SyntaxKind.Identifier));
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

        return new TableReference
        {
            NameParts = nameParts,
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
            extras.Add(new CommaFrom
            {
                Comma = Advance(),
                Table = ParseTableSource(),
                Joins = ParseJoins(),
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

        return new JoinClause
        {
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

            return new OnConstraint
            {
                OnKeyword = Advance(),
                Condition = ParseExpression(),
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

        return new UsingConstraint
        {
            UsingKeyword = usingKeyword,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = Expect(SyntaxKind.CloseParen),
        };
    }

    private bool IsJoinStart() =>
        _current.Kind is SyntaxKind.NaturalKeyword or SyntaxKind.InnerKeyword or SyntaxKind.LeftKeyword
            or SyntaxKind.RightKeyword or SyntaxKind.FullKeyword or SyntaxKind.CrossKeyword
            or SyntaxKind.JoinKeyword
        || (_current.Kind == SyntaxKind.UnionKeyword && NextKind == SyntaxKind.JoinKeyword);
}
