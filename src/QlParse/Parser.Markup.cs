namespace QlParse;

internal sealed partial class Parser
{
    private bool IsMarkupCall() => IsMarkupName(_current) && NextKind == SyntaxKind.OpenParen;

    private bool IsMarkupTable() =>
        (TokenEquals(_current, Keyword.XmlTable) || TokenEquals(_current, Keyword.JsonTable))
        && NextKind == SyntaxKind.OpenParen;

    private bool IsMarkupName(SyntaxToken token) =>
        TokenEquals(token, Keyword.XmlParse)
        || TokenEquals(token, Keyword.XmlSerialize)
        || TokenEquals(token, Keyword.XmlElement)
        || TokenEquals(token, Keyword.XmlAttributes)
        || TokenEquals(token, Keyword.XmlForest)
        || TokenEquals(token, Keyword.XmlConcat)
        || TokenEquals(token, Keyword.XmlAgg)
        || TokenEquals(token, Keyword.XmlComment)
        || TokenEquals(token, Keyword.XmlPi)
        || TokenEquals(token, Keyword.XmlDocument)
        || TokenEquals(token, Keyword.XmlQuery)
        || TokenEquals(token, Keyword.XmlExists)
        || TokenEquals(token, Keyword.XmlTable)
        || TokenEquals(token, Keyword.XmlCast)
        || TokenEquals(token, Keyword.XmlValidate)
        || TokenEquals(token, Keyword.XmlNamespaces)
        || TokenEquals(token, Keyword.JsonObject)
        || TokenEquals(token, Keyword.JsonArray)
        || TokenEquals(token, Keyword.JsonObjectAgg)
        || TokenEquals(token, Keyword.JsonArrayAgg)
        || TokenEquals(token, Keyword.JsonValue)
        || TokenEquals(token, Keyword.JsonQuery)
        || TokenEquals(token, Keyword.JsonExists)
        || TokenEquals(token, Keyword.JsonTable)
        || TokenEquals(token, Keyword.JsonSerialize)
        || TokenEquals(token, Keyword.JsonScalar);

    private MarkupCallExpression ParseMarkupCall()
    {
        var name = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var body = ParseMarkupBody();
        var closeParen = Expect(SyntaxKind.CloseParen);
        FilterClause? filter = _current.Kind == SyntaxKind.FilterKeyword ? ParseFilter() : null;
        WindowSpecification? over = _current.Kind == SyntaxKind.OverKeyword ? ParseOver() : null;
        var end = over?.Span ?? filter?.Span ?? closeParen.Span;
        return new MarkupCallExpression
        {
            Span = SourceSpan.From(name, end),
            Name = name,
            OpenParen = openParen,
            CloseParen = closeParen,
            Arguments = body.Arguments,
            Clauses = body.Clauses,
            Returning = body.Returning,
            Query = body.Query,
            OrderBy = body.OrderBy,
            Columns = body.Columns,
            Filter = filter,
            Over = over,
        };
    }

    private MarkupTable ParseMarkupTable()
    {
        var name = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var body = ParseMarkupBody();
        var closeParen = Expect(SyntaxKind.CloseParen);
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

        var end = alias?.Span ?? closeParen.Span;
        return new MarkupTable
        {
            Span = SourceSpan.From(name.Span, end),
            Name = name,
            OpenParen = openParen,
            CloseParen = closeParen,
            Arguments = body.Arguments,
            Clauses = body.Clauses,
            Columns = body.Columns,
            AsKeyword = asKeyword,
            Alias = alias,
        };
    }

    private MarkupBody ParseMarkupBody()
    {
        var arguments = new List<Expression>();
        var clauses = new List<SyntaxToken>();
        DataType? returning = null;
        Query? query = null;
        OrderByClause? orderBy = null;
        var columns = new List<MarkupColumn>();
        var needComma = false;
        while (_current.Kind != SyntaxKind.CloseParen && _current.Kind != SyntaxKind.EndOfFile)
        {
            if (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                needComma = false;
            }
            else if (needComma && !IsMarkupClause())
            {
                throw new SqlParseException($"Expected comma, found {_current.Kind}", _current.Position);
            }

            if (query is null && arguments.Count == 0 && IsQueryStart(_current.Kind))
            {
                query = ParseQuery();
                needComma = true;
                continue;
            }

            if (IsMarkupClause())
            {
                ParseMarkupClause(clauses, arguments, ref returning, ref orderBy, columns);
                needComma = false;
                continue;
            }

            ParseMarkupEntry(clauses, arguments);
            needComma = true;
        }

        return new MarkupBody(arguments, clauses, returning, query, orderBy, columns);
    }

