namespace QlParse;

internal sealed partial class Parser
{
    private CreateTriggerStatement ParseCreateTrigger()
    {
        var createKeyword = Advance();
        var triggerKeyword = ExpectIdent(Keyword.Trigger);
        var name = ParseQualifiedName();
        SyntaxToken actionTime;
        SyntaxToken? ofKeyword = null;
        if (IdentifierEquals(Keyword.Before) || IdentifierEquals(Keyword.After))
        {
            actionTime = Advance();
        }
        else if (IdentifierEquals(Keyword.Instead))
        {
            actionTime = Advance();
            ofKeyword = Expect(SyntaxKind.OfKeyword);
        }
        else
        {
            throw new SqlParseException($"Expected BEFORE, AFTER, or INSTEAD OF, found {_current.Kind}", _current.Position);
        }

        SyntaxToken eventKeyword;
        if (IdentifierEquals(Keyword.Insert) || IdentifierEquals(Keyword.Delete) || _current.Kind == SyntaxKind.UpdateKeyword)
        {
            eventKeyword = Advance();
        }
        else
        {
            throw new SqlParseException($"Expected INSERT, DELETE, or UPDATE, found {_current.Kind}", _current.Position);
        }

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

        SyntaxToken? forKeyword = null;
        SyntaxToken? eachKeyword = null;
        SyntaxToken? granularity = null;
        if (_current.Kind == SyntaxKind.ForKeyword && NextEquals(Keyword.Each))
        {
            forKeyword = Advance();
            eachKeyword = ExpectIdent(Keyword.Each);
            if (!IdentifierEquals(Keyword.Row) && !IdentifierEquals(Keyword.Statement))
            {
                throw new SqlParseException($"Expected ROW or STATEMENT, found {_current.Kind}", _current.Position);
            }

            granularity = Advance();
        }

        SyntaxToken? whenKeyword = null;
        SyntaxToken? whenOpen = null;
        Expression? when = null;
        SyntaxToken? whenClose = null;
        if (_current.Kind == SyntaxKind.WhenKeyword)
        {
            whenKeyword = Advance();
            whenOpen = Expect(SyntaxKind.OpenParen);
            when = ParseExpression();
            whenClose = Expect(SyntaxKind.CloseParen);
        }

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

    private DropTriggerStatement ParseDropTrigger()
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

    private CreateRoutineStatement ParseCreateFunction() => ParseCreateRoutine(requiresReturns: true, allowsFor: false);

    private CreateRoutineStatement ParseCreateProcedure() => ParseCreateRoutine(requiresReturns: false, allowsFor: false);

    private CreateRoutineStatement ParseCreateMethod() => ParseCreateRoutine(requiresReturns: true, allowsFor: true);

    private CreateRoutineStatement ParseCreateRoutine(bool requiresReturns, bool allowsFor)
    {
        var createKeyword = Advance();
        SyntaxToken? kindPrefix = null;
        if (IdentifierEquals(Keyword.Instance) || IdentifierEquals(Keyword.Static) || IdentifierEquals(Keyword.Constructor))
        {
            kindPrefix = Advance();
        }

        if (!IdentifierEquals(Keyword.Function) && !IdentifierEquals(Keyword.Procedure) && !IdentifierEquals(Keyword.Method))
        {
            throw new SqlParseException($"Expected FUNCTION, PROCEDURE, or METHOD, found {_current.Kind}", _current.Position);
        }

        var routineKeyword = Advance();
        var name = ParseQualifiedName();
        ParseParameterList(out var openParen, out var parameters, out var closeParen);
        SyntaxToken? returnsKeyword = null;
        DataType? returnsType = null;
        SyntaxToken? returnsAs = null;
        SyntaxToken? returnsLocator = null;
        if (requiresReturns || IdentifierEquals(Keyword.Returns))
        {
            returnsKeyword = ExpectIdent(Keyword.Returns);
            returnsType = ParseDataType();
            if (_current.Kind == SyntaxKind.AsKeyword && NextEquals(Keyword.Locator))
            {
                returnsAs = Advance();
                returnsLocator = Advance();
            }
        }

        SyntaxToken? forKeyword = null;
        IReadOnlyList<SyntaxToken>? forType = null;
        if (allowsFor && _current.Kind == SyntaxKind.ForKeyword)
        {
            forKeyword = Advance();
            forType = ParseQualifiedName();
        }
        else if (allowsFor)
        {
            throw new SqlParseException($"Expected FOR, found {_current.Kind}", _current.Position);
        }

        var characteristics = ParseRoutineCharacteristics();
        ParseRoutineBody(out var sqlKeyword, out var body, out var externalKeyword, out var nameKeyword, out var externalName);
        var end = closeParen.Span;
        if (characteristics.Count > 0)
        {
            end = characteristics[^1].Span;
        }

        if (body is not null)
        {
            end = body.Span;
        }
        else if (externalName is SyntaxToken externalNameToken)
        {
            end = externalNameToken.Span;
        }
        else if (externalKeyword is SyntaxToken externalToken)
        {
            end = externalToken.Span;
        }

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

    private AlterRoutineStatement ParseAlterRoutine()
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
            ParseRoutineBody(out sqlKeyword, out body, out externalKeyword, out nameKeyword, out externalName);
        }
        else if (characteristics.Count == 0)
        {
            throw new SqlParseException($"Expected routine characteristic, found {_current.Kind}", _current.Position);
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
        else if (externalName is SyntaxToken externalNameToken)
        {
            end = externalNameToken.Span;
        }
        else if (externalKeyword is SyntaxToken externalToken)
        {
            end = externalToken.Span;
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

    private DropRoutineStatement ParseDropRoutine()
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

    private CallStatement ParseCall()
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

    private ReturnStatement ParseReturn()
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
