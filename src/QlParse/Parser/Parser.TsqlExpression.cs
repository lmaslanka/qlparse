namespace QlParse;

// SQL Server expressions (workstream B): calls whose grammar differs from a plain `name(args)` call
// (CONVERT / TRY_CONVERT with a leading type, TRY_CAST / PARSE / TRY_PARSE, the CONTAINS / FREETEXT
// full-text predicates, AI_GENERATE_EMBEDDINGS ... USE MODEL, AI_GENERATE_CHUNKS named arguments),
// aggregate calls with DISTINCT / ALL and WITHIN GROUP, keyword-named functions called with plain
// arguments (LEFT, RIGHT, SUBSTRING, TRANSLATE), `AT TIME ZONE`, and NEXT VALUE FOR ... OVER.
// Every entry point is reached only under SqlOptions.SqlServer.
internal sealed partial class Parser
{
    private const int AtTimeZoneBindingPower = ExponentBindingPower;

    // A single flag test for "some dialect is on", so ISO dispatch keeps one branch where it had one.
    internal bool IsPostgresOrSqlServer => (_flags & (SqlOptions.Postgres | SqlOptions.SqlServer)) != 0;

    // Prefix fallback: LEFT and RIGHT are join keywords in ISO but ordinary string functions in T-SQL.
    internal Expression? TryParseTsqlExpressionPrefix() =>
        _current.Kind is SyntaxKind.LeftKeyword or SyntaxKind.RightKeyword && NextKind == SyntaxKind.OpenParen
            ? ParseFunctionCall()
            : null;

    // `name(` under a dialect flag. Postgres keeps its own call grammar; SQL Server goes through ParseTsqlCall.
    internal Expression ParseDialectCall() => IsPostgres ? ParsePgCallOrOperator() : ParseTsqlCall();

    private Expression ParseTsqlCall() =>
        TryParseTsqlConversionCall() ?? TryParseTsqlSearchCall() ?? TryParseTsqlGapsCall() ?? ParseTsqlFunctionCall();

    private Expression? TryParseTsqlConversionCall()
    {
        if (IdentifierEquals(Keyword.TsqlExprTryConvert))
        {
            return ParseTsqlConvert();
        }

        if (IdentifierEquals(Keyword.TsqlExprTryCast) || IdentifierEquals(Keyword.TsqlExprParse)
            || IdentifierEquals(Keyword.TsqlExprTryParse))
        {
            return ParseTsqlCast();
        }

        return null;
    }

    private Expression? TryParseTsqlSearchCall()
    {
        if (IdentifierEquals(Keyword.Contains) || IdentifierEquals(Keyword.TsqlExprFreetext))
        {
            return ParseTsqlFullTextPredicate();
        }

        if (IdentifierEquals(Keyword.TsqlExprGenerateEmbeddings))
        {
            return ParseTsqlGenerateEmbeddings();
        }

        return IdentifierEquals(Keyword.TsqlExprGenerateChunks) ? ParseTsqlNamedArgumentCall() : null;
    }

    // `name([DISTINCT | ALL] args) [WITHIN GROUP (ORDER BY ...)] [IGNORE | RESPECT NULLS] [OVER (...)]`.
    // A plain call stays a FunctionCallExpression; DISTINCT / ALL or WITHIN GROUP make it an AggregateCallExpression.
    private Expression ParseTsqlFunctionCall()
    {
        var name = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var quantifier = _current.Kind is SyntaxKind.DistinctKeyword or SyntaxKind.AllKeyword
            ? Advance()
            : (SyntaxToken?)null;
        var arguments = ParseFunctionCallArguments();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return quantifier is null && IsTsqlGraphPathWithinGroup()
            ? ParseTsqlGraphPathAggregate(name, openParen, arguments, closeParen)
            : ParseTsqlFunctionCallTail(new TsqlCallHead(name, openParen, quantifier, arguments, closeParen));
    }

    private readonly record struct TsqlCallHead(
        SyntaxToken Name,
        SyntaxToken OpenParen,
        SyntaxToken? Quantifier,
        IReadOnlyList<Expression> Arguments,
        SyntaxToken CloseParen);

