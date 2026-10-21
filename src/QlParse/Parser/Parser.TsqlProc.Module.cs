namespace QlParse;

// T-SQL modules: CREATE [OR ALTER] / ALTER PROCEDURE, FUNCTION and TRIGGER, CREATE OR ALTER VIEW (routed to the
// ordinary view parser), CLR modules (EXTERNAL NAME) and CREATE AGGREGATE. Every other CREATE / ALTER is left to
// the ISO dispatch or another workstream.
internal sealed partial class Parser
{
    private readonly record struct TsqlModuleHead(SyntaxToken Start, SyntaxToken? CreateKeyword, SyntaxToken? OrKeyword, SyntaxToken? AlterKeyword);

    private Query? TryParseTsqlCreateModule()
    {
        var index = _index;
        var orAlter = NextKind == SyntaxKind.OrKeyword && IsKeywordAt(index + 1, Keyword.Alter);
        if (orAlter)
        {
            index += Len2;
            if (IsKeywordAt(index, Keyword.View))
            {
                return ParseTsqlCreateOrAlterView();
            }
        }

        var module = TryParseTsqlModuleAt(index);
        if (module is not null || orAlter)
        {
            return module;
        }

        if (IsKeywordAt(index, Keyword.ProcAggregate))
        {
            return ParseTsqlCreateAggregate();
        }

        return IsKeywordAt(index, Keyword.ProcEvent) && IsKeywordAt(index + 1, Keyword.ProcNotification)
            ? ParseTsqlCreateEventNotification()
            : null;
    }

    private Query? TryParseTsqlAlterModule() => TryParseTsqlModuleAt(_index);

    // PROC[EDURE], FUNCTION or TRIGGER at the given token index, after CREATE [OR ALTER] or ALTER.
    private Query? TryParseTsqlModuleAt(int index)
    {
        if (IsKeywordAt(index, Keyword.ProcProc) || IsKeywordAt(index, Keyword.Procedure))
        {
            return ParseTsqlProcedure();
        }

        if (IsKeywordAt(index, Keyword.Function))
        {
            return ParseTsqlFunction();
        }

        return IsKeywordAt(index, Keyword.Trigger) ? ParseTsqlTrigger() : null;
    }

    // CREATE [OR ALTER] or ALTER.
    private TsqlModuleHead ParseTsqlModuleHead()
    {
        if (IdentifierEquals(Keyword.Alter))
        {
            var alterKeyword = Advance();
            return new TsqlModuleHead(alterKeyword, null, null, alterKeyword);
        }

        var createKeyword = Advance();
        if (_current.Kind != SyntaxKind.OrKeyword)
        {
            return new TsqlModuleHead(createKeyword, createKeyword, null, null);
        }

        var orKeyword = Advance();
        return new TsqlModuleHead(createKeyword, createKeyword, orKeyword, Advance());
    }

    // CREATE OR ALTER VIEW: the view definition is parsed from the ALTER keyword on by the ordinary view parser.
    private TsqlCreateOrAlterViewStatement ParseTsqlCreateOrAlterView()
    {
        var createKeyword = Advance();
        var orKeyword = Advance();
        var view = ParseCreateView();
        return new TsqlCreateOrAlterViewStatement
        {
            Span = SourceSpan.From(createKeyword.Span, view.Span),
            CreateKeyword = createKeyword,
            OrKeyword = orKeyword,
            View = view,
        };
    }

