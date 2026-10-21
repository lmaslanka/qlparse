namespace QlParse;

// Postgres expression-level grammar: positional parameters, custom/prefix operators, OPERATOR(schema.op),
// array slices with omitted bounds, and the Postgres function-call argument list (named arguments,
// VARIADIC, aggregate DISTINCT / ORDER BY, WITHIN GROUP). Every entry point is reached only when
// SqlOptions.Postgres is set, or from a token kind the lexer produces only under that flag.
internal sealed partial class Parser
{
    // Reached from ParsePrefixTail's error arm only, so ISO prefixes pay nothing for it.
    internal Expression? TryParsePgPrefix()
    {
        if (!IsPostgres)
        {
            return null;
        }

        return _current.Kind switch
        {
            SyntaxKind.PositionalParameter => PositionalParameter(),
            SyntaxKind.OperatorToken => ParsePgPrefixOperator(),
            SyntaxKind.LeftKeyword or SyntaxKind.RightKeyword or SyntaxKind.UnnestKeyword
                when NextKind == SyntaxKind.OpenParen => ParsePgFunctionCall(),
            _ => null,
        };
    }

    private PositionalParameterExpression PositionalParameter()
    {
        var parameter = Advance();
        return new PositionalParameterExpression { Parameter = parameter, Span = parameter.Span };
    }

    private UnaryExpression ParsePgPrefixOperator()
    {
        var operatorToken = Advance();
        var expression = ParseExpression(OtherOperatorBindingPower + 1);
        return new UnaryExpression
        {
            Span = SourceSpan.From(operatorToken, expression.Span),
            OperatorToken = operatorToken,
            Expression = expression,
        };
    }

    internal bool IsQualifiedOperatorStart() =>
        IdentifierEquals(Keyword.Operator) && NextKind == SyntaxKind.OpenParen;

    internal QualifiedOperatorExpression ParseQualifiedOperator(Expression? left)
    {
        var operatorKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var schemaParts = new List<SyntaxToken>();
        while (_current.Kind == SyntaxKind.Identifier && NextKind == SyntaxKind.Dot)
        {
            schemaParts.Add(Advance());
            Advance();
        }

        if (!IsOperatorKind(_current.Kind))
        {
            throw new SqlParseException($"Expected operator, found {_current.Kind}", _current.Position);
        }

        var operatorToken = Advance();
        var closeParen = Expect(SyntaxKind.CloseParen);
        var right = ParseExpression(OtherOperatorBindingPower + 1);
        return new QualifiedOperatorExpression
        {
            Span = SourceSpan.From(left?.Span ?? operatorKeyword.Span, right.Span),
            Left = left,
            OperatorKeyword = operatorKeyword,
            OpenParen = openParen,
            SchemaParts = schemaParts,
            Operator = operatorToken,
            CloseParen = closeParen,
            Right = right,
        };
    }

    // `x = ANY (array)` / `x < ALL (array)`: Postgres accepts an array operand where ISO requires a subquery.
    internal Expression ParsePgQuantified(Expression left, SyntaxToken operatorToken)
    {
        var firstInside = _index + 1;
        if (firstInside < _tokens.Count && IsQueryStart(_tokens[firstInside].Kind))
        {
            return ParseQuantifiedSubquery(left, operatorToken);
        }

        var quantifier = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var array = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new QuantifiedArrayExpression
        {
            Span = SourceSpan.From(left.Span, closeParen),
            Left = left,
            OperatorToken = operatorToken,
            Quantifier = quantifier,
            OpenParen = openParen,
            Array = array,
            CloseParen = closeParen,
        };
    }

    private static bool IsOperatorKind(SyntaxKind kind) => kind is
        SyntaxKind.OperatorToken
        or SyntaxKind.PlusToken
        or SyntaxKind.MinusToken
        or SyntaxKind.Star
        or SyntaxKind.SlashToken
        or SyntaxKind.PercentToken
        or SyntaxKind.CaretToken
        or SyntaxKind.LessThan
        or SyntaxKind.GreaterThan
        or SyntaxKind.EqualsToken
        or SyntaxKind.LessOrEqual
        or SyntaxKind.GreaterOrEqual
        or SyntaxKind.NotEqualsToken
        or SyntaxKind.ConcatToken
        or SyntaxKind.BarToken
        or SyntaxKind.QuestionMark
        or SyntaxKind.JsonArrowToken
        or SyntaxKind.JsonTextArrowToken;

    // Postgres subscripts: a slice is any bracket holding a top-level ':' (`a[1:3]` `a[:3]` `a[2:]` `a[:]`);
    // anything else is a plain element subscript and falls through to the JsonAccessorExpression postfix.
    internal bool IsPgArraySlice()
    {
        var depth = 0;
        for (var index = _index; index < _tokens.Count; index++)
        {
            var kind = _tokens[index].Kind;
            if (kind is SyntaxKind.OpenBracket or SyntaxKind.OpenParen)
            {
                depth++;
                continue;
            }

            if (kind is SyntaxKind.CloseBracket or SyntaxKind.CloseParen)
            {
                if (depth == 0)
                {
                    return false;
                }

                depth--;
                continue;
            }

            if (depth == 0 && kind == SyntaxKind.ColonToken)
            {
                return true;
            }
        }

        return false;
    }

