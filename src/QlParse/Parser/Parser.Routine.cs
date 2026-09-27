namespace QlParse;

internal sealed partial class Parser
{
    internal CreateTriggerStatement ParseCreateTrigger()
    {
        var createKeyword = Advance();
        var triggerKeyword = ExpectIdent(Keyword.Trigger);
        var name = ParseQualifiedName();
        var actionTimeClause = ParseTriggerActionTime();
        var actionTime = actionTimeClause.ActionTime;
        var ofKeyword = actionTimeClause.OfKeyword;
        var eventKeyword = ParseTriggerEvent();

        SyntaxToken? columnsOf = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        if (eventKeyword.Kind == SyntaxKind.UpdateKeyword && _current.Kind == SyntaxKind.OfKeyword)
        {
            columnsOf = Advance();
            columns = ParseNameList();
        }

        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var tableName = ParseQualifiedName();
        SyntaxToken? referencing = null;
        IReadOnlyList<TransitionClause> transitions = [];
        if (IdentifierEquals(Keyword.Referencing))
        {
            referencing = Advance();
            transitions = ParseTransitions();
        }

        var granularityClause = ParseTriggerGranularity();
        var forKeyword = granularityClause.ForKeyword;
        var eachKeyword = granularityClause.EachKeyword;
        var granularity = granularityClause.Granularity;
        var whenClause = ParseTriggerWhenClause();
        var whenKeyword = whenClause.WhenKeyword;
        var whenOpen = whenClause.WhenOpen;
        var when = whenClause.When;
        var whenClose = whenClause.WhenClose;
        var body = ParseStatement();
        return new CreateTriggerStatement
        {
            Span = SourceSpan.From(createKeyword, body.Span),
            CreateKeyword = createKeyword,
            TriggerKeyword = triggerKeyword,
            Name = name,
            ActionTime = actionTime,
            OfKeyword = ofKeyword,
            Event = eventKeyword,
            ColumnsOfKeyword = columnsOf,
            Columns = columns,
            OnKeyword = onKeyword,
            TableName = tableName,
            ReferencingKeyword = referencing,
            Transitions = transitions,
            ForKeyword = forKeyword,
            EachKeyword = eachKeyword,
            Granularity = granularity,
            WhenKeyword = whenKeyword,
            WhenOpen = whenOpen,
            When = when,
            WhenClose = whenClose,
            Body = body,
        };
    }

    private readonly record struct TriggerGranularity(SyntaxToken? ForKeyword, SyntaxToken? EachKeyword, SyntaxToken? Granularity);

    private TriggerGranularity ParseTriggerGranularity()
    {
        if (_current.Kind != SyntaxKind.ForKeyword || !NextEquals(Keyword.Each))
        {
            return new TriggerGranularity(null, null, null);
        }

        var forKeyword = Advance();
        var eachKeyword = ExpectIdent(Keyword.Each);
        if (!IdentifierEquals(Keyword.Row) && !IdentifierEquals(Keyword.Statement))
        {
            throw new SqlParseException($"Expected ROW or STATEMENT, found {_current.Kind}", _current.Position);
        }

        return new TriggerGranularity(forKeyword, eachKeyword, Advance());
    }

    private readonly record struct TriggerWhenClause(SyntaxToken? WhenKeyword, SyntaxToken? WhenOpen, Expression? When, SyntaxToken? WhenClose);

    private TriggerWhenClause ParseTriggerWhenClause()
    {
        if (_current.Kind != SyntaxKind.WhenKeyword)
        {
            return new TriggerWhenClause(null, null, null, null);
        }

        var whenKeyword = Advance();
        var whenOpen = Expect(SyntaxKind.OpenParen);
        var when = ParseExpression();
        var whenClose = Expect(SyntaxKind.CloseParen);
        return new TriggerWhenClause(whenKeyword, whenOpen, when, whenClose);
    }

    private readonly record struct TriggerActionTime(SyntaxToken ActionTime, SyntaxToken? OfKeyword);