    // The clauses after a call's argument list: WITHIN GROUP, IGNORE / RESPECT NULLS and OVER.
    private Expression ParseTsqlFunctionCallTail(TsqlCallHead head)
    {
        var (name, openParen, quantifier, arguments, closeParen) = head;
        var withinGroup = IsWithinGroup() ? ParseWithinGroup() : null;
        var nullTreatment = ParseOptionalNullTreatment();
        WindowSpecification? over = _current.Kind == SyntaxKind.OverKeyword ? ParseOver() : null;
        var span = SourceSpan.From(name, over?.Span ?? nullTreatment.NullsKeyword?.Span ?? withinGroup?.Span ?? closeParen.Span);
        if (quantifier is null && withinGroup is null)
        {
            return new FunctionCallExpression
            {
                Span = span,
                Name = name,
                OpenParen = openParen,
                Arguments = arguments,
                CloseParen = closeParen,
                NullTreatment = nullTreatment.NullTreatment,
                NullsKeyword = nullTreatment.NullsKeyword,
                Over = over,
            };
        }

        return new AggregateCallExpression
        {
            Span = span,
            Name = name,
            OpenParen = openParen,
            SetQuantifier = quantifier,
            Arguments = arguments,
            CloseParen = closeParen,
            WithinGroup = withinGroup,
            Over = over,
        };
    }

