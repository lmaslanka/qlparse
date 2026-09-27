namespace QlParse;

internal sealed partial class Parser
{
    internal PrepareStatement ParsePrepare()
    {
        var prepareKeyword = Advance();
        var name = ParseScopedName(out var scope);
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var source = ParseSessionValue();
        return new PrepareStatement
        {
            Span = SourceSpan.From(prepareKeyword, source),
            PrepareKeyword = prepareKeyword,
            Scope = scope,
            Name = name,
            FromKeyword = fromKeyword,
            Source = source,
        };
    }

    internal ExecuteStatement ParseExecute()
    {
        var executeKeyword = Advance();
        var name = ParseScopedName(out var scope);
        var into = ParseExecuteIntoClause();
        var intoKeyword = into.IntoKeyword;
        var intoSql = into.IntoSqlKeyword;
        var intoDescriptorKeyword = into.IntoDescriptorKeyword;
        var intoDescriptorScope = into.IntoDescriptorScope;
        var intoDescriptor = into.IntoDescriptor;
        var targets = into.Targets;
        var usingClause = ParseExecuteUsingClause();
        var usingKeyword = usingClause.UsingKeyword;
        var usingSql = usingClause.UsingSqlKeyword;
        var usingDescriptorKeyword = usingClause.UsingDescriptorKeyword;
        var usingDescriptorScope = usingClause.UsingDescriptorScope;
        var usingDescriptor = usingClause.UsingDescriptor;
        var arguments = usingClause.Arguments;
        var end = name.Span;
        if (targets is { Count: > 0 })
        {
            end = targets[^1].Span;
        }

        if (intoDescriptor is SyntaxToken intoDescriptorToken)
        {
            end = intoDescriptorToken.Span;
        }

        if (arguments is { Count: > 0 })
        {
            end = arguments[^1].Span;
        }

        if (usingDescriptor is SyntaxToken usingDescriptorToken)
        {
            end = usingDescriptorToken.Span;
        }

        return new ExecuteStatement
        {
            Span = SourceSpan.From(executeKeyword, end),
            ExecuteKeyword = executeKeyword,
            Scope = scope,
            Name = name,
            IntoKeyword = intoKeyword,
            IntoSqlKeyword = intoSql,
            IntoDescriptorKeyword = intoDescriptorKeyword,
            IntoDescriptorScope = intoDescriptorScope,
            IntoDescriptor = intoDescriptor,
            Targets = targets,
            UsingKeyword = usingKeyword,
            UsingSqlKeyword = usingSql,
            UsingDescriptorKeyword = usingDescriptorKeyword,
            UsingDescriptorScope = usingDescriptorScope,
            UsingDescriptor = usingDescriptor,
            Arguments = arguments,
        };
    }

    private readonly record struct ExecuteIntoClause(
        SyntaxToken? IntoKeyword,
        SyntaxToken? IntoSqlKeyword,
        SyntaxToken? IntoDescriptorKeyword,
        SyntaxToken? IntoDescriptorScope,
        SyntaxToken? IntoDescriptor,
        IReadOnlyList<SyntaxToken>? Targets);

    private ExecuteIntoClause ParseExecuteIntoClause()
    {
        if (!IdentifierEquals(Keyword.Into))
        {
            return new ExecuteIntoClause(null, null, null, null, null, null);
        }

        var intoKeyword = Advance();
        if (!IsDescriptorStart())
        {
            return new ExecuteIntoClause(intoKeyword, null, null, null, null, ParseIntoTargets());
        }

        ParseDescriptorName(out var intoSql, out var intoDescriptorKeyword, out var intoDescriptorScope, out var intoDescriptor);
        return new ExecuteIntoClause(intoKeyword, intoSql, intoDescriptorKeyword, intoDescriptorScope, intoDescriptor, null);
    }

    private readonly record struct ExecuteUsingClause(
        SyntaxToken? UsingKeyword,
        SyntaxToken? UsingSqlKeyword,
        SyntaxToken? UsingDescriptorKeyword,
        SyntaxToken? UsingDescriptorScope,
        SyntaxToken? UsingDescriptor,
        IReadOnlyList<SyntaxToken>? Arguments);

    private ExecuteUsingClause ParseExecuteUsingClause()
    {
        if (_current.Kind != SyntaxKind.UsingKeyword)
        {
            return new ExecuteUsingClause(null, null, null, null, null, null);
        }

        var usingKeyword = Advance();
        if (!IsDescriptorStart())
        {
            return new ExecuteUsingClause(usingKeyword, null, null, null, null, ParseUsingArguments());
        }

        ParseDescriptorName(out var usingSql, out var usingDescriptorKeyword, out var usingDescriptorScope, out var usingDescriptor);
        return new ExecuteUsingClause(usingKeyword, usingSql, usingDescriptorKeyword, usingDescriptorScope, usingDescriptor, null);
    }

