namespace QlParse;

internal sealed partial class Parser
{
    private bool IsPrepare() => IdentifierEquals(Keyword.Prepare);

    private bool IsExecuteImmediate() =>
        IdentifierEquals(Keyword.Execute) && NextEquals(Keyword.Immediate);

    private bool IsExecute() => IdentifierEquals(Keyword.Execute);

    private bool IsDescribe() => IdentifierEquals(Keyword.Describe);

    private bool IsAllocateDescriptor()
    {
        if (!IdentifierEquals(Keyword.Allocate))
        {
            return false;
        }

        var index = _index;
        if (IsKeywordAt(index, Keyword.Sql))
        {
            index++;
        }

        return IsKeywordAt(index, Keyword.Descriptor);
    }

    private bool IsGetDiagnostics() =>
        IdentifierEquals(Keyword.Get) && NextEquals(Keyword.Diagnostics);

    private bool IsSignal() => IdentifierEquals(Keyword.Signal);

    private bool IsResignal() => IdentifierEquals(Keyword.Resignal);

    private PrepareStatement ParsePrepare()
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

    private ExecuteStatement ParseExecute()
    {
        var executeKeyword = Advance();
        var name = ParseScopedName(out var scope);
        SyntaxToken? intoKeyword = null;
        SyntaxToken? intoSql = null;
        SyntaxToken? intoDescriptorKeyword = null;
        SyntaxToken? intoDescriptorScope = null;
        SyntaxToken? intoDescriptor = null;
        IReadOnlyList<SyntaxToken>? targets = null;
        if (IdentifierEquals(Keyword.Into))
        {
            intoKeyword = Advance();
            if (IsDescriptorStart())
            {
                ParseDescriptorName(out intoSql, out var intoDescriptorKeywordToken, out intoDescriptorScope, out var intoDescriptorName);
                intoDescriptorKeyword = intoDescriptorKeywordToken;
                intoDescriptor = intoDescriptorName;
            }
            else
            {
                targets = ParseIntoTargets();
            }
        }

        SyntaxToken? usingKeyword = null;
        SyntaxToken? usingSql = null;
        SyntaxToken? usingDescriptorKeyword = null;
        SyntaxToken? usingDescriptorScope = null;
        SyntaxToken? usingDescriptor = null;
        IReadOnlyList<SyntaxToken>? arguments = null;
        if (_current.Kind == SyntaxKind.UsingKeyword)
        {
            usingKeyword = Advance();
            if (IsDescriptorStart())
            {
                ParseDescriptorName(out usingSql, out var usingDescriptorKeywordToken, out usingDescriptorScope, out var usingDescriptorName);
                usingDescriptorKeyword = usingDescriptorKeywordToken;
                usingDescriptor = usingDescriptorName;
            }
            else
            {
                arguments = ParseUsingArguments();
            }
        }

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

    private ExecuteImmediateStatement ParseExecuteImmediate()
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

    private DescribeStatement ParseDescribe()
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

    private AllocateDescriptorStatement ParseAllocateDescriptor()
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

    private GetDiagnosticsStatement ParseGetDiagnostics()
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

    private SignalStatement ParseSignal()
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

    private ResignalStatement ParseResignal()
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