    private void ParseMarkupEntry(List<SyntaxToken> clauses, List<Expression> arguments)
    {
        if (IdentifierEquals(Keyword.Key))
        {
            clauses.Add(Advance());
            arguments.Add(ParseExpression(ComparisonBindingPower + 1));
            if (!IdentifierEquals(Keyword.Value))
            {
                throw new SqlParseException($"Expected VALUE, found {_current.Kind}", _current.Position);
            }

            clauses.Add(Advance());
            arguments.Add(ParseExpression(ComparisonBindingPower + 1));
            return;
        }

        if (_current.Kind is SyntaxKind.Identifier or SyntaxKind.String && NextKind == SyntaxKind.ColonToken)
        {
            arguments.Add(_current.Kind == SyntaxKind.String ? Literal() : Identifier());
            clauses.Add(Advance());
            arguments.Add(ParseExpression(ComparisonBindingPower + 1));
            ParseFormatJson(clauses);
            return;
        }

        arguments.Add(ParseExpression(ComparisonBindingPower + 1));
        ParseFormatJson(clauses);
    }

    private void ParseMarkupClause(
        List<SyntaxToken> clauses,
        List<Expression> arguments,
        ref DataType? returning,
        ref OrderByClause? orderBy,
        List<MarkupColumn> columns)
    {
        if (_current.Kind == SyntaxKind.OrderKeyword)
        {
            orderBy = ParseOrderBy();
            return;
        }

        if (IdentifierEquals(Keyword.Columns))
        {
            clauses.Add(Advance());
            columns.AddRange(ParseMarkupColumns());
            return;
        }

        if (IdentifierEquals(Keyword.Passing))
        {
            ParsePassing(clauses, arguments);
            return;
        }

        if (IdentifierEquals(Keyword.Returning) || _current.Kind == SyntaxKind.AsKeyword)
        {
            clauses.Add(Advance());
            returning = ParseDataType();
            ParseFormatJson(clauses);
            ParseByRef(clauses);
            return;
        }

        if (IdentifierEquals(Keyword.Default) || IdentifierEquals(Keyword.No))
        {
            ParseDefaultOrNo(clauses, arguments);
            return;
        }

        if (IsOnBehavior())
        {
            ParseOnBehavior(clauses);
            return;
        }

        if (_current.Kind == SyntaxKind.WithKeyword || IdentifierEquals(Keyword.Without))
        {
            ParseWrapperOrUnique(clauses);
            return;
        }

        if (IdentifierEquals(Keyword.According))
        {
            ParseAccording(clauses, arguments);
            return;
        }

        if (IdentifierEquals(Keyword.Preserve) || IdentifierEquals(Keyword.Strip))
        {
            clauses.Add(Advance());
            clauses.Add(ExpectIdent(Keyword.Whitespace));
            return;
        }

        if (IdentifierEquals(Keyword.Encoding))
        {
            clauses.Add(Advance());
            clauses.Add(Expect(SyntaxKind.Identifier));
            return;
        }

        if (IdentifierEquals(Keyword.Name))
        {
            clauses.Add(Advance());
            clauses.Add(Expect(SyntaxKind.Identifier));
            return;
        }

        if (IdentifierEquals(Keyword.Format))
        {
            ParseFormatJson(clauses);
            return;
        }

        if (_current.Kind == SyntaxKind.ByKeyword)
        {
            ParseByRef(clauses);
            return;
        }

        clauses.Add(Advance());
    }

    private void ParsePassing(List<SyntaxToken> clauses, List<Expression> arguments)
    {
        clauses.Add(Advance());
        ParseByRef(clauses);
        var needComma = false;
        while (!IsMarkupClause() && _current.Kind != SyntaxKind.CloseParen)
        {
            if (needComma)
            {
                if (_current.Kind != SyntaxKind.Comma)
                {
                    break;
                }

                Advance();
            }

            arguments.Add(ParseExpression(ComparisonBindingPower + 1));
            if (_current.Kind == SyntaxKind.AsKeyword)
            {
                clauses.Add(Advance());
                clauses.Add(Expect(SyntaxKind.Identifier));
            }

            ParseByRef(clauses);
            needComma = true;
        }
    }

    private void ParseByRef(List<SyntaxToken> clauses)
    {
        if (_current.Kind != SyntaxKind.ByKeyword)
        {
            return;
        }

        clauses.Add(Advance());
        if (!IdentifierEquals(Keyword.Ref) && !IdentifierEquals(Keyword.Value))
        {
            throw new SqlParseException($"Expected REF or VALUE, found {_current.Kind}", _current.Position);
        }

        clauses.Add(Advance());
    }

