namespace QlParse;

// EXEC[UTE] of a module (stored procedure, system procedure, @module variable, numbered procedure) and of a
// character string, also used as the source of INSERT ... EXEC. EXECUTE AS is the security statement owned by
// workstream F, so it is left alone.
internal sealed partial class Parser
{
    internal Query? TryParseTsqlExecute()
    {
        var next = NextKind;
        if (next == SyntaxKind.AsKeyword)
        {
            return null;
        }

        return next == SyntaxKind.OpenParen ? ParseTsqlExecuteString() : ParseTsqlExecuteModule();
    }

    // The source of INSERT [INTO] t [(columns)] EXEC ...; any other source is an ordinary query.
    internal Query ParseTsqlInsertSource() =>
        (IdentifierEquals(Keyword.Exec) || IdentifierEquals(Keyword.Execute) ? TryParseTsqlExecute() : null) ?? ParseQuery();

    // EXEC [@status =] {module | @module} [;number] [argument, ...] [WITH option, ...]
    private TsqlExecuteStatement ParseTsqlExecuteModule()
    {
        var executeKeyword = Advance();
        SyntaxToken? returnStatus = null;
        SyntaxToken? returnEquals = null;
        if (_current.Kind == SyntaxKind.EmbeddedHost && NextKind == SyntaxKind.EqualsToken)
        {
            returnStatus = Advance();
            returnEquals = Advance();
        }

        IReadOnlyList<SyntaxToken> module = _current.Kind == SyntaxKind.EmbeddedHost ? [Advance()] : ParseQualifiedName();
        var (numberSemicolon, number) = ParseTsqlProcedureNumber();
        var arguments = new List<TsqlExecuteArgument>();
        if (IsTsqlExpressionStart() || IdentifierEquals(Keyword.Default))
        {
            arguments.Add(ParseTsqlExecuteArgument());
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                arguments.Add(ParseTsqlExecuteArgument());
            }
        }

        SyntaxToken? withKeyword = null;
        IReadOnlyList<TsqlExecuteOption> options = [];
        if (_current.Kind == SyntaxKind.WithKeyword)
        {
            withKeyword = Advance();
            options = ParseTsqlExecuteOptions();
        }

