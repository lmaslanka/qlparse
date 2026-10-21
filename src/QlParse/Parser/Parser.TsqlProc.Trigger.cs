namespace QlParse;

// T-SQL triggers (DML, DDL and logon), ENABLE / DISABLE TRIGGER and CREATE EVENT NOTIFICATION.
internal sealed partial class Parser
{
    private readonly record struct TsqlTriggerScope(
        IReadOnlyList<SyntaxToken> Target,
        SyntaxToken? DatabaseKeyword,
        SyntaxToken? AllKeyword,
        SyntaxToken? ServerKeyword);

    private readonly record struct TsqlTriggerTiming(SyntaxToken Keyword, SyntaxToken? OfKeyword);

    private readonly record struct TsqlTriggerTail(
        SyntaxToken? AppendWithKeyword,
        SyntaxToken? AppendKeyword,
        SyntaxToken? NotKeyword,
        SyntaxToken? ReplicationForKeyword,
        SyntaxToken? ReplicationKeyword);

    // ... TRIGGER name ON {table | DATABASE | ALL SERVER} [WITH option, ...] {FOR | AFTER | INSTEAD OF} event, ...
    //     [WITH APPEND] [NOT FOR REPLICATION] AS {statements | EXTERNAL NAME a.c.m}
    private TsqlCreateTriggerStatement ParseTsqlTrigger()
    {
        var head = ParseTsqlModuleHead();
        var triggerKeyword = Advance();
        var name = ParseQualifiedName();
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var scope = ParseTsqlTriggerScope();
        var (withKeyword, options) = ParseTsqlModuleOptions();
        var timing = ParseTsqlTriggerTiming();
        var events = ParseTsqlEventList();
        var tail = ParseTsqlTriggerTail();
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var externalName = TryParseTsqlExternalName();
        var statements = externalName is null ? ParseTsqlBody() : [];
        return new TsqlCreateTriggerStatement
        {
            Span = SourceSpan.From(head.Start, externalName?.Span ?? statements[^1].Span),
            CreateKeyword = head.CreateKeyword,
            OrKeyword = head.OrKeyword,
            AlterKeyword = head.AlterKeyword,
            TriggerKeyword = triggerKeyword,
            Name = name,
            OnKeyword = onKeyword,
            Target = scope.Target,
            DatabaseKeyword = scope.DatabaseKeyword,
            AllKeyword = scope.AllKeyword,
            ServerKeyword = scope.ServerKeyword,
            WithKeyword = withKeyword,
            Options = options,
            TimingKeyword = timing.Keyword,
            OfKeyword = timing.OfKeyword,
            Events = events,
            AppendWithKeyword = tail.AppendWithKeyword,
            AppendKeyword = tail.AppendKeyword,
            NotKeyword = tail.NotKeyword,
            ReplicationForKeyword = tail.ReplicationForKeyword,
            ReplicationKeyword = tail.ReplicationKeyword,
            AsKeyword = asKeyword,
            Statements = statements,
            ExternalName = externalName,
        };
    }

    // table | DATABASE | ALL SERVER
    private TsqlTriggerScope ParseTsqlTriggerScope()
    {
        if (IdentifierEquals(Keyword.ProcDatabase))
        {
            return new TsqlTriggerScope([], Advance(), null, null);
        }

        if (_current.Kind == SyntaxKind.AllKeyword)
        {
            var allKeyword = Advance();
            return new TsqlTriggerScope([], null, allKeyword, ExpectIdent(Keyword.ProcServer));
        }

        return new TsqlTriggerScope(ParseQualifiedName(), null, null, null);
    }

    // FOR | AFTER | INSTEAD OF
    private TsqlTriggerTiming ParseTsqlTriggerTiming()
    {
        if (_current.Kind == SyntaxKind.ForKeyword || IdentifierEquals(Keyword.After))
        {
            return new TsqlTriggerTiming(Advance(), null);
        }

        var insteadKeyword = ExpectIdent(Keyword.Instead);
        return new TsqlTriggerTiming(insteadKeyword, Expect(SyntaxKind.OfKeyword));
    }

    // INSERT, UPDATE, DELETE, or DDL / logon event types and groups (CREATE_TABLE, DDL_DATABASE_LEVEL_EVENTS, LOGON).
    private List<SyntaxToken> ParseTsqlEventList()
    {
        var events = new List<SyntaxToken> { ExpectTsqlEvent() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            events.Add(ExpectTsqlEvent());
        }

        return events;
    }