    private void ParseFormatJson(List<SyntaxToken> clauses)
    {
        if (!IdentifierEquals(Keyword.Format))
        {
            return;
        }

        clauses.Add(Advance());
        clauses.Add(ExpectIdent(Keyword.Json));
    }

    private void ParseDefaultOrNo(List<SyntaxToken> clauses, List<Expression> arguments)
    {
        if (IdentifierEquals(Keyword.No))
        {
            clauses.Add(Advance());
            clauses.Add(ExpectIdent(Keyword.Default));
            return;
        }

        clauses.Add(Advance());
        arguments.Add(ParseExpression(ComparisonBindingPower + 1));
        if (_current.Kind == SyntaxKind.OnKeyword)
        {
            clauses.Add(Advance());
            clauses.Add(Advance());
        }
    }

    private void ParseOnBehavior(List<SyntaxToken> clauses)
    {
        clauses.Add(Advance());
        clauses.Add(Expect(SyntaxKind.OnKeyword));
        clauses.Add(Advance());
    }

    private IsExpression ParseMarkupPredicate(Expression target, SyntaxToken isKeyword, SyntaxToken? notKeyword)
    {
        var value = Advance();
        SyntaxToken? kind = null;
        SyntaxToken? uniqueWith = null;
        SyntaxToken? unique = null;
        SyntaxToken? keys = null;
        var end = value.Span;
        if (TokenEquals(value, Keyword.Json))
        {
            if (IdentifierEquals(Keyword.Value) || IdentifierEquals(Keyword.Object) || IdentifierEquals(Keyword.Scalar)
                || _current.Kind == SyntaxKind.ArrayKeyword)
            {
                kind = Advance();
                end = kind.Value.Span;
            }

            if (_current.Kind == SyntaxKind.WithKeyword || IdentifierEquals(Keyword.Without))
            {
                uniqueWith = Advance();
                unique = Expect(SyntaxKind.UniqueKeyword);
                end = unique.Value.Span;
                if (IdentifierEquals(Keyword.Keys))
                {
                    keys = Advance();
                    end = keys.Value.Span;
                }
            }
        }

        return new IsExpression
        {
            Span = SourceSpan.From(target.Span, end),
            Target = target,
            IsKeyword = isKeyword,
            NotKeyword = notKeyword,
            Value = value,
            KindKeyword = kind,
            UniqueWith = uniqueWith,
            UniqueKeyword = unique,
            KeysKeyword = keys,
        };
    }

    private void ParseWrapperOrUnique(List<SyntaxToken> clauses)
    {
        clauses.Add(Advance());
        if (IdentifierEquals(Keyword.No))
        {
            clauses.Add(Advance());
        }

        if (_current.Kind == SyntaxKind.UniqueKeyword)
        {
            clauses.Add(Advance());
            if (IdentifierEquals(Keyword.Keys))
            {
                clauses.Add(Advance());
            }

            return;
        }

        if (IdentifierEquals(Keyword.Conditional) || IdentifierEquals(Keyword.Unconditional))
        {
            clauses.Add(Advance());
        }

        if (_current.Kind == SyntaxKind.ArrayKeyword)
        {
            clauses.Add(Advance());
        }

        if (IdentifierEquals(Keyword.Wrapper) || IdentifierEquals(Keyword.Bom))
        {
            clauses.Add(Advance());
        }
    }

    private void ParseAccording(List<SyntaxToken> clauses, List<Expression> arguments)
    {
        clauses.Add(Advance());
        if (!IdentifierEquals(Keyword.To))
        {
            throw new SqlParseException($"Expected TO, found {_current.Kind}", _current.Position);
        }

        clauses.Add(Advance());
        clauses.Add(ExpectIdent(Keyword.XmlSchema));
        arguments.Add(ParseExpression(ComparisonBindingPower + 1));
        if (IdentifierEquals(Keyword.Namespace) || (IdentifierEquals(Keyword.No) && NextEquals(Keyword.Namespace)))
        {
            if (IdentifierEquals(Keyword.No))
            {
                clauses.Add(Advance());
            }

            clauses.Add(Advance());
            if (_current.Kind != SyntaxKind.CloseParen && !IsMarkupClause())
            {
                arguments.Add(ParseExpression(ComparisonBindingPower + 1));
            }
        }

        if (IdentifierEquals(Keyword.Element) || (IdentifierEquals(Keyword.No) && NextEquals(Keyword.Element)))
        {
            if (IdentifierEquals(Keyword.No))
            {
                clauses.Add(Advance());
            }

            clauses.Add(Advance());
            if (_current.Kind == SyntaxKind.Identifier)
            {
                clauses.Add(Advance());
            }
        }
    }