    internal ExecuteImmediateStatement ParseExecuteImmediate()
    {
        var executeKeyword = Advance();
        var immediateKeyword = ExpectIdent(Keyword.Immediate);
        var source = ParseSessionValue();
        return new ExecuteImmediateStatement
        {
            Span = SourceSpan.From(executeKeyword, source),
            ExecuteKeyword = executeKeyword,
            ImmediateKeyword = immediateKeyword,
            Source = source,
        };
    }

    internal DescribeStatement ParseDescribe()
    {
        var describeKeyword = Advance();
        SyntaxToken? inputOrOutput = null;
        if (IdentifierEquals(Keyword.Input) || IdentifierEquals(Keyword.Output))
        {
            inputOrOutput = Advance();
        }

        var name = ParseScopedName(out var scope);
        SyntaxToken usingOrInto;
        if (_current.Kind == SyntaxKind.UsingKeyword || IdentifierEquals(Keyword.Into))
        {
            usingOrInto = Advance();
        }
        else
        {
            throw new SqlParseException($"Expected USING or INTO, found {_current.Kind}", _current.Position);
        }

        ParseDescriptorName(out var sql, out var descriptorKeyword, out var descriptorScope, out var descriptor);
        return new DescribeStatement
        {
            Span = SourceSpan.From(describeKeyword, descriptor),
            DescribeKeyword = describeKeyword,
            InputOrOutput = inputOrOutput,
            Scope = scope,
            Name = name,
            UsingOrInto = usingOrInto,
            SqlKeyword = sql,
            DescriptorKeyword = descriptorKeyword,
            DescriptorScope = descriptorScope,
            Descriptor = descriptor,
        };
    }

    internal AllocateDescriptorStatement ParseAllocateDescriptor()
    {
        var allocateKeyword = Advance();
        SyntaxToken? sql = null;
        if (IdentifierEquals(Keyword.Sql))
        {
            sql = Advance();
        }

        var descriptorKeyword = ExpectIdent(Keyword.Descriptor);
        var name = ParseScopedName(out var scope);
        SyntaxToken? withKeyword = null;
        SyntaxToken? maxKeyword = null;
        SyntaxToken? occurrences = null;
        var end = name.Span;
        if (_current.Kind == SyntaxKind.WithKeyword)
        {
            withKeyword = Advance();
            maxKeyword = ExpectIdent(Keyword.Max);
            if (!IsSessionValue() && _current.Kind != SyntaxKind.Number)
            {
                throw new SqlParseException($"Expected occurrences, found {_current.Kind}", _current.Position);
            }

            var count = Advance();
            occurrences = count;
            end = count.Span;
        }

        return new AllocateDescriptorStatement
        {
            Span = SourceSpan.From(allocateKeyword, end),
            AllocateKeyword = allocateKeyword,
            SqlKeyword = sql,
            DescriptorKeyword = descriptorKeyword,
            Scope = scope,
            Name = name,
            WithKeyword = withKeyword,
            MaxKeyword = maxKeyword,
            Occurrences = occurrences,
        };
    }

    internal GetDiagnosticsStatement ParseGetDiagnostics()
    {
        var getKeyword = Advance();
        var diagnosticsKeyword = ExpectIdent(Keyword.Diagnostics);
        SyntaxToken? conditionKeyword = null;
        Expression? conditionNumber = null;
        if (IdentifierEquals(Keyword.Exception) || IdentifierEquals(Keyword.Condition))
        {
            conditionKeyword = Advance();
            conditionNumber = ParseExpression();
        }

        var items = ParseDiagnosticsItems();
        return new GetDiagnosticsStatement
        {
            Span = SourceSpan.From(getKeyword, items[^1].Span),
            GetKeyword = getKeyword,
            DiagnosticsKeyword = diagnosticsKeyword,
            ConditionKeyword = conditionKeyword,
            ConditionNumber = conditionNumber,
            Items = items,
        };
    }

    internal SignalStatement ParseSignal()
    {
        var signalKeyword = Advance();
        ParseSignalValue(required: true, out var sqlState, out var valueKeyword, out var state, out var condition);
        ParseSignalSet(out var setKeyword, out var items);
        var end = SignalEnd(signalKeyword, state, condition, items);

        return new SignalStatement
        {
            Span = SourceSpan.From(signalKeyword, end),
            SignalKeyword = signalKeyword,
            SqlStateKeyword = sqlState,
            ValueKeyword = valueKeyword,
            State = state,
            Condition = condition,
            SetKeyword = setKeyword,
            Items = items,
        };
    }