    private SyntaxToken ExpectTsqlEvent() =>
        _current.Kind == SyntaxKind.UpdateKeyword ? Advance() : Expect(SyntaxKind.Identifier);

    // [WITH APPEND] [NOT FOR REPLICATION]
    private TsqlTriggerTail ParseTsqlTriggerTail()
    {
        SyntaxToken? appendWithKeyword = null;
        SyntaxToken? appendKeyword = null;
        if (_current.Kind == SyntaxKind.WithKeyword)
        {
            appendWithKeyword = Advance();
            appendKeyword = ExpectIdent(Keyword.ProcAppend);
        }

        if (_current.Kind != SyntaxKind.NotKeyword)
        {
            return new TsqlTriggerTail(appendWithKeyword, appendKeyword, null, null, null);
        }

        var notKeyword = Advance();
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        return new TsqlTriggerTail(appendWithKeyword, appendKeyword, notKeyword, forKeyword, ExpectIdent(Keyword.ProcReplication));
    }

    // {ENABLE | DISABLE} TRIGGER {name, ... | ALL} ON {object | DATABASE | ALL SERVER}
    private TsqlToggleTriggerStatement ParseTsqlToggleTrigger()
    {
        var toggleKeyword = Advance();
        var triggerKeyword = Advance();
        var allTriggersKeyword = _current.Kind == SyntaxKind.AllKeyword ? Advance() : (SyntaxToken?)null;
        var triggers = new List<IReadOnlyList<SyntaxToken>>();
        if (allTriggersKeyword is null)
        {
            triggers.Add(ParseQualifiedName());
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                triggers.Add(ParseQualifiedName());
            }
        }

        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var scope = ParseTsqlTriggerScope();
        return new TsqlToggleTriggerStatement
        {
            Span = SourceSpan.From(toggleKeyword, TsqlPreviousToken()),
            ToggleKeyword = toggleKeyword,
            TriggerKeyword = triggerKeyword,
            AllTriggersKeyword = allTriggersKeyword,
            Triggers = triggers,
            OnKeyword = onKeyword,
            Target = scope.Target,
            DatabaseKeyword = scope.DatabaseKeyword,
            AllKeyword = scope.AllKeyword,
            ServerKeyword = scope.ServerKeyword,
        };
    }

    // CREATE EVENT NOTIFICATION name ON {SERVER | DATABASE | QUEUE queue} [WITH FAN_IN] FOR event, ...
    // TO SERVICE 'service', {'broker_instance' | 'current database'}
    private TsqlCreateEventNotificationStatement ParseTsqlCreateEventNotification()
    {
        var createKeyword = Advance();
        var eventKeyword = Advance();
        var notificationKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var scope = Expect(SyntaxKind.Identifier);
        IReadOnlyList<SyntaxToken> queue = TokenEquals(scope, Keyword.ProcQueue) ? ParseQualifiedName() : [];
        SyntaxToken? withKeyword = null;
        SyntaxToken? fanInKeyword = null;
        if (_current.Kind == SyntaxKind.WithKeyword)
        {
            withKeyword = Advance();
            fanInKeyword = ExpectIdent(Keyword.ProcFanIn);
        }

        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var events = ParseTsqlEventList();
        var toKeyword = ExpectIdent(Keyword.To);
        var serviceKeyword = ExpectIdent(Keyword.ProcService);
        var service = Expect(SyntaxKind.String);
        var comma = Expect(SyntaxKind.Comma);
        var brokerInstance = Expect(SyntaxKind.String);
        return new TsqlCreateEventNotificationStatement
        {
            Span = SourceSpan.From(createKeyword, brokerInstance),
            CreateKeyword = createKeyword,
            EventKeyword = eventKeyword,
            NotificationKeyword = notificationKeyword,
            Name = name,
            OnKeyword = onKeyword,
            Scope = scope,
            Queue = queue,
            WithKeyword = withKeyword,
            FanInKeyword = fanInKeyword,
            ForKeyword = forKeyword,
            Events = events,
            ToKeyword = toKeyword,
            ServiceKeyword = serviceKeyword,
            Service = service,
            Comma = comma,
            BrokerInstance = brokerInstance,
        };
    }
}