    private List<MarkupColumn> ParseMarkupColumns()
    {
        var parenthesized = _current.Kind == SyntaxKind.OpenParen;
        if (parenthesized)
        {
            Advance();
        }

        var columns = new List<MarkupColumn> { ParseMarkupColumn() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            columns.Add(ParseMarkupColumn());
        }

        if (parenthesized)
        {
            Expect(SyntaxKind.CloseParen);
        }

        return columns;
    }

    private MarkupColumn ParseMarkupColumn()
    {
        if (IdentifierEquals(Keyword.Nested))
        {
            var nested = Advance();
            if (IdentifierEquals(Keyword.Path))
            {
                Advance();
            }

            var nestedPath = ParseExpression(ComparisonBindingPower + 1);
            SyntaxToken? alias = null;
            if (_current.Kind == SyntaxKind.AsKeyword)
            {
                Advance();
                alias = Expect(SyntaxKind.Identifier);
            }

            ExpectIdent(Keyword.Columns);
            var children = ParseMarkupColumns();
            var name = alias ?? nested;
            return new MarkupColumn
            {
                Span = SourceSpan.From(nested, children.Count > 0 ? children[^1].Span : nestedPath.Span),
                Name = name,
                Path = nestedPath,
                Nested = children,
            };
        }

        var columnName = Expect(SyntaxKind.Identifier);
        if (_current.Kind == SyntaxKind.ForKeyword)
        {
            var forKeyword = Advance();
            var ordinality = ExpectIdent(Keyword.Ordinality);
            return new MarkupColumn
            {
                Span = SourceSpan.From(columnName, ordinality),
                Name = columnName,
                ForKeyword = forKeyword,
                OrdinalityKeyword = ordinality,
            };
        }

        var type = ParseDataType();
        Expression? path = null;
        Expression? defaultValue = null;
        var end = type.Span;
        while (IdentifierEquals(Keyword.Path) || IdentifierEquals(Keyword.Default) || IsOnBehavior())
        {
            if (IdentifierEquals(Keyword.Path))
            {
                Advance();
                path = ParseExpression(ComparisonBindingPower + 1);
                end = path.Span;
                continue;
            }

            if (IdentifierEquals(Keyword.Default))
            {
                Advance();
                defaultValue = ParseExpression(ComparisonBindingPower + 1);
                end = defaultValue.Span;
                continue;
            }

            Advance();
            Expect(SyntaxKind.OnKeyword);
            var behavior = Advance();
            end = behavior.Span;
        }

        return new MarkupColumn
        {
            Span = SourceSpan.From(columnName.Span, end),
            Name = columnName,
            Type = type,
            Path = path,
            Default = defaultValue,
        };
    }

    private bool IsMarkupClause() =>
        IdentifierEquals(Keyword.Document)
        || IdentifierEquals(Keyword.Content)
        || IdentifierEquals(Keyword.Sequence)
        || IdentifierEquals(Keyword.Passing)
        || IdentifierEquals(Keyword.Returning)
        || IdentifierEquals(Keyword.Preserve)
        || IdentifierEquals(Keyword.Strip)
        || IdentifierEquals(Keyword.Encoding)
        || IdentifierEquals(Keyword.According)
        || IdentifierEquals(Keyword.Columns)
        || IdentifierEquals(Keyword.Format)
        || (IdentifierEquals(Keyword.Name) && NextKind == SyntaxKind.Identifier)
        || _current.Kind == SyntaxKind.OrderKeyword
        || _current.Kind == SyntaxKind.WithKeyword
        || _current.Kind == SyntaxKind.AsKeyword
        || _current.Kind == SyntaxKind.ByKeyword
        || IdentifierEquals(Keyword.Without)
        || IdentifierEquals(Keyword.No)
        || (IdentifierEquals(Keyword.Default) && NextKind != SyntaxKind.CloseParen)
        || IsOnBehavior();

    private bool IsOnBehavior() =>
        (_current.Kind == SyntaxKind.NullKeyword && NextKind == SyntaxKind.OnKeyword)
        || (_current.Kind == SyntaxKind.AbsentKeyword && NextKind == SyntaxKind.OnKeyword)
        || (_current.Kind is SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword or SyntaxKind.UnknownKeyword
            && NextKind == SyntaxKind.OnKeyword)
        || ((IdentifierEquals(Keyword.Error) || IdentifierEquals(Keyword.Empty)) && NextKind == SyntaxKind.OnKeyword);

    private readonly record struct MarkupBody(
        IReadOnlyList<Expression> Arguments,
        IReadOnlyList<SyntaxToken> Clauses,
        DataType? Returning,
        Query? Query,
        OrderByClause? OrderBy,
        IReadOnlyList<MarkupColumn> Columns);
}