    internal ResignalStatement ParseResignal()
    {
        var resignalKeyword = Advance();
        ParseSignalValue(required: false, out var sqlState, out var valueKeyword, out var state, out var condition);
        ParseSignalSet(out var setKeyword, out var items);
        var end = SignalEnd(resignalKeyword, state, condition, items);

        return new ResignalStatement
        {
            Span = SourceSpan.From(resignalKeyword, end),
            ResignalKeyword = resignalKeyword,
            SqlStateKeyword = sqlState,
            ValueKeyword = valueKeyword,
            State = state,
            Condition = condition,
            SetKeyword = setKeyword,
            Items = items,
        };
    }

    private SyntaxToken ParseScopedName(out SyntaxToken? scope)
    {
        scope = null;
        if (IdentifierEquals(Keyword.Global) || IdentifierEquals(Keyword.Local))
        {
            scope = Advance();
        }

        return ParseCursorName();
    }

    private void ParseDescriptorName(
        out SyntaxToken? sql,
        out SyntaxToken descriptorKeyword,
        out SyntaxToken? scope,
        out SyntaxToken name)
    {
        sql = null;
        if (IdentifierEquals(Keyword.Sql))
        {
            sql = Advance();
        }

        descriptorKeyword = ExpectIdent(Keyword.Descriptor);
        name = ParseScopedName(out scope);
    }

    private List<SyntaxToken> ParseUsingArguments()
    {
        var arguments = new List<SyntaxToken> { ParseUsingArgument() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            arguments.Add(ParseUsingArgument());
        }

        return arguments;
    }

    private SyntaxToken ParseUsingArgument()
    {
        if (IsSessionValue() || _current.Kind == SyntaxKind.Number)
        {
            return Advance();
        }

        throw new SqlParseException($"Expected using argument, found {_current.Kind}", _current.Position);
    }

    private List<DiagnosticsItem> ParseDiagnosticsItems()
    {
        var items = new List<DiagnosticsItem> { ParseDiagnosticsItem() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParseDiagnosticsItem());
        }

        return items;
    }

    private DiagnosticsItem ParseDiagnosticsItem()
    {
        var target = ParseIntoTarget();
        var equals = Expect(SyntaxKind.EqualsToken);
        var name = Expect(SyntaxKind.Identifier);
        return new DiagnosticsItem
        {
            Span = SourceSpan.From(target, name),
            Target = target,
            EqualsToken = equals,
            Name = name,
        };
    }

    private void ParseSignalValue(
        bool required,
        out SyntaxToken? sqlState,
        out SyntaxToken? valueKeyword,
        out SyntaxToken? state,
        out SyntaxToken? condition)
    {
        sqlState = null;
        valueKeyword = null;
        state = null;
        condition = null;
        if (IdentifierEquals(Keyword.SqlState))
        {
            sqlState = Advance();
            if (IdentifierEquals(Keyword.Value))
            {
                valueKeyword = Advance();
            }

            if (_current.Kind is not (SyntaxKind.String or SyntaxKind.QuestionMark or SyntaxKind.EmbeddedHost))
            {
                throw new SqlParseException($"Expected SQLSTATE value, found {_current.Kind}", _current.Position);
            }

            state = Advance();
            return;
        }

        if (_current.Kind == SyntaxKind.Identifier)
        {
            condition = Advance();
            return;
        }

        if (required)
        {
            throw new SqlParseException($"Expected signal value, found {_current.Kind}", _current.Position);
        }
    }

    private void ParseSignalSet(out SyntaxToken? setKeyword, out IReadOnlyList<SignalInformation> items)
    {
        setKeyword = null;
        if (_current.Kind != SyntaxKind.SetKeyword)
        {
            items = [];
            return;
        }

        setKeyword = Advance();
        var list = new List<SignalInformation> { ParseSignalInformation() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            list.Add(ParseSignalInformation());
        }

        items = list;
    }

    private SignalInformation ParseSignalInformation()
    {
        var name = Expect(SyntaxKind.Identifier);
        var equals = Expect(SyntaxKind.EqualsToken);
        var value = ParseExpression();
        return new SignalInformation
        {
            Span = SourceSpan.From(name, value.Span),
            Name = name,
            EqualsToken = equals,
            Value = value,
        };
    }

    private static SourceSpan SignalEnd(
        SyntaxToken start,
        SyntaxToken? state,
        SyntaxToken? condition,
        IReadOnlyList<SignalInformation> items)
    {
        if (items.Count > 0)
        {
            return items[^1].Span;
        }

        if (condition is SyntaxToken conditionToken)
        {
            return conditionToken.Span;
        }

        if (state is SyntaxToken stateToken)
        {
            return stateToken.Span;
        }

        return start.Span;
    }

    private bool IsDescriptorStart() =>
        IdentifierEquals(Keyword.Sql) || IdentifierEquals(Keyword.Descriptor);
}
