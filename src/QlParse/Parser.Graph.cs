namespace QlParse;

internal sealed partial class Parser
{
    private bool IsCreatePropertyGraph() =>
        IdentifierEquals(Keyword.Create) && NextEquals(Keyword.Property);

    private bool IsDropPropertyGraph() =>
        IdentifierEquals(Keyword.Drop) && NextEquals(Keyword.Property);

    private bool IsGraphTable() =>
        TokenEquals(_current, Keyword.GraphTable) && NextKind == SyntaxKind.OpenParen;

    private CreatePropertyGraphStatement ParseCreatePropertyGraph()
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

    private DropPropertyGraphStatement ParseDropPropertyGraph()
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
                var endpoint = Advance();
                ExpectIdent(Keyword.Key);
                var columns = ParseParenthesizedNames();
                ExpectIdent(Keyword.References);
                var reference = ParseQualifiedName();
                if (_current.Kind == SyntaxKind.OpenParen)
                {
                    reference = [.. reference, .. ParseParenthesizedNames()];
                }

                if (TokenEquals(endpoint, Keyword.Source))
                {
                    source = endpoint;
                    sourceColumns = columns;
                    sourceReference = reference;
                }
                else
                {
                    destination = endpoint;
                    destinationColumns = columns;
                    destinationReference = reference;
                }

                end = reference[^1].Span;
                continue;
            }

            labels.Add(Advance());
            labels.Add(Expect(SyntaxKind.Identifier));
            end = labels[^1].Span;
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

                end = properties[^1].Span;
            }
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

    private GraphTable ParseGraphTable()
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
            if (_current.Kind == SyntaxKind.AsKeyword)
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
        else if (_current.Kind == SyntaxKind.IsKeyword)
        {
            labels.Add(Advance());
            labels.Add(Expect(SyntaxKind.Identifier));
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

        var openBrace = Advance();
        if (_current.Kind == SyntaxKind.Number)
        {
            Advance();
        }

        if (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            if (_current.Kind == SyntaxKind.Number)
            {
                Advance();
            }
        }

        return Expect(SyntaxKind.CloseBrace) is var close && openBrace.Kind == SyntaxKind.OpenBrace
            ? close
            : close;
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
            var kind = _tokens[index].Kind;
            if (kind == SyntaxKind.OpenParen)
            {
                depth++;
                continue;
            }

            if (kind == SyntaxKind.CloseParen)
            {
                depth--;
                if (depth == 0)
                {
                    return false;
                }

                continue;
            }

            if (depth == 1 && kind is SyntaxKind.MinusToken or SyntaxKind.JsonArrowToken)
            {
                return true;
            }

            if (depth == 1 && kind == SyntaxKind.LessThan
                && index + 1 < _tokens.Count
                && _tokens[index + 1].Kind == SyntaxKind.MinusToken)
            {
                return true;
            }
        }

        return false;
    }
}