    private TriggerActionTime ParseTriggerActionTime()
    {
        if (IdentifierEquals(Keyword.Before) || IdentifierEquals(Keyword.After))
        {
            return new TriggerActionTime(Advance(), null);
        }

        if (IdentifierEquals(Keyword.Instead))
        {
            var actionTime = Advance();
            return new TriggerActionTime(actionTime, Expect(SyntaxKind.OfKeyword));
        }

        throw new SqlParseException($"Expected BEFORE, AFTER, or INSTEAD OF, found {_current.Kind}", _current.Position);
    }

    private SyntaxToken ParseTriggerEvent()
    {
        if (IdentifierEquals(Keyword.Insert) || IdentifierEquals(Keyword.Delete) || _current.Kind == SyntaxKind.UpdateKeyword)
        {
            return Advance();
        }

        throw new SqlParseException($"Expected INSERT, DELETE, or UPDATE, found {_current.Kind}", _current.Position);
    }

    internal DropTriggerStatement ParseDropTrigger()
    {
        var dropKeyword = Advance();
        var triggerKeyword = ExpectIdent(Keyword.Trigger);
        var name = ParseQualifiedName();
        return new DropTriggerStatement
        {
            Span = SourceSpan.From(dropKeyword, name[^1]),
            DropKeyword = dropKeyword,
            TriggerKeyword = triggerKeyword,
            Name = name,
        };
    }

    internal CreateRoutineStatement ParseCreateFunction() => ParseCreateRoutine(requiresReturns: true, allowsFor: false);

    internal CreateRoutineStatement ParseCreateProcedure() => ParseCreateRoutine(requiresReturns: false, allowsFor: false);

    internal CreateRoutineStatement ParseCreateMethod() => ParseCreateRoutine(requiresReturns: true, allowsFor: true);

