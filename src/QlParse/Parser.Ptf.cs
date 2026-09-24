namespace QlParse;

internal sealed partial class Parser
{
    private bool IsPtfInvocation()
    {
        if (_current.Kind != SyntaxKind.Identifier || NextKind != SyntaxKind.OpenParen)
        {
            return false;
        }

        var depth = 0;
        for (var index = _index; index < _tokens.Count; index++)
        {
            var token = _tokens[index];
            if (token.Kind == SyntaxKind.OpenParen)
            {
                depth++;
                continue;
            }

            if (token.Kind == SyntaxKind.CloseParen)
            {
                depth--;
                if (depth == 0)
                {
                    return false;
                }

                continue;
            }

            if (depth == 1 && IsPtfMarker(token))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPtfMarker(SyntaxToken token) =>
        TokenEquals(token, Keyword.Table)
        || TokenEquals(token, Keyword.Row)
        || TokenEquals(token, Keyword.Copartition)
        || TokenEquals(token, Keyword.Partition)
        || TokenEquals(token, Keyword.Prune)
        || TokenEquals(token, Keyword.Semantics);

    private PtfTable ParsePtf()
    {
        var name = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var arguments = new List<PtfArgument>();
        if (_current.Kind != SyntaxKind.CloseParen)
        {
            arguments.Add(ParsePtfArgument());
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                arguments.Add(ParsePtfArgument());
            }
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        ParseCorrelation(required: false, out var asKeyword, out var alias, out _, out _, out var columnClose);
        var end = columnClose?.Span ?? alias?.Span ?? closeParen.Span;
        return new PtfTable
        {
            Span = SourceSpan.From(name.Span, end),
            Name = name,
            OpenParen = openParen,
            CloseParen = closeParen,
            Arguments = arguments,
            AsKeyword = asKeyword,
            Alias = alias,
        };
    }

    private PtfArgument ParsePtfArgument()
    {
        if (IdentifierEquals(Keyword.Copartition))
        {
            var copartition = Advance();
            var openParen = Expect(SyntaxKind.OpenParen);
            var names = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                names.Add(Expect(SyntaxKind.Identifier));
            }

            var closeParen = Expect(SyntaxKind.CloseParen);
            return new PtfArgument
            {
                Span = SourceSpan.From(copartition, closeParen),
                CopartitionKeyword = copartition,
                Names = names,
            };
        }

        if ((IdentifierEquals(Keyword.Table) || IdentifierEquals(Keyword.Row)) && NextKind == SyntaxKind.OpenParen)
        {
            return ParsePtfTableOrRow();
        }

        var scalar = ParseExpression();
        var semantics = ParseSemantics(out var semanticsKeyword);
        var end = semanticsKeyword?.Span ?? scalar.Span;
        return new PtfArgument
        {
            Span = SourceSpan.From(scalar.Span, end),
            Scalar = scalar,
            KindKeyword = semantics,
            SemanticsKeyword = semanticsKeyword,
        };
    }

    private PtfArgument ParsePtfTableOrRow()
    {
        var kind = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        Query? query = null;
        IReadOnlyList<Expression> values = [];
        if (TokenEquals(kind, Keyword.Table))
        {
            query = ParseQuery();
        }
        else if (_current.Kind != SyntaxKind.CloseParen)
        {
            values = ParseExpressionList();
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        SyntaxToken? asKeyword = null;
        SyntaxToken? alias = null;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            alias = Expect(SyntaxKind.Identifier);
        }

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
        SyntaxToken? prune = null;
        Expression? pruneWhen = null;
        if (IdentifierEquals(Keyword.Prune))
        {
            prune = Advance();
            Expect(SyntaxKind.WhenKeyword);
            pruneWhen = ParseExpression();
        }

        var semantics = ParseSemantics(out var semanticsKeyword);
        var end = semanticsKeyword?.Span
            ?? pruneWhen?.Span
            ?? orderBy?.Span
            ?? (partitionList.Count > 0 ? partitionList[^1].Span : alias?.Span ?? closeParen.Span);
        return new PtfArgument
        {
            Span = SourceSpan.From(kind.Span, end),
            KindKeyword = kind,
            Query = query,
            Values = values,
            AsKeyword = asKeyword,
            Alias = alias,
            PartitionKeyword = partition,
            PartitionBy = partitionBy,
            PartitionByList = partitionList,
            OrderBy = orderBy,
            PruneKeyword = prune,
            Prune = pruneWhen,
            SemanticsKeyword = semanticsKeyword,
            Names = semantics is SyntaxToken semanticsToken ? [semanticsToken] : [],
        };
    }

    private SyntaxToken? ParseSemantics(out SyntaxToken? semanticsKeyword)
    {
        semanticsKeyword = null;
        if (!IdentifierEquals(Keyword.Row) && !IdentifierEquals(Keyword.Table))
        {
            return null;
        }

        if (!NextEquals(Keyword.Semantics))
        {
            return null;
        }

        var kind = Advance();
        semanticsKeyword = Advance();
        return kind;
    }
}
