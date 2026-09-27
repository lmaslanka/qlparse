namespace QlParse;

internal sealed partial class Parser
{
    internal bool IsGraphTable() =>
        TokenEquals(_current, Keyword.GraphTable) && NextKind == SyntaxKind.OpenParen;

    internal CreatePropertyGraphStatement ParseCreatePropertyGraph()
    {
        var createKeyword = Advance();
        var propertyKeyword = Advance();
        var graphKeyword = ExpectIdent(Keyword.Graph);
        var name = ParseQualifiedName();
        var vertexKeyword = ExpectIdent(Keyword.Vertex);
        ExpectIdent(Keyword.Tables);
        var vertices = ParseGraphElements();
        SyntaxToken? edgeKeyword = null;
        IReadOnlyList<GraphTableElement> edges = [];
        if (IdentifierEquals(Keyword.Edge))
        {
            edgeKeyword = Advance();
            ExpectIdent(Keyword.Tables);
            edges = ParseGraphElements();
        }

        var end = edges.Count > 0 ? edges[^1].Span : vertices[^1].Span;
        return new CreatePropertyGraphStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            PropertyKeyword = propertyKeyword,
            GraphKeyword = graphKeyword,
            Name = name,
            VertexKeyword = vertexKeyword,
            Vertices = vertices,
            EdgeKeyword = edgeKeyword,
            Edges = edges,
        };
    }

    internal DropPropertyGraphStatement ParseDropPropertyGraph()
    {
        var dropKeyword = Advance();
        var propertyKeyword = Advance();
        var graphKeyword = ExpectIdent(Keyword.Graph);
        var name = ParseQualifiedName();
        SyntaxToken? behavior = null;
        if (IdentifierEquals(Keyword.Cascade) || IdentifierEquals(Keyword.Restrict))
        {
            behavior = Advance();
        }

        var end = behavior?.Span ?? name[^1].Span;
        return new DropPropertyGraphStatement
        {
            Span = SourceSpan.From(dropKeyword, end),
            DropKeyword = dropKeyword,
            PropertyKeyword = propertyKeyword,
            GraphKeyword = graphKeyword,
            Name = name,
            Behavior = behavior,
        };
    }

    private List<GraphTableElement> ParseGraphElements()
    {
        Expect(SyntaxKind.OpenParen);
        var elements = new List<GraphTableElement> { ParseGraphElement() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            elements.Add(ParseGraphElement());
        }

        Expect(SyntaxKind.CloseParen);
        return elements;
    }

    private readonly record struct GraphEndpointClause(
        SyntaxToken Endpoint,
        IReadOnlyList<SyntaxToken> Columns,
        IReadOnlyList<SyntaxToken> Reference);

    private readonly record struct GraphLabelClause(
        IReadOnlyList<SyntaxToken> Labels,
        IReadOnlyList<SyntaxToken> Properties);

    private GraphTableElement ParseGraphElement()
    {
        var name = ParseQualifiedName();
        SyntaxToken? key = null;
        IReadOnlyList<SyntaxToken> keyColumns = [];
        SyntaxToken? source = null;
        IReadOnlyList<SyntaxToken> sourceColumns = [];
        IReadOnlyList<SyntaxToken> sourceReference = [];
        SyntaxToken? destination = null;
        IReadOnlyList<SyntaxToken> destinationColumns = [];
        IReadOnlyList<SyntaxToken> destinationReference = [];
        var labels = new List<SyntaxToken>();
        var properties = new List<SyntaxToken>();
        var end = name[^1].Span;
        while (IdentifierEquals(Keyword.Key) || IdentifierEquals(Keyword.Source) || IdentifierEquals(Keyword.Destination)
            || IdentifierEquals(Keyword.Label))
        {
            if (IdentifierEquals(Keyword.Key))
            {
                key = Advance();
                keyColumns = ParseParenthesizedNames();
                end = keyColumns[^1].Span;
                continue;
            }

            if (IdentifierEquals(Keyword.Source) || IdentifierEquals(Keyword.Destination))
            {
                var endpointClause = ParseGraphEndpointClause();
                if (TokenEquals(endpointClause.Endpoint, Keyword.Source))
                {
                    source = endpointClause.Endpoint;
                    sourceColumns = endpointClause.Columns;
                    sourceReference = endpointClause.Reference;
                }
                else
                {
                    destination = endpointClause.Endpoint;
                    destinationColumns = endpointClause.Columns;
                    destinationReference = endpointClause.Reference;
                }

                end = endpointClause.Reference[^1].Span;
                continue;
            }

            var labelClause = ParseGraphLabelClause();
            labels.AddRange(labelClause.Labels);
            properties.AddRange(labelClause.Properties);
            end = properties.Count > 0 ? properties[^1].Span : labels[^1].Span;
        }

        return new GraphTableElement
        {
            Span = SourceSpan.From(name[0].Span, end),
            Name = name,
            KeyKeyword = key,
            KeyColumns = keyColumns,
            SourceKeyword = source,
            SourceColumns = sourceColumns,
            SourceReference = sourceReference,
            DestinationKeyword = destination,
            DestinationColumns = destinationColumns,
            DestinationReference = destinationReference,
            Labels = labels,
            Properties = properties,
        };
    }

    private GraphEndpointClause ParseGraphEndpointClause()
    {
        var endpoint = Advance();
        ExpectIdent(Keyword.Key);
        var columns = ParseParenthesizedNames();
        ExpectIdent(Keyword.References);
        var reference = ParseQualifiedName();
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            reference = [.. reference, .. ParseParenthesizedNames()];
        }

        return new GraphEndpointClause(endpoint, columns, reference);
    }

    private GraphLabelClause ParseGraphLabelClause()
    {
        var labels = new List<SyntaxToken> { Advance(), Expect(SyntaxKind.Identifier) };
        var properties = new List<SyntaxToken>();
        if (IdentifierEquals(Keyword.Properties))
        {
            properties.Add(Advance());
            if (_current.Kind == SyntaxKind.AllKeyword)
            {
                properties.Add(Advance());
            }
            else
            {
                properties.AddRange(ParseParenthesizedNames());
            }
        }

        return new GraphLabelClause(labels, properties);
    }

    private List<SyntaxToken> ParseParenthesizedNames()
    {
        Expect(SyntaxKind.OpenParen);
        var names = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            names.Add(Expect(SyntaxKind.Identifier));
        }

        Expect(SyntaxKind.CloseParen);
        return names;
    }

    internal GraphTable ParseGraphTable()
    {
        var keyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var graphName = ParseQualifiedName();
        var matchKeyword = Expect(SyntaxKind.MatchKeyword);
        var pattern = ParseGraphPattern();
        SyntaxToken? whereKeyword = null;
        Expression? where = null;
        if (_current.Kind == SyntaxKind.WhereKeyword)
        {
            whereKeyword = Advance();
            where = ParseExpression();
        }

        SyntaxToken? columnsKeyword = null;
        IReadOnlyList<Expression> columns = [];
        if (IdentifierEquals(Keyword.Columns))
        {
            columnsKeyword = Advance();
            Expect(SyntaxKind.OpenParen);
            columns = ParseGraphColumns();
            Expect(SyntaxKind.CloseParen);
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        ParseCorrelation(required: false, out var asKeyword, out var alias, out _, out _, out var columnClose);
        var end = columnClose?.Span ?? alias?.Span ?? closeParen.Span;
        return new GraphTable
        {
            Span = SourceSpan.From(keyword.Span, end),
            GraphTableKeyword = keyword,
            OpenParen = openParen,
            GraphName = graphName,
            MatchKeyword = matchKeyword,
            Pattern = pattern,
            WhereKeyword = whereKeyword,
            Where = where,
            ColumnsKeyword = columnsKeyword,
            Columns = columns,
            CloseParen = closeParen,
            AsKeyword = asKeyword,
            Alias = alias,
        };
    }

    private List<Expression> ParseGraphColumns()
    {
        var columns = new List<Expression> { ParseExpression() };
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            Advance();
            Expect(SyntaxKind.Identifier);
        }

        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            columns.Add(ParseExpression());
            if (_current.Kind is SyntaxKind.AsKeyword)
            {
                Advance();
                Expect(SyntaxKind.Identifier);
            }
        }

        return columns;
    }

    private List<GraphElement> ParseGraphPattern()
    {
        var elements = ParseGraphPath();
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            elements.AddRange(ParseGraphPath());
        }

        return elements;
    }

    private List<GraphElement> ParseGraphPath()
    {
        var elements = new List<GraphElement> { ParseGraphVertexOrGroup() };
        while (IsGraphEdgeStart())
        {
            elements.Add(ParseGraphEdge());
            if (_current.Kind == SyntaxKind.OpenParen)
            {
                elements.Add(ParseGraphVertexOrGroup());
            }
        }

        return elements;
    }

    private GraphElement ParseGraphVertexOrGroup()
    {
        if (IsParenthesizedPath())
        {
            var openParen = Advance();
            var group = ParseGraphPath();
            var closeParen = Expect(SyntaxKind.CloseParen);
            var quantifier = ParseGraphQuantifier();
            var end = quantifier?.Span ?? closeParen.Span;
            return new GraphElement
            {
                Span = SourceSpan.From(openParen.Span, end),
                Group = group,
                Quantifier = quantifier,
            };
        }

        return ParseGraphVertex();
    }

    private GraphElement ParseGraphVertex()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        SyntaxToken? variable = null;
        if (_current.Kind == SyntaxKind.Identifier)
        {
            variable = Advance();
        }

        var labels = ParseGraphLabels();
        var properties = ParseGraphProperties();
        Expression? where = null;
        if (_current.Kind == SyntaxKind.WhereKeyword)
        {
            Advance();
            where = ParseExpression();
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        var quantifier = ParseGraphQuantifier();
        var end = quantifier?.Span ?? where?.Span ?? closeParen.Span;
        return new GraphElement
        {
            Span = SourceSpan.From(openParen.Span, end),
            Variable = variable,
            Labels = labels,
            Properties = properties,
            Where = where,
            Quantifier = quantifier,
        };
    }

    private List<SyntaxToken> ParseGraphLabels()
    {
        var labels = new List<SyntaxToken>();
        if (_current.Kind == SyntaxKind.ColonToken)
        {
            labels.Add(Advance());
            labels.Add(Expect(SyntaxKind.Identifier));
            while (_current.Kind == SyntaxKind.BarToken)
            {
                labels.Add(Advance());
                labels.Add(Expect(SyntaxKind.Identifier));
            }
        }
        else
        {
            if (_current.Kind == SyntaxKind.IsKeyword)
            {
                labels.Add(Advance());
                labels.Add(Expect(SyntaxKind.Identifier));
            }
        }

        return labels;
    }

    private List<GraphProperty> ParseGraphProperties()
    {
        if (_current.Kind != SyntaxKind.OpenBrace)
        {
            return [];
        }

        Advance();
        var properties = new List<GraphProperty>();
        if (_current.Kind != SyntaxKind.CloseBrace)
        {
            properties.Add(ParseGraphProperty());
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                properties.Add(ParseGraphProperty());
            }
        }

        Expect(SyntaxKind.CloseBrace);
        return properties;
    }

    private GraphProperty ParseGraphProperty()
    {
        var name = Expect(SyntaxKind.Identifier);
        var colon = Expect(SyntaxKind.ColonToken);
        var value = ParseExpression(ComparisonBindingPower + 1);
        return new GraphProperty
        {
            Span = SourceSpan.From(name, value.Span),
            Name = name,
            Colon = colon,
            Value = value,
        };
    }

    private bool IsGraphEdgeStart() =>
        _current.Kind == SyntaxKind.MinusToken
        || (_current.Kind == SyntaxKind.LessThan && NextKind == SyntaxKind.MinusToken);

    private GraphElement ParseGraphEdge()
    {
        var start = _current;
        if (_current.Kind == SyntaxKind.LessThan)
        {
            Advance();
        }

        Expect(SyntaxKind.MinusToken);
        Expect(SyntaxKind.OpenBracket);
        SyntaxToken? variable = null;
        if (_current.Kind == SyntaxKind.Identifier)
        {
            variable = Advance();
        }

        var labels = ParseGraphLabels();
        Expect(SyntaxKind.CloseBracket);
        if (_current.Kind == SyntaxKind.JsonArrowToken)
        {
            Advance();
        }
        else
        {
            Expect(SyntaxKind.MinusToken);
            if (_current.Kind == SyntaxKind.GreaterThan)
            {
                Advance();
            }
        }

        var quantifier = ParseGraphQuantifier();
        var end = quantifier?.Span ?? _tokens[_index - 1].Span;
        return new GraphElement
        {
            Span = SourceSpan.From(start.Span, end),
            Variable = variable,
            Labels = labels,
            Edge = true,
            Quantifier = quantifier,
        };
    }

    private SyntaxToken? ParseGraphQuantifier()
    {
        if (_current.Kind is SyntaxKind.Star or SyntaxKind.PlusToken or SyntaxKind.QuestionMark)
        {
            return Advance();
        }

        if (_current.Kind != SyntaxKind.OpenBrace)
        {
            return null;
        }

        Advance();
        if (_current.Kind == SyntaxKind.Number)
        {
            Advance();
        }

        if (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            if (_current.Kind is SyntaxKind.Number)
            {
                Advance();
            }
        }

        return Expect(SyntaxKind.CloseBrace);
    }

    private bool IsParenthesizedPath()
    {
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return false;
        }

        var depth = 1;
        for (var index = _index; index < _tokens.Count; index++)
        {
            var (result, nextDepth) = ClassifyParenthesizedPathToken(index, depth);
            depth = nextDepth;
            if (result.HasValue)
            {
                return result.Value;
            }
        }

        return false;
    }

    private (bool? Result, int Depth) ClassifyParenthesizedPathToken(int index, int depth)
    {
        var kind = _tokens[index].Kind;
        if (kind == SyntaxKind.OpenParen)
        {
            return (null, depth + 1);
        }

        if (kind == SyntaxKind.CloseParen)
        {
            var closedDepth = depth - 1;
            return closedDepth == 0 ? (false, closedDepth) : (null, closedDepth);
        }

        if (depth == 1 && kind is SyntaxKind.MinusToken or SyntaxKind.JsonArrowToken)
        {
            return (true, depth);
        }

        if (depth == 1 && kind == SyntaxKind.LessThan
            && index + 1 < _tokens.Count
            && _tokens[index + 1].Kind == SyntaxKind.MinusToken)
        {
            return (true, depth);
        }

        return (null, depth);
    }
}