    // ... PROC[EDURE] name [;number] [(] @param, ... [)] [WITH option, ...] [FOR REPLICATION] AS {statements | EXTERNAL NAME}
    private TsqlCreateProcedureStatement ParseTsqlProcedure()
    {
        var head = ParseTsqlModuleHead();
        var procedureKeyword = Advance();
        var name = ParseQualifiedName();
        var (numberSemicolon, number) = ParseTsqlProcedureNumber();
        var parameters = ParseTsqlProcedureParameters();
        var (withKeyword, options) = ParseTsqlModuleOptions();
        SyntaxToken? forKeyword = null;
        SyntaxToken? replicationKeyword = null;
        if (_current.Kind == SyntaxKind.ForKeyword)
        {
            forKeyword = Advance();
            replicationKeyword = ExpectIdent(Keyword.ProcReplication);
        }

        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var externalName = TryParseTsqlExternalName();
        var statements = externalName is null ? ParseTsqlBody() : [];
        return new TsqlCreateProcedureStatement
        {
            Span = SourceSpan.From(head.Start, externalName?.Span ?? statements[^1].Span),
            CreateKeyword = head.CreateKeyword,
            OrKeyword = head.OrKeyword,
            AlterKeyword = head.AlterKeyword,
            ProcedureKeyword = procedureKeyword,
            Name = name,
            NumberSemicolon = numberSemicolon,
            Number = number,
            OpenParen = parameters.OpenParen,
            Parameters = parameters.Parameters,
            CloseParen = parameters.CloseParen,
            WithKeyword = withKeyword,
            Options = options,
            ForKeyword = forKeyword,
            ReplicationKeyword = replicationKeyword,
            AsKeyword = asKeyword,
            Statements = statements,
            ExternalName = externalName,
        };
    }

    private readonly record struct TsqlParameterList(SyntaxToken? OpenParen, IReadOnlyList<TsqlParameter> Parameters, SyntaxToken? CloseParen);

    // A procedure's parameter list may be parenthesized or bare.
    private TsqlParameterList ParseTsqlProcedureParameters()
    {
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            var openParen = Advance();
            IReadOnlyList<TsqlParameter> list = _current.Kind == SyntaxKind.CloseParen ? [] : ParseTsqlParameters();
            return new TsqlParameterList(openParen, list, Expect(SyntaxKind.CloseParen));
        }