    // `CONVERT(type, expression [, style])` / `TRY_CONVERT(...)`.
    internal TsqlConvertExpression ParseTsqlConvert()
    {
        var functionName = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var type = ParseDataType();
        var comma = Expect(SyntaxKind.Comma);
        var expression = ParseExpression();
        SyntaxToken? styleComma = null;
        Expression? style = null;
        if (_current.Kind == SyntaxKind.Comma)
        {
            styleComma = Advance();
            style = ParseExpression();
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlConvertExpression
        {
            Span = SourceSpan.From(functionName, closeParen),
            FunctionName = functionName,
            OpenParen = openParen,
            Type = type,
            Comma = comma,
            Expression = expression,
            StyleComma = styleComma,
            Style = style,
            CloseParen = closeParen,
        };
    }

    // `TRY_CAST(expression AS type)`, `PARSE(string AS type [USING culture])`, `TRY_PARSE(...)`.
    private TsqlCastExpression ParseTsqlCast()
    {
        var allowsCulture = !IdentifierEquals(Keyword.TsqlExprTryCast);
        var functionName = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var expression = ParseExpression();
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var type = ParseDataType();
        SyntaxToken? usingKeyword = null;
        Expression? culture = null;
        if (allowsCulture && IdentifierEquals(Keyword.Using))
        {
            usingKeyword = Advance();
            culture = ParseExpression();
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlCastExpression
        {
            Span = SourceSpan.From(functionName, closeParen),
            FunctionName = functionName,
            OpenParen = openParen,
            Expression = expression,
            AsKeyword = asKeyword,
            Type = type,
            UsingKeyword = usingKeyword,
            Culture = culture,
            CloseParen = closeParen,
        };
    }

    // `{CONTAINS | FREETEXT}({column | (columns) | * | PROPERTY(column, 'name')}, condition [, LANGUAGE l])`.
    private TsqlFullTextPredicateExpression ParseTsqlFullTextPredicate()
    {
        var functionName = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var columns = _current.Kind == SyntaxKind.Star ? ParseStar() : ParseExpression();
        var comma = Expect(SyntaxKind.Comma);
        var searchCondition = ParseExpression();
        SyntaxToken? languageComma = null;
        SyntaxToken? languageKeyword = null;
        Expression? language = null;
        if (_current.Kind == SyntaxKind.Comma)
        {
            languageComma = Advance();
            languageKeyword = ExpectTsqlWord(Keyword.Language);
            language = ParseExpression();
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlFullTextPredicateExpression
        {
            Span = SourceSpan.From(functionName, closeParen),
            FunctionName = functionName,
            OpenParen = openParen,
            Columns = columns,
            Comma = comma,
            SearchCondition = searchCondition,
            LanguageComma = languageComma,
            LanguageKeyword = languageKeyword,
            Language = language,
            CloseParen = closeParen,
        };
    }

    // `AI_GENERATE_EMBEDDINGS(source USE MODEL model [PARAMETERS json])`.
    private TsqlGenerateEmbeddingsExpression ParseTsqlGenerateEmbeddings()
    {
        var functionName = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var source = ParseExpression();
        var useKeyword = ExpectTsqlWord(Keyword.TsqlExprUse);
        var modelKeyword = ExpectTsqlWord(Keyword.TsqlExprModel);
        var modelName = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        modelName.AddRange(ParseDottedNameTail());
        SyntaxToken? parametersKeyword = null;
        Expression? parameters = null;
        if (IdentifierEquals(Keyword.TsqlExprParameters))
        {
            parametersKeyword = Advance();
            parameters = ParseExpression();
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlGenerateEmbeddingsExpression
        {
            Span = SourceSpan.From(functionName, closeParen),
            FunctionName = functionName,
            OpenParen = openParen,
            Source = source,
            UseKeyword = useKeyword,
            ModelKeyword = modelKeyword,
            ModelName = modelName,
            ParametersKeyword = parametersKeyword,
            Parameters = parameters,
            CloseParen = closeParen,
        };
    }

    // `AI_GENERATE_CHUNKS(source = text, chunk_type = FIXED [, chunk_size = n] ...)`: `name = value` arguments.
    private FunctionCallExpression ParseTsqlNamedArgumentCall()
    {
        var name = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var arguments = new List<Expression> { ParseTsqlNamedArgument() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            arguments.Add(ParseTsqlNamedArgument());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new FunctionCallExpression
        {
            Span = SourceSpan.From(name, closeParen),
            Name = name,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
        };
    }

    private Expression ParseTsqlNamedArgument()
    {
        if (_current.Kind != SyntaxKind.Identifier || NextKind != SyntaxKind.EqualsToken)
        {
            return ParseExpression();
        }

        var name = Advance();
        var equals = Advance();
        var value = ParseExpression();
        return new NamedArgumentExpression
        {
            Span = SourceSpan.From(name, value.Span),
            Name = name,
            Arrow = equals,
            Value = value,
        };
    }

    private SyntaxToken ExpectTsqlWord(string keyword) =>
        IdentifierEquals(keyword)
            ? Advance()
            : throw new SqlParseException($"Expected {keyword.ToUpperInvariant()}, found {_current.Kind}", _current.Position);

    // `JSON_OBJECT(@key : value)`: a variable key name.
    internal bool IsTsqlJsonVariableKey() =>
        _current.Kind == SyntaxKind.EmbeddedHost && IsSqlServer && NextKind == SyntaxKind.ColonToken;

    // `NEXT VALUE FOR sequence`, which SQL Server may follow with `OVER (ORDER BY ...)`.
    internal Expression ParseOptionalTsqlNextValueOver(NextValueExpression nextValue)
    {
        if (_current.Kind != SyntaxKind.OverKeyword || !IsSqlServer)
        {
            return nextValue;
        }

        var over = ParseOver();
        return new TsqlNextValueOverExpression
        {
            Span = SourceSpan.From(nextValue.Span, over.Span),
            NextValue = nextValue,
            Over = over,
        };
    }

    // Binding power of an identifier that follows a complete operand under a dialect flag. Only a Postgres
    // qualified OPERATOR call and a SQL Server AT TIME ZONE suffix continue the expression.
    internal int DialectIdentifierBindingPower()
    {
        if (IsPostgres && IsQualifiedOperatorStart())
        {
            return OtherOperatorBindingPower;
        }

        return IsSqlServer && IsTsqlAtTimeZone() ? AtTimeZoneBindingPower : 0;
    }

    // The operator an identifier with a binding power starts: AT TIME ZONE or a Postgres qualified OPERATOR.
    internal Expression ParseDialectIdentifierOperator(Expression left) =>
        IsSqlServer && IsTsqlAtTimeZone() ? ParseTsqlAtTimeZone(left) : ParseQualifiedOperator(left);

    private bool IsTsqlAtTimeZone() =>
        IdentifierEquals(Keyword.TsqlExprAt)
        && NextEquals(Keyword.Time)
        && _index + 1 < _tokens.Count
        && TokenEquals(_tokens[_index + 1], Keyword.Zone);

    private TsqlAtTimeZoneExpression ParseTsqlAtTimeZone(Expression expression)
    {
        var atKeyword = Advance();
        var timeKeyword = Advance();
        var zoneKeyword = Advance();
        var timeZone = ParseExpression(AtTimeZoneBindingPower + 1);
        return new TsqlAtTimeZoneExpression
        {
            Span = SourceSpan.From(expression.Span, timeZone.Span),
            Expression = expression,
            AtKeyword = atKeyword,
            TimeKeyword = timeKeyword,
            ZoneKeyword = zoneKeyword,
            TimeZone = timeZone,
        };
    }
}