    internal MdarraySliceExpression ParsePgArraySlice(Expression target)
    {
        var openBracket = Advance();
        Expression? lower = _current.Kind == SyntaxKind.ColonToken ? null : ParseExpression();
        var colon = Expect(SyntaxKind.ColonToken);
        Expression? upper = _current.Kind == SyntaxKind.CloseBracket ? null : ParseExpression();
        var closeBracket = Expect(SyntaxKind.CloseBracket);
        var axis = new MdarrayAxis
        {
            Span = SourceSpan.From(lower?.Span ?? colon.Span, upper?.Span ?? colon.Span),
            Lower = lower,
            Colon = colon,
            Upper = upper,
        };
        return new MdarraySliceExpression
        {
            Span = SourceSpan.From(target.Span, closeBracket),
            Target = target,
            OpenBracket = openBracket,
            Axes = [axis],
            CloseBracket = closeBracket,
        };
    }

    // Postgres function call: `name(...)` where the argument list may carry DISTINCT/ALL, named
    // (`=>` / `:=`) and VARIADIC arguments and a trailing ORDER BY, followed by WITHIN GROUP, FILTER,
    // and OVER. A plain call stays a FunctionCallExpression; one using an aggregate-only clause becomes
    // an AggregateCallExpression.
    internal Expression ParsePgCallOrOperator() =>
        IsQualifiedOperatorStart() ? ParseQualifiedOperator(null) : ParsePgFunctionCall();

    private Expression ParsePgFunctionCall()
    {
        var name = Advance();
        var call = ParsePgCallArguments();
        var withinGroup = IsWithinGroup() ? ParseWithinGroup() : null;
        FilterClause? filter = _current.Kind == SyntaxKind.FilterKeyword ? ParseFilter() : null;
        WindowSpecification? over = _current.Kind == SyntaxKind.OverKeyword ? ParseOver() : null;
        var span = SourceSpan.From(name, over?.Span ?? filter?.Span ?? withinGroup?.Span ?? call.CloseParen.Span);
        if (call.Quantifier is null && call.OrderBy is null && withinGroup is null)
        {
            return new FunctionCallExpression
            {
                Span = span,
                Name = name,
                OpenParen = call.OpenParen,
                Arguments = call.Arguments,
                CloseParen = call.CloseParen,
                Filter = filter,
                Over = over,
            };
        }

        return new AggregateCallExpression
        {
            Span = span,
            Name = name,
            OpenParen = call.OpenParen,
            SetQuantifier = call.Quantifier,
            Arguments = call.Arguments,
            OrderBy = call.OrderBy,
            CloseParen = call.CloseParen,
            WithinGroup = withinGroup,
            Filter = filter,
            Over = over,
        };
    }

    private readonly record struct PgCallArguments(
        SyntaxToken OpenParen,
        SyntaxToken? Quantifier,
        IReadOnlyList<Expression> Arguments,
        OrderByClause? OrderBy,
        SyntaxToken CloseParen);

    private PgCallArguments ParsePgCallArguments()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var quantifier = _current.Kind is SyntaxKind.DistinctKeyword or SyntaxKind.AllKeyword
            ? Advance()
            : (SyntaxToken?)null;
        var arguments = ParsePgFunctionArguments();
        OrderByClause? orderBy = _current.Kind == SyntaxKind.OrderKeyword ? ParseOrderBy() : null;
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PgCallArguments(openParen, quantifier, arguments, orderBy, closeParen);
    }

    private IReadOnlyList<Expression> ParsePgFunctionArguments()
    {
        if (_current.Kind == SyntaxKind.CloseParen)
        {
            return [];
        }

        if (_current.Kind == SyntaxKind.Star)
        {
            return [ParseStar()];
        }

        var arguments = new List<Expression> { ParsePgFunctionArgument() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            arguments.Add(ParsePgFunctionArgument());
        }

        return arguments;
    }

    private Expression ParsePgFunctionArgument()
    {
        if (IdentifierEquals(Keyword.Variadic) && IsVariadicOperandStart())
        {
            var variadicKeyword = Advance();
            var argument = ParsePgFunctionArgument();
            return new VariadicArgumentExpression
            {
                Span = SourceSpan.From(variadicKeyword, argument.Span),
                VariadicKeyword = variadicKeyword,
                Argument = argument,
            };
        }

        if (_current.Kind == SyntaxKind.Identifier
            && NextKind is SyntaxKind.EqualsGreaterToken or SyntaxKind.ColonEqualsToken)
        {
            var name = Advance();
            var arrow = Advance();
            var value = ParseExpression();
            return new NamedArgumentExpression
            {
                Span = SourceSpan.From(name, value.Span),
                Name = name,
                Arrow = arrow,
                Value = value,
            };
        }

        return ParseExpression();
    }

    private bool IsVariadicOperandStart() =>
        _index < _tokens.Count
        && (NextKind == SyntaxKind.PositionalParameter || IsExpressionStart(_tokens[_index]));

    private bool IsWithinGroup() =>
        IdentifierEquals(Keyword.Within) && NextKind == SyntaxKind.GroupKeyword;

    private WithinGroupClause ParseWithinGroup()
    {
        var withinKeyword = Advance();
        var groupKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var orderBy = ParseOrderBy();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new WithinGroupClause
        {
            Span = SourceSpan.From(withinKeyword, closeParen),
            WithinKeyword = withinKeyword,
            GroupKeyword = groupKeyword,
            OpenParen = openParen,
            OrderBy = orderBy,
            CloseParen = closeParen,
        };
    }
}