        return _current.Kind == SyntaxKind.EmbeddedHost ? new TsqlParameterList(null, ParseTsqlParameters(), null) : new TsqlParameterList(null, [], null);
    }

    private List<TsqlParameter> ParseTsqlParameters()
    {
        var parameters = new List<TsqlParameter> { ParseTsqlParameter() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            parameters.Add(ParseTsqlParameter());
        }

        return parameters;
    }

    // @name [AS] {type | CURSOR} [VARYING] [[NOT] NULL] [= default] [OUT | OUTPUT] [READONLY]
    private TsqlParameter ParseTsqlParameter()
    {
        var name = Expect(SyntaxKind.EmbeddedHost);
        var asKeyword = _current.Kind == SyntaxKind.AsKeyword ? Advance() : (SyntaxToken?)null;
        var cursorKeyword = AcceptTsqlWord(Keyword.Cursor);
        var dataType = cursorKeyword is null ? ParseDataType() : null;
        var varyingKeyword = AcceptTsqlWord(Keyword.Varying);
        var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
        var nullKeyword = notKeyword is not null || _current.Kind == SyntaxKind.NullKeyword ? Expect(SyntaxKind.NullKeyword) : (SyntaxToken?)null;
        var equalsToken = _current.Kind == SyntaxKind.EqualsToken ? Advance() : (SyntaxToken?)null;
        var defaultValue = equalsToken is null ? null : ParseExpression();
        var outputKeyword = IdentifierEquals(Keyword.Output) || IdentifierEquals(Keyword.Out) ? Advance() : (SyntaxToken?)null;
        var readonlyKeyword = AcceptTsqlWord(Keyword.ProcReadonly);
        return new TsqlParameter
        {
            Span = SourceSpan.From(name, TsqlPreviousToken()),
            Name = name,
            AsKeyword = asKeyword,
            DataType = dataType,
            CursorKeyword = cursorKeyword,
            VaryingKeyword = varyingKeyword,
            NotKeyword = notKeyword,
            NullKeyword = nullKeyword,
            EqualsToken = equalsToken,
            Default = defaultValue,
            OutputKeyword = outputKeyword,
            ReadonlyKeyword = readonlyKeyword,
        };
    }

    // [WITH option, ...] before a module's AS (or a trigger's FOR / AFTER / INSTEAD OF).
    private (SyntaxToken? WithKeyword, IReadOnlyList<TsqlOption> Options) ParseTsqlModuleOptions()
    {
        if (_current.Kind != SyntaxKind.WithKeyword)
        {
            return (null, []);
        }

        var withKeyword = Advance();
        var options = new List<TsqlOption> { ParseTsqlModuleOption() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            options.Add(ParseTsqlModuleOption());
        }

        return (withKeyword, options);
    }

    // ENCRYPTION | RECOMPILE | SCHEMABINDING | NATIVE_COMPILATION | RETURNS NULL ON NULL INPUT |
    // CALLED ON NULL INPUT | EXECUTE AS {CALLER | SELF | OWNER | 'name'} | INLINE = {ON | OFF}
    private TsqlOption ParseTsqlModuleOption()
    {
        if (IdentifierEquals(Keyword.Exec) || IdentifierEquals(Keyword.Execute))
        {
            var executeKeyword = Advance();
            var asKeyword = Expect(SyntaxKind.AsKeyword);
            var principal = Advance();
            return new TsqlOption { Span = SourceSpan.From(executeKeyword, principal), Name = [executeKeyword, asKeyword], Value = [principal] };
        }

        if (IdentifierEquals(Keyword.Returns) || IdentifierEquals(Keyword.Called))
        {
            return ParseTsqlNullInputOption();
        }

        var name = Expect(SyntaxKind.Identifier);
        if (_current.Kind != SyntaxKind.EqualsToken)
        {
            return new TsqlOption { Span = name.Span, Name = [name] };
        }

        var equalsToken = Advance();
        var value = Advance();
        return new TsqlOption { Span = SourceSpan.From(name, value), Name = [name], EqualsToken = equalsToken, Value = [value] };
    }

    // RETURNS NULL ON NULL INPUT | CALLED ON NULL INPUT
    private TsqlOption ParseTsqlNullInputOption()
    {
        var words = new List<SyntaxToken> { Advance() };
        if (TokenEquals(words[0], Keyword.Returns))
        {
            words.Add(Expect(SyntaxKind.NullKeyword));
        }

        words.Add(Expect(SyntaxKind.OnKeyword));
        words.Add(Expect(SyntaxKind.NullKeyword));
        words.Add(ExpectIdent(Keyword.Input));
        return new TsqlOption { Span = SourceSpan.From(words[0], words[^1]), Name = words };
    }

    // EXTERNAL NAME assembly.class[.method]
    private TsqlExternalName? TryParseTsqlExternalName()
    {
        if (!IdentifierEquals(Keyword.External) || !NextEquals(Keyword.Name))
        {
            return null;
        }

        var externalKeyword = Advance();
        var nameKeyword = Advance();
        var name = ParseQualifiedName();
        return new TsqlExternalName
        {
            Span = SourceSpan.From(externalKeyword, name[^1]),
            ExternalKeyword = externalKeyword,
            NameKeyword = nameKeyword,
            Name = name,
        };
    }

    // ... FUNCTION name ([@param, ...]) RETURNS {type | TABLE | @t TABLE (...)} [WITH option, ...] [AS] body
    private TsqlCreateFunctionStatement ParseTsqlFunction()
    {
        var head = ParseTsqlModuleHead();
        var functionKeyword = Advance();
        var name = ParseQualifiedName();
        var openParen = Expect(SyntaxKind.OpenParen);
        IReadOnlyList<TsqlParameter> parameters = _current.Kind == SyntaxKind.CloseParen ? [] : ParseTsqlParameters();
        var closeParen = Expect(SyntaxKind.CloseParen);
        var returnsKeyword = ExpectIdent(Keyword.Returns);
        var result = ParseTsqlFunctionResult();
        var (withKeyword, options) = ParseTsqlModuleOptions();
        var asKeyword = _current.Kind == SyntaxKind.AsKeyword ? Advance() : (SyntaxToken?)null;
        var externalName = TryParseTsqlExternalName();
        var body = externalName is null ? ParseTsqlFunctionBody() : null;
        return new TsqlCreateFunctionStatement
        {
            Span = SourceSpan.From(head.Start, externalName?.Span ?? body!.Span),
            CreateKeyword = head.CreateKeyword,
            OrKeyword = head.OrKeyword,
            AlterKeyword = head.AlterKeyword,
            FunctionKeyword = functionKeyword,
            Name = name,
            OpenParen = openParen,
            Parameters = parameters,
            CloseParen = closeParen,
            ReturnsKeyword = returnsKeyword,
            ReturnType = result.ReturnType,
            ReturnTableKeyword = result.TableKeyword,
            ReturnVariable = result.Variable,
            ReturnTable = result.Table,
            WithKeyword = withKeyword,
            Options = options,
            AsKeyword = asKeyword,
            Body = body,
            ExternalName = externalName,
        };
    }

    private readonly record struct TsqlFunctionResult(DataType? ReturnType, SyntaxToken? TableKeyword, SyntaxToken? Variable, TsqlTableDefinition? Table);

    private TsqlFunctionResult ParseTsqlFunctionResult()
    {
        if (_current.Kind == SyntaxKind.EmbeddedHost)
        {
            var variable = Advance();
            return new TsqlFunctionResult(null, null, variable, ParseTsqlTableDefinition());
        }

        if (!IdentifierEquals(Keyword.Table))
        {
            return new TsqlFunctionResult(ParseDataType(), null, null, null);
        }

        return NextKind == SyntaxKind.OpenParen
            ? new TsqlFunctionResult(null, null, null, ParseTsqlTableDefinition())
            : new TsqlFunctionResult(null, Advance(), null, null);
    }

    // BEGIN ... END | BEGIN ATOMIC WITH (...) ... END | RETURN [(] select [)]
    private Query ParseTsqlFunctionBody()
    {
        if (IdentifierEquals(Keyword.Return))
        {
            return ParseTsqlInlineReturn();
        }

        if (!IdentifierEquals(Keyword.Begin))
        {
            throw new SqlParseException($"Expected BEGIN or RETURN, found {_current.Kind}", _current.Position);
        }

        return NextEquals(Keyword.Atomic) ? ParseTsqlAtomicBlock() : ParseTsqlBlock();
    }

    // CREATE AGGREGATE name (@param type, ...) RETURNS type EXTERNAL NAME assembly.class
    private TsqlCreateAggregateStatement ParseTsqlCreateAggregate()
    {
        var createKeyword = Advance();
        var aggregateKeyword = Advance();
        var name = ParseQualifiedName();
        var openParen = Expect(SyntaxKind.OpenParen);
        var parameters = ParseTsqlParameters();
        var closeParen = Expect(SyntaxKind.CloseParen);
        var returnsKeyword = ExpectIdent(Keyword.Returns);
        var returnType = ParseDataType();
        var externalName = TryParseTsqlExternalName()
            ?? throw new SqlParseException($"Expected EXTERNAL NAME, found {_current.Kind}", _current.Position);
        return new TsqlCreateAggregateStatement
        {
            Span = SourceSpan.From(createKeyword.Span, externalName.Span),
            CreateKeyword = createKeyword,
            AggregateKeyword = aggregateKeyword,
            Name = name,
            OpenParen = openParen,
            Parameters = parameters,
            CloseParen = closeParen,
            ReturnsKeyword = returnsKeyword,
            ReturnType = returnType,
            ExternalName = externalName,
        };
    }
}