        return new TsqlExecuteStatement
        {
            Span = SourceSpan.From(executeKeyword, TsqlPreviousToken()),
            ExecuteKeyword = executeKeyword,
            ReturnStatus = returnStatus,
            ReturnEquals = returnEquals,
            Module = module,
            NumberSemicolon = numberSemicolon,
            Number = number,
            Arguments = arguments,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // `name;n` numbers one of a group of procedures; a statement never starts with a number, so `;` followed
    // by a number is never a terminator.
    internal (SyntaxToken? Semicolon, SyntaxToken? Number) ParseTsqlProcedureNumber()
    {
        if (_current.Kind != SyntaxKind.Semicolon || NextKind != SyntaxKind.Number)
        {
            return default;
        }

        var semicolon = Advance();
        return (semicolon, Advance());
    }

    // [@parameter =] {value | @variable [OUTPUT] | DEFAULT}
    private TsqlExecuteArgument ParseTsqlExecuteArgument()
    {
        SyntaxToken? parameter = null;
        SyntaxToken? equalsToken = null;
        if (_current.Kind == SyntaxKind.EmbeddedHost && NextKind == SyntaxKind.EqualsToken)
        {
            parameter = Advance();
            equalsToken = Advance();
        }

        var start = _current;
        var defaultKeyword = AcceptTsqlWord(Keyword.Default);
        var value = defaultKeyword is null ? ParseExpression() : null;
        var outputKeyword = IdentifierEquals(Keyword.Output) || IdentifierEquals(Keyword.Out) ? Advance() : (SyntaxToken?)null;
        return new TsqlExecuteArgument
        {
            Span = SourceSpan.From(parameter ?? start, TsqlPreviousToken()),
            Parameter = parameter,
            EqualsToken = equalsToken,
            Value = value,
            DefaultKeyword = defaultKeyword,
            OutputKeyword = outputKeyword,
        };
    }

    // RECOMPILE | RESULT SETS {UNDEFINED | NONE | (definition, ...)}, comma-separated
    private List<TsqlExecuteOption> ParseTsqlExecuteOptions()
    {
        var options = new List<TsqlExecuteOption> { ParseTsqlExecuteOption() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            options.Add(ParseTsqlExecuteOption());
        }

        return options;
    }

    private TsqlExecuteOption ParseTsqlExecuteOption()
    {
        if (IdentifierEquals(Keyword.ProcRecompile))
        {
            var recompile = Advance();
            return new TsqlExecuteOption { Span = recompile.Span, Keywords = [recompile] };
        }

        var resultKeyword = ExpectIdent(Keyword.Result);
        var setsKeyword = ExpectIdent(Keyword.Sets);
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            var kind = IdentifierEquals(Keyword.None) ? Advance() : ExpectIdent(Keyword.ProcUndefined);
            return new TsqlExecuteOption { Span = SourceSpan.From(resultKeyword, kind), Keywords = [resultKeyword, setsKeyword, kind] };
        }

        var openParen = Advance();
        var resultSets = new List<TsqlResultSetDefinition> { ParseTsqlResultSetDefinition() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            resultSets.Add(ParseTsqlResultSetDefinition());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlExecuteOption
        {
            Span = SourceSpan.From(resultKeyword, closeParen),
            Keywords = [resultKeyword, setsKeyword],
            OpenParen = openParen,
            ResultSets = resultSets,
            CloseParen = closeParen,
        };
    }

    // (column type [COLLATE c] [[NOT] NULL], ...) | AS OBJECT name | AS TYPE name | AS FOR XML
    private TsqlResultSetDefinition ParseTsqlResultSetDefinition()
    {
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            var asKeyword = Advance();
            if (_current.Kind == SyntaxKind.ForKeyword)
            {
                var forKeyword = Advance();
                var xml = ExpectIdent(Keyword.Xml);
                return new TsqlResultSetDefinition { Span = SourceSpan.From(asKeyword, xml), AsKeyword = asKeyword, Kind = [forKeyword, xml] };
            }

            var kind = IdentifierEquals(Keyword.Object) ? Advance() : ExpectIdent(Keyword.Type);
            var name = ParseQualifiedName();
            return new TsqlResultSetDefinition { Span = SourceSpan.From(asKeyword, name[^1]), AsKeyword = asKeyword, Kind = [kind], Name = name };
        }

        var openParen = Expect(SyntaxKind.OpenParen);
        var columns = new List<TsqlResultSetColumn> { ParseTsqlResultSetColumn() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            columns.Add(ParseTsqlResultSetColumn());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlResultSetDefinition
        {
            Span = SourceSpan.From(openParen, closeParen),
            OpenParen = openParen,
            Columns = columns,
            CloseParen = closeParen,
        };
    }

    private TsqlResultSetColumn ParseTsqlResultSetColumn()
    {
        var name = Expect(SyntaxKind.Identifier);
        var dataType = ParseDataType();
        SyntaxToken? collateKeyword = null;
        SyntaxToken? collation = null;
        if (_current.Kind == SyntaxKind.CollateKeyword)
        {
            collateKeyword = Advance();
            collation = Expect(SyntaxKind.Identifier);
        }

        var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
        var nullKeyword = notKeyword is not null || _current.Kind == SyntaxKind.NullKeyword ? Expect(SyntaxKind.NullKeyword) : (SyntaxToken?)null;
        return new TsqlResultSetColumn
        {
            Span = SourceSpan.From(name, TsqlPreviousToken()),
            Name = name,
            DataType = dataType,
            CollateKeyword = collateKeyword,
            Collation = collation,
            NotKeyword = notKeyword,
            NullKeyword = nullKeyword,
        };
    }

    // EXEC (string [+ ...] [, argument ...]) [AS {LOGIN | USER} = 'name'] [AT linked_server | AT DATA_SOURCE name]
    private TsqlExecuteStringStatement ParseTsqlExecuteString()
    {
        var executeKeyword = Advance();
        var openParen = Advance();
        var command = ParseExpression();
        var arguments = new List<Expression>();
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            arguments.Add(ParseExpression());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        var context = ParseTsqlExecuteContext();
        var target = ParseTsqlExecuteTarget();
        return new TsqlExecuteStringStatement
        {
            Span = SourceSpan.From(executeKeyword, TsqlPreviousToken()),
            ExecuteKeyword = executeKeyword,
            OpenParen = openParen,
            Command = command,
            Arguments = arguments,
            CloseParen = closeParen,
            AsKeyword = context.AsKeyword,
            PrincipalKind = context.Kind,
            PrincipalEquals = context.EqualsToken,
            Principal = context.Principal,
            AtKeyword = target.AtKeyword,
            DataSourceKeyword = target.DataSourceKeyword,
            Target = target.Target,
        };
    }

    private readonly record struct TsqlExecuteContext(SyntaxToken? AsKeyword, SyntaxToken? Kind, SyntaxToken? EqualsToken, SyntaxToken? Principal);

    private readonly record struct TsqlExecuteTarget(SyntaxToken? AtKeyword, SyntaxToken? DataSourceKeyword, SyntaxToken? Target);

    private TsqlExecuteContext ParseTsqlExecuteContext()
    {
        if (_current.Kind != SyntaxKind.AsKeyword || (NextKind != SyntaxKind.UserKeyword && !NextEquals(Keyword.ProcLogin)))
        {
            return default;
        }

        var asKeyword = Advance();
        var kind = Advance();
        var equalsToken = Expect(SyntaxKind.EqualsToken);
        var principal = Expect(SyntaxKind.String);
        return new TsqlExecuteContext(asKeyword, kind, equalsToken, principal);
    }

    private TsqlExecuteTarget ParseTsqlExecuteTarget()
    {
        if (!IdentifierEquals(Keyword.ProcAt))
        {
            return default;
        }

        var atKeyword = Advance();
        var dataSourceKeyword = AcceptTsqlWord(Keyword.ProcDataSource);
        return new TsqlExecuteTarget(atKeyword, dataSourceKeyword, Expect(SyntaxKind.Identifier));
    }
}