    private CreateRoutineStatement ParseCreateRoutine(bool requiresReturns, bool allowsFor)
    {
        var createKeyword = Advance();
        var kindPrefix = ParseRoutineKindPrefix();
        var routineKeyword = ParseRoutineTypeKeyword();
        var name = ParseQualifiedName();
        ParseParameterList(out var openParen, out var parameters, out var closeParen);
        var returns = ParseRoutineReturnsClause(requiresReturns);
        var returnsKeyword = returns.ReturnsKeyword;
        var returnsType = returns.ReturnsType;
        var returnsAs = returns.ReturnsAsKeyword;
        var returnsLocator = returns.ReturnsLocator;
        var forClause = ParseRoutineForClause(allowsFor);
        var forKeyword = forClause.ForKeyword;
        var forType = forClause.ForType;
        var characteristics = ParseRoutineCharacteristics();
        ParseRoutineBody(out var sqlKeyword, out var body, out var externalKeyword, out var nameKeyword, out var externalName);
        var end = body?.Span
            ?? externalName?.Span
            ?? externalKeyword?.Span
            ?? (characteristics.Count > 0 ? characteristics[^1].Span : (SourceSpan?)null)
            ?? closeParen.Span;

        return new CreateRoutineStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            KindPrefix = kindPrefix,
            RoutineKeyword = routineKeyword,
            Name = name,
            OpenParen = openParen,
            Parameters = parameters,
            CloseParen = closeParen,
            ReturnsKeyword = returnsKeyword,
            ReturnsType = returnsType,
            ReturnsAsKeyword = returnsAs,
            ReturnsLocator = returnsLocator,
            ForKeyword = forKeyword,
            ForType = forType,
            Characteristics = characteristics,
            SqlKeyword = sqlKeyword,
            Body = body,
            ExternalKeyword = externalKeyword,
            NameKeyword = nameKeyword,
            ExternalName = externalName,
        };
    }

    private SyntaxToken? ParseRoutineKindPrefix() =>
        IdentifierEquals(Keyword.Instance) || IdentifierEquals(Keyword.Static) || IdentifierEquals(Keyword.Constructor)
            ? Advance()
            : null;

    private SyntaxToken ParseRoutineTypeKeyword()
    {
        if (!IdentifierEquals(Keyword.Function) && !IdentifierEquals(Keyword.Procedure) && !IdentifierEquals(Keyword.Method))
        {
            throw new SqlParseException($"Expected FUNCTION, PROCEDURE, or METHOD, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    private readonly record struct RoutineReturnsClause(
        SyntaxToken? ReturnsKeyword, DataType? ReturnsType, SyntaxToken? ReturnsAsKeyword, SyntaxToken? ReturnsLocator);

    private RoutineReturnsClause ParseRoutineReturnsClause(bool requiresReturns)
    {
        if (!requiresReturns && !IdentifierEquals(Keyword.Returns))
        {
            return new RoutineReturnsClause(null, null, null, null);
        }

        var returnsKeyword = ExpectIdent(Keyword.Returns);
        var returnsType = ParseDataType();
        SyntaxToken? returnsAs = null;
        SyntaxToken? returnsLocator = null;
        if (_current.Kind == SyntaxKind.AsKeyword && NextEquals(Keyword.Locator))
        {
            returnsAs = Advance();
            returnsLocator = Advance();
        }

        return new RoutineReturnsClause(returnsKeyword, returnsType, returnsAs, returnsLocator);
    }

    private readonly record struct RoutineForClause(SyntaxToken? ForKeyword, IReadOnlyList<SyntaxToken>? ForType);

    private RoutineForClause ParseRoutineForClause(bool allowsFor)
    {
        if (allowsFor && _current.Kind == SyntaxKind.ForKeyword)
        {
            var forKeyword = Advance();
            return new RoutineForClause(forKeyword, ParseQualifiedName());
        }

        if (allowsFor)
        {
            throw new SqlParseException($"Expected FOR, found {_current.Kind}", _current.Position);
        }

        return new RoutineForClause(null, null);
    }

    internal AlterRoutineStatement ParseAlterRoutine()
    {
        var alterKeyword = Advance();
        var designator = ParseRoutineDesignator();
        var characteristics = ParseRoutineCharacteristics();
        SyntaxToken? sqlKeyword = null;
        Query? body = null;
        SyntaxToken? externalKeyword = null;
        SyntaxToken? nameKeyword = null;
        SyntaxToken? externalName = null;
        if (IdentifierEquals(Keyword.Sql) || IdentifierEquals(Keyword.External))
        {
            ParseRoutineBody(out var sqlToken, out var bodyQuery, out var externalToken, out var nameToken, out var externalNameToken);
            sqlKeyword = sqlToken;
            body = bodyQuery;
            externalKeyword = externalToken;
            nameKeyword = nameToken;
            externalName = externalNameToken;
        }
        else
        {
            if (characteristics.Count == 0)
            {
                throw new SqlParseException($"Expected routine characteristic, found {_current.Kind}", _current.Position);
            }
        }

        var end = designator.Span;
        if (characteristics.Count > 0)
        {
            end = characteristics[^1].Span;
        }

        if (body is not null)
        {
            end = body.Span;
        }
        else
        {
            if (externalName is SyntaxToken externalNameToken)
            {
                end = externalNameToken.Span;
            }
            else
            {
                if (externalKeyword is SyntaxToken externalToken)
                {
                    end = externalToken.Span;
                }
            }
        }

        return new AlterRoutineStatement
        {
            Span = SourceSpan.From(alterKeyword, end),
            AlterKeyword = alterKeyword,
            Designator = designator,
            Characteristics = characteristics,
            SqlKeyword = sqlKeyword,
            Body = body,
            ExternalKeyword = externalKeyword,
            NameKeyword = nameKeyword,
            ExternalName = externalName,
        };
    }

    internal DropRoutineStatement ParseDropRoutine()
    {
        var dropKeyword = Advance();
        var designator = ParseRoutineDesignator();
        var behavior = ParseDropBehavior();
        return new DropRoutineStatement
        {
            Span = SourceSpan.From(dropKeyword, behavior),
            DropKeyword = dropKeyword,
            Designator = designator,
            Behavior = behavior,
        };
    }

    internal CallStatement ParseCall()
    {
        var callKeyword = Advance();
        var name = ParseQualifiedName();
        var openParen = Expect(SyntaxKind.OpenParen);
        var arguments = new List<Expression>();
        if (_current.Kind != SyntaxKind.CloseParen)
        {
            arguments.Add(ParseExpression());
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                arguments.Add(ParseExpression());
            }
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new CallStatement
        {
            Span = SourceSpan.From(callKeyword, closeParen),
            CallKeyword = callKeyword,
            Name = name,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
        };
    }

    internal ReturnStatement ParseReturn()
    {
        var returnKeyword = Advance();
        var value = ParseExpression();
        return new ReturnStatement
        {
            Span = SourceSpan.From(returnKeyword, value.Span),
            ReturnKeyword = returnKeyword,
            Value = value,
        };
    }

    private List<TransitionClause> ParseTransitions()
    {
        var transitions = new List<TransitionClause>();
        while (IdentifierEquals(Keyword.Old) || IdentifierEquals(Keyword.New))
        {
            var kind = Advance();
            SyntaxToken? rowOrTable = null;
            if (IdentifierEquals(Keyword.Row) || IdentifierEquals(Keyword.Table))
            {
                rowOrTable = Advance();
            }

            SyntaxToken? asKeyword = null;
            if (_current.Kind == SyntaxKind.AsKeyword)
            {
                asKeyword = Advance();
            }

            var name = Expect(SyntaxKind.Identifier);
            transitions.Add(new TransitionClause
            {
                Span = SourceSpan.From(kind, name),
                Kind = kind,
                RowOrTable = rowOrTable,
                AsKeyword = asKeyword,
                Name = name,
            });
        }

        if (transitions.Count == 0)
        {
            throw new SqlParseException($"Expected OLD or NEW, found {_current.Kind}", _current.Position);
        }

        return transitions;
    }

    private void ParseParameterList(
        out SyntaxToken openParen,
        out IReadOnlyList<RoutineParameter> parameters,
        out SyntaxToken closeParen)
    {
        openParen = Expect(SyntaxKind.OpenParen);
        var items = new List<RoutineParameter>();
        if (_current.Kind != SyntaxKind.CloseParen)
        {
            items.Add(ParseRoutineParameter());
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                items.Add(ParseRoutineParameter());
            }
        }

        parameters = items;
        closeParen = Expect(SyntaxKind.CloseParen);
    }

    private List<RoutineCharacteristic> ParseRoutineCharacteristics()
    {
        var characteristics = new List<RoutineCharacteristic>();
        while (IsCharacteristicStart() || IdentifierEquals(Keyword.Specific))
        {
            if (IdentifierEquals(Keyword.Specific))
            {
                var specific = Advance();
                var specificName = ParseQualifiedName();
                characteristics.Add(new RoutineCharacteristic
                {
                    Span = SourceSpan.From(specific, specificName[^1]),
                    Name = specific,
                    Tail = specificName,
                });
                continue;
            }

            characteristics.Add(ParseCharacteristic());
        }

        return characteristics;
    }

    private void ParseRoutineBody(
        out SyntaxToken? sqlKeyword,
        out Query? body,
        out SyntaxToken? externalKeyword,
        out SyntaxToken? nameKeyword,
        out SyntaxToken? externalName)
    {
        sqlKeyword = null;
        body = null;
        externalKeyword = null;
        nameKeyword = null;
        externalName = null;
        if (IdentifierEquals(Keyword.Sql))
        {
            sqlKeyword = Advance();
            body = ParseStatement();
            return;
        }

        if (!IdentifierEquals(Keyword.External))
        {
            throw new SqlParseException($"Expected SQL or EXTERNAL, found {_current.Kind}", _current.Position);
        }

        externalKeyword = Advance();
        if (!IdentifierEquals(Keyword.Name))
        {
            return;
        }

        nameKeyword = Advance();
        externalName = ParseSessionValue();
    }
}
