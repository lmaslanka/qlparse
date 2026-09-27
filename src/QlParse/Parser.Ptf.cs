namespace QlParse;

internal sealed partial class Parser
{
    internal bool IsPtfInvocation()
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

    internal PtfTable ParsePtf()
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
            Expect(SyntaxKind.OpenParen);
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

    private readonly record struct PtfAliasClause(SyntaxToken? AsKeyword, SyntaxToken? Alias);

    private readonly record struct PtfPartitionClause(
        SyntaxToken? PartitionKeyword,
        SyntaxToken? PartitionByKeyword,
        IReadOnlyList<Expression> PartitionList);

    private readonly record struct PtfPruneClause(SyntaxToken? PruneKeyword, Expression? PruneWhen);

    private PtfArgument ParsePtfTableOrRow()
    {
        var kind = Advance();
        Expect(SyntaxKind.OpenParen);
        var (query, values) = ParsePtfTableOrRowBody(kind);
        var closeParen = Expect(SyntaxKind.CloseParen);
        var aliasClause = ParsePtfAliasClause();
        var partitionClause = ParsePtfPartitionClause();
        var orderBy = _current.Kind == SyntaxKind.OrderKeyword ? ParseOrderBy() : null;
        var pruneClause = ParsePtfPruneClause();
        var semantics = ParseSemantics(out var semanticsKeyword);
        var end = semanticsKeyword?.Span
            ?? pruneClause.PruneWhen?.Span
            ?? orderBy?.Span
            ?? (partitionClause.PartitionList.Count > 0
                ? partitionClause.PartitionList[^1].Span
                : aliasClause.Alias?.Span ?? closeParen.Span);
        return new PtfArgument
        {
            Span = SourceSpan.From(kind.Span, end),
            KindKeyword = kind,
            Query = query,
            Values = values,
            AsKeyword = aliasClause.AsKeyword,
            Alias = aliasClause.Alias,
            PartitionKeyword = partitionClause.PartitionKeyword,
            PartitionBy = partitionClause.PartitionByKeyword,
            PartitionByList = partitionClause.PartitionList,
            OrderBy = orderBy,
            PruneKeyword = pruneClause.PruneKeyword,
            Prune = pruneClause.PruneWhen,
            SemanticsKeyword = semanticsKeyword,
            Names = semantics is SyntaxToken semanticsToken ? [semanticsToken] : [],
        };
    }

    private (Query? Query, IReadOnlyList<Expression> Values) ParsePtfTableOrRowBody(SyntaxToken kind)
    {
        if (TokenEquals(kind, Keyword.Table))
        {
            return (ParseQuery(), []);
        }

        if (_current.Kind != SyntaxKind.CloseParen)
        {
            return (null, ParseExpressionList());
        }

        return (null, []);
    }

    private PtfAliasClause ParsePtfAliasClause()
    {
        if (_current.Kind != SyntaxKind.AsKeyword)
        {
            return default;
        }

        var asKeyword = Advance();
        var alias = Expect(SyntaxKind.Identifier);
        return new PtfAliasClause(asKeyword, alias);
    }

    private PtfPartitionClause ParsePtfPartitionClause()
    {
        if (!IdentifierEquals(Keyword.Partition))
        {
            return new PtfPartitionClause(null, null, []);
        }

        var partition = Advance();
        var partitionBy = Expect(SyntaxKind.ByKeyword);
        var partitionList = ParseExpressionList();
        return new PtfPartitionClause(partition, partitionBy, partitionList);
    }

    private PtfPruneClause ParsePtfPruneClause()
    {
        if (!IdentifierEquals(Keyword.Prune))
        {
            return default;
        }

        var prune = Advance();
        Expect(SyntaxKind.WhenKeyword);
        var pruneWhen = ParseExpression();
        return new PtfPruneClause(prune, pruneWhen);
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
