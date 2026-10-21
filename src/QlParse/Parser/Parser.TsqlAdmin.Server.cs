namespace QlParse;

// Workstream G: server-level administration: ALTER SERVER CONFIGURATION, availability groups, endpoints,
// Resource Governor and extended events sessions.
internal sealed partial class Parser
{
    private const int SettingTwoWords = 2;
    private const int SettingThreeWords = 3;

    private Query? TryParseTsqlServerCreate()
    {
        if (NextEquals(Keyword.AdminAvailability) && IsTsqlGroupAt(_index + 1))
        {
            return ParseTsqlCreateAvailabilityGroup();
        }

        if (NextEquals(Keyword.AdminEndpoint))
        {
            return ParseTsqlEndpoint();
        }

        return TryParseTsqlGovernanceStatement();
    }

    private Query? TryParseTsqlServerAlter()
    {
        if (NextEquals(Keyword.AdminServer) && IsKeywordAt(_index + 1, Keyword.AdminConfiguration))
        {
            return ParseTsqlAlterServerConfiguration();
        }

        if (NextEquals(Keyword.AdminAvailability) && IsTsqlGroupAt(_index + 1))
        {
            return ParseTsqlAlterAvailabilityGroup();
        }

        if (NextEquals(Keyword.AdminEndpoint))
        {
            return ParseTsqlEndpoint();
        }

        if (NextEquals(Keyword.AdminResource) && IsKeywordAt(_index + 1, Keyword.AdminGovernor))
        {
            return ParseTsqlAlterResourceGovernor();
        }

        return TryParseTsqlGovernanceStatement();
    }

    // Shapes CREATE and ALTER share: [EXTERNAL] RESOURCE POOL, WORKLOAD GROUP, EVENT SESSION.
    private Query? TryParseTsqlGovernanceStatement()
    {
        if (NextEquals(Keyword.AdminResource) && IsKeywordAt(_index + 1, Keyword.AdminPool))
        {
            return ParseTsqlResourcePool();
        }

        if (NextEquals(Keyword.External) && IsKeywordAt(_index + 1, Keyword.AdminResource) && IsKeywordAt(_index + 2, Keyword.AdminPool))
        {
            return ParseTsqlResourcePool();
        }

        if (NextEquals(Keyword.AdminWorkload) && IsTsqlGroupAt(_index + 1))
        {
            return ParseTsqlWorkloadGroup();
        }

        return NextEquals(Keyword.AdminEvent) && IsKeywordAt(_index + 1, Keyword.AdminSession) ? ParseTsqlEventSession() : null;
    }

    private bool IsTsqlGroupAt(int index) => index < _tokens.Count && _tokens[index].Kind == SyntaxKind.GroupKeyword;

    // ALTER SERVER CONFIGURATION SET setting ...
    private TsqlAlterServerConfigurationStatement ParseTsqlAlterServerConfiguration()
    {
        var alterKeyword = Advance();
        var serverKeyword = Advance();
        var configurationKeyword = Advance();
        var setKeyword = Expect(SyntaxKind.SetKeyword);
        var settingLength = TsqlServerSettingLength();
        var setting = new List<SyntaxToken>(settingLength);
        for (var i = 0; i < settingLength; i++)
        {
            setting.Add(Expect(SyntaxKind.Identifier));
        }

        var options = ParseTsqlBareOptionList(allowConnector: true);
        return new TsqlAlterServerConfigurationStatement
        {
            Span = SourceSpan.From(alterKeyword, options.Span),
            AlterKeyword = alterKeyword,
            ServerKeyword = serverKeyword,
            ConfigurationKeyword = configurationKeyword,
            SetKeyword = setKeyword,
            Setting = setting,
            Options = options,
        };
    }

    // PROCESS AFFINITY, DIAGNOSTICS LOG, EXTERNAL AUTHENTICATION, FAILOVER CLUSTER PROPERTY,
    // HADR CLUSTER CONTEXT, BUFFER POOL EXTENSION; every other setting is one word (SOFTNUMA, ...).
    private int TsqlServerSettingLength()
    {
        if (IdentifierEquals(Keyword.AdminProcess) || IdentifierEquals(Keyword.Diagnostics) || IdentifierEquals(Keyword.External))
        {
            return SettingTwoWords;
        }

        return IdentifierEquals(Keyword.AdminFailover) || IdentifierEquals(Keyword.AdminHadr) || IdentifierEquals(Keyword.AdminBuffer)
            ? SettingThreeWords
            : 1;
    }

    // CREATE AVAILABILITY GROUP name [WITH (option, ...)] FOR [DATABASE db, ...]
    //     {REPLICA ON | AVAILABILITY GROUP ON} replica, ... [[,] LISTENER 'name' (option, ...)]
    private TsqlCreateAvailabilityGroupStatement ParseTsqlCreateAvailabilityGroup()
    {
        var createKeyword = Advance();
        var availabilityKeyword = Advance();
        var groupKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        SyntaxToken? withKeyword = null;
        TsqlOptionList? options = null;
        if (_current.Kind == SyntaxKind.WithKeyword)
        {
            withKeyword = Advance();
            options = ParseTsqlOptionList();
        }

        var forKeyword = _current.Kind == SyntaxKind.ForKeyword ? Advance() : (SyntaxToken?)null;
        var databaseKeyword = IdentifierEquals(Keyword.AdminDatabase) ? Advance() : (SyntaxToken?)null;
        IReadOnlyList<SyntaxToken> databases = databaseKeyword is null ? [] : ParseTsqlIdentifierList();
        var replicaKeywords = ParseTsqlReplicaKeywords();
        var replicas = new List<TsqlAvailabilityReplica> { ParseTsqlAvailabilityReplica() };
        SyntaxToken? listenerKeyword = null;
        TsqlAvailabilityReplica? listener = null;
        while (_current.Kind == SyntaxKind.Comma || IdentifierEquals(Keyword.AdminListener))
        {
            if (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
            }

            if (IdentifierEquals(Keyword.AdminListener))
            {
                listenerKeyword = Advance();
                listener = ParseTsqlAvailabilityReplica();
                break;
            }

            replicas.Add(ParseTsqlAvailabilityReplica());
        }

        return new TsqlCreateAvailabilityGroupStatement
        {
            Span = SourceSpan.From(createKeyword, listener?.Span ?? replicas[^1].Span),
            CreateKeyword = createKeyword,
            AvailabilityKeyword = availabilityKeyword,
            GroupKeyword = groupKeyword,
            Name = name,
            WithKeyword = withKeyword,
            Options = options,
            ForKeyword = forKeyword,
            DatabaseKeyword = databaseKeyword,
            Databases = databases,
            ReplicaKeywords = replicaKeywords,
            Replicas = replicas,
            ListenerKeyword = listenerKeyword,
            Listener = listener,
        };
    }

    private List<SyntaxToken> ParseTsqlIdentifierList()
    {
        var names = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            names.Add(Expect(SyntaxKind.Identifier));
        }

        return names;
    }

    // REPLICA ON | AVAILABILITY GROUP ON (a distributed availability group)
    private List<SyntaxToken> ParseTsqlReplicaKeywords()
    {
        var keywords = new List<SyntaxToken>();
        if (IdentifierEquals(Keyword.AdminAvailability))
        {
            keywords.Add(Advance());
            keywords.Add(Expect(SyntaxKind.GroupKeyword));
        }
        else
        {
            keywords.Add(ExpectIdent(Keyword.AdminReplica));
        }

        keywords.Add(Expect(SyntaxKind.OnKeyword));
        return keywords;
    }

    // 'server' [WITH (option, ...)] | 'listener' (option, ...) | database
    private TsqlAvailabilityReplica ParseTsqlAvailabilityReplica()
    {
        var name = _current.Kind is SyntaxKind.String or SyntaxKind.Identifier
            ? Advance()
            : throw new SqlParseException($"Expected replica name, found {_current.Kind}", _current.Position);
        var withKeyword = _current.Kind == SyntaxKind.WithKeyword && NextKind == SyntaxKind.OpenParen ? Advance() : (SyntaxToken?)null;
        var options = _current.Kind == SyntaxKind.OpenParen ? ParseTsqlOptionList() : null;
        return new TsqlAvailabilityReplica
        {
            Span = options is null ? name.Span : SourceSpan.From(name, options.Span),
            Name = name,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // ALTER AVAILABILITY GROUP name {SET (option, ...) | ADD DATABASE db | {ADD | MODIFY} REPLICA ON replica, ...
    //     | REMOVE REPLICA ON 'server' | JOIN [AVAILABILITY GROUP ON group, ...] | FAILOVER | OFFLINE
    //     | {GRANT | DENY} CREATE ANY DATABASE | {ADD | MODIFY | RESTART | REMOVE} LISTENER 'name' [(...)] | ...}
    private TsqlAlterAvailabilityGroupStatement ParseTsqlAlterAvailabilityGroup()
    {
        var alterKeyword = Advance();
        var availabilityKeyword = Advance();
        var groupKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var action = ParseTsqlAvailabilityAction();
        var options = _current.Kind == SyntaxKind.OpenParen ? ParseTsqlOptionList() : null;
        var targets = new List<TsqlAvailabilityReplica>();
        if (options is null && TakesTsqlAvailabilityTargets(action))
        {
            targets.Add(ParseTsqlAvailabilityReplica());
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                targets.Add(ParseTsqlAvailabilityReplica());
            }
        }

        var end = options?.Span ?? (targets.Count > 0 ? targets[^1].Span : action[^1].Span);
        return new TsqlAlterAvailabilityGroupStatement
        {
            Span = SourceSpan.From(alterKeyword, end),
            AlterKeyword = alterKeyword,
            AvailabilityKeyword = availabilityKeyword,
            GroupKeyword = groupKeyword,
            Name = name,
            Action = action,
            Options = options,
            Targets = targets,
        };
    }

    // The action verb and the object words after it (DATABASE, REPLICA, LISTENER, AVAILABILITY GROUP, ON,
    // CREATE ANY DATABASE).
    private List<SyntaxToken> ParseTsqlAvailabilityAction()
    {
        var action = new List<SyntaxToken>
        {
            _current.Kind == SyntaxKind.Identifier || IsTsqlKeywordKind(_current.Kind)
                ? Advance()
                : throw new SqlParseException($"Expected availability group action, found {_current.Kind}", _current.Position),
        };
        while (IsTsqlAvailabilityActionWord())
        {
            action.Add(Advance());
        }

        return action;
    }

    private bool IsTsqlAvailabilityActionWord() =>
        _current.Kind is SyntaxKind.GroupKeyword or SyntaxKind.OnKeyword or SyntaxKind.AnyKeyword
        || IdentifierEquals(Keyword.AdminDatabase)
        || IdentifierEquals(Keyword.AdminReplica)
        || IdentifierEquals(Keyword.AdminListener)
        || IdentifierEquals(Keyword.AdminAvailability)
        || IdentifierEquals(Keyword.Create);

    // ADD/REMOVE DATABASE, ... REPLICA ON, ... AVAILABILITY GROUP ON and ... LISTENER name what they act on;
    // GRANT/DENY CREATE ANY DATABASE does not.
    private bool TakesTsqlAvailabilityTargets(List<SyntaxToken> action)
    {
        var first = action[0];
        if (TokenEquals(first, Keyword.Grant) || TokenEquals(first, Keyword.AdminDeny))
        {
            return false;
        }

        var last = action[^1];
        return last.Kind == SyntaxKind.OnKeyword || TokenEquals(last, Keyword.AdminDatabase) || TokenEquals(last, Keyword.AdminListener);
    }

    // {CREATE | ALTER} ENDPOINT name [AUTHORIZATION login] [STATE = state]
    //     [AS {TCP | HTTP} (option, ...)] [FOR {TSQL | SERVICE_BROKER | DATABASE_MIRRORING} (option, ...)]
    private TsqlEndpointStatement ParseTsqlEndpoint()
    {
        var verb = Advance();
        var endpointKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var (authorizationKeyword, owner) = ParseTsqlAuthorization();
        var state = IdentifierEquals(Keyword.State) ? ParseTsqlOption(bounded: false, allowConnector: false) : null;
        SyntaxToken? asKeyword = null;
        SyntaxToken? protocol = null;
        TsqlOptionList? protocolOptions = null;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            protocol = Expect(SyntaxKind.Identifier);
            protocolOptions = ParseTsqlOptionList();
        }

        SyntaxToken? forKeyword = null;
        SyntaxToken? payload = null;
        TsqlOptionList? payloadOptions = null;
        if (_current.Kind == SyntaxKind.ForKeyword)
        {
            forKeyword = Advance();
            payload = Expect(SyntaxKind.Identifier);
            payloadOptions = ParseTsqlOptionList();
        }

        var end = payloadOptions?.Span ?? protocolOptions?.Span ?? state?.Span ?? owner?.Span ?? name.Span;
        return new TsqlEndpointStatement
        {
            Span = SourceSpan.From(verb, end),
            CreateOrAlterKeyword = verb,
            EndpointKeyword = endpointKeyword,
            Name = name,
            AuthorizationKeyword = authorizationKeyword,
            Owner = owner,
            State = state,
            AsKeyword = asKeyword,
            Protocol = protocol,
            ProtocolOptions = protocolOptions,
            ForKeyword = forKeyword,
            Payload = payload,
            PayloadOptions = payloadOptions,
        };
    }

    // {CREATE | ALTER} [EXTERNAL] RESOURCE POOL name [WITH (option, ...)]
    private TsqlResourcePoolStatement ParseTsqlResourcePool()
    {
        var verb = Advance();
        var externalKeyword = IdentifierEquals(Keyword.External) ? Advance() : (SyntaxToken?)null;
        var resourceKeyword = Advance();
        var poolKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var (withKeyword, options) = ParseTsqlParenthesizedWith();
        return new TsqlResourcePoolStatement
        {
            Span = SourceSpan.From(verb, options?.Span ?? name.Span),
            CreateOrAlterKeyword = verb,
            ExternalKeyword = externalKeyword,
            ResourceKeyword = resourceKeyword,
            PoolKeyword = poolKeyword,
            Name = name,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // [WITH (option, ...)]
    internal (SyntaxToken? WithKeyword, TsqlOptionList? Options) ParseTsqlParenthesizedWith()
    {
        if (_current.Kind != SyntaxKind.WithKeyword || NextKind != SyntaxKind.OpenParen)
        {
            return (null, null);
        }

        var withKeyword = Advance();
        return (withKeyword, ParseTsqlOptionList());
    }

    // {CREATE | ALTER} WORKLOAD GROUP name [WITH (option, ...)] [USING pool [, EXTERNAL external_pool]]
    private TsqlWorkloadGroupStatement ParseTsqlWorkloadGroup()
    {
        var verb = Advance();
        var workloadKeyword = Advance();
        var groupKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var (withKeyword, options) = ParseTsqlParenthesizedWith();
        SyntaxToken? usingKeyword = null;
        SyntaxToken? pool = null;
        SyntaxToken? externalKeyword = null;
        SyntaxToken? externalPool = null;
        if (IdentifierEquals(Keyword.Using))
        {
            usingKeyword = Advance();
            pool = IdentifierEquals(Keyword.External) ? null : Expect(SyntaxKind.Identifier);
            if (pool is not null && _current.Kind == SyntaxKind.Comma && NextEquals(Keyword.External))
            {
                Advance();
            }

            if (IdentifierEquals(Keyword.External))
            {
                externalKeyword = Advance();
                externalPool = Expect(SyntaxKind.Identifier);
            }
        }

        var end = externalPool?.Span ?? pool?.Span ?? options?.Span ?? name.Span;
        return new TsqlWorkloadGroupStatement
        {
            Span = SourceSpan.From(verb, end),
            CreateOrAlterKeyword = verb,
            WorkloadKeyword = workloadKeyword,
            GroupKeyword = groupKeyword,
            Name = name,
            WithKeyword = withKeyword,
            Options = options,
            UsingKeyword = usingKeyword,
            Pool = pool,
            ExternalKeyword = externalKeyword,
            ExternalPool = externalPool,
        };
    }

    // ALTER RESOURCE GOVERNOR {DISABLE | RECONFIGURE | RESET STATISTICS | WITH (option, ...)}
    private TsqlAlterResourceGovernorStatement ParseTsqlAlterResourceGovernor()
    {
        var alterKeyword = Advance();
        var resourceKeyword = Advance();
        var governorKeyword = Advance();
        var action = new List<SyntaxToken>();
        if (_current.Kind != SyntaxKind.WithKeyword)
        {
            var verb = Expect(SyntaxKind.Identifier);
            action.Add(verb);
            if (TokenEquals(verb, Keyword.AdminReset))
            {
                action.Add(ExpectIdent(Keyword.AdminStatistics));
            }
        }

        var (withKeyword, options) = ParseTsqlParenthesizedWith();
        if (action.Count == 0 && options is null)
        {
            throw new SqlParseException($"Expected Resource Governor action, found {_current.Kind}", _current.Position);
        }

        return new TsqlAlterResourceGovernorStatement
        {
            Span = SourceSpan.From(alterKeyword, options?.Span ?? action[^1].Span),
            AlterKeyword = alterKeyword,
            ResourceKeyword = resourceKeyword,
            GovernorKeyword = governorKeyword,
            Action = action,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // {CREATE | ALTER} EVENT SESSION name ON {SERVER | DATABASE} [{ADD | DROP} {EVENT | TARGET} ..., ...]
    //     [WITH (option, ...)] | ALTER EVENT SESSION name ON SERVER STATE = {START | STOP}
    private TsqlEventSessionStatement ParseTsqlEventSession()
    {
        var verb = Advance();
        var eventKeyword = Advance();
        var sessionKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var scope = Expect(SyntaxKind.Identifier);
        var items = new List<TsqlEventSessionItem>();
        while (IsTsqlEventSessionItemStart())
        {
            items.Add(ParseTsqlEventSessionItem());
            if (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
            }
        }

        var (withKeyword, options) = ParseTsqlParenthesizedWith();
        var state = IdentifierEquals(Keyword.State) && NextKind == SyntaxKind.EqualsToken
            ? ParseTsqlOption(bounded: false, allowConnector: false)
            : null;
        var end = state?.Span ?? options?.Span ?? (items.Count > 0 ? items[^1].Span : scope.Span);
        return new TsqlEventSessionStatement
        {
            Span = SourceSpan.From(verb, end),
            CreateOrAlterKeyword = verb,
            EventKeyword = eventKeyword,
            SessionKeyword = sessionKeyword,
            Name = name,
            OnKeyword = onKeyword,
            Scope = scope,
            Items = items,
            WithKeyword = withKeyword,
            Options = options,
            State = state,
        };
    }

    private bool IsTsqlEventSessionItemStart() =>
        (IdentifierEquals(Keyword.Add) || IdentifierEquals(Keyword.Drop))
        && (NextEquals(Keyword.AdminEvent) || NextEquals(Keyword.Target));

    // {ADD | DROP} {EVENT | TARGET} name [( [SET field = value, ...] [ACTION (action, ...)] [WHERE predicate] )]
    private TsqlEventSessionItem ParseTsqlEventSessionItem()
    {
        var action = Advance();
        var kind = Advance();
        var name = ParseQualifiedName();
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new TsqlEventSessionItem { Span = SourceSpan.From(action, name[^1]), Action = action, Kind = kind, Name = name };
        }

        var openParen = Advance();
        var setKeyword = _current.Kind == SyntaxKind.SetKeyword ? Advance() : (SyntaxToken?)null;
        var settings = setKeyword is null ? null : ParseTsqlBareOptionList(allowConnector: false);
        var actions = ParseTsqlEventActions();
        var whereKeyword = _current.Kind == SyntaxKind.WhereKeyword ? Advance() : (SyntaxToken?)null;
        var predicate = whereKeyword is null ? null : ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlEventSessionItem
        {
            Span = SourceSpan.From(action, closeParen),
            Action = action,
            Kind = kind,
            Name = name,
            OpenParen = openParen,
            SetKeyword = setKeyword,
            Settings = settings,
            ActionKeyword = actions.ActionKeyword,
            ActionOpenParen = actions.OpenParen,
            Actions = actions.Actions,
            ActionCloseParen = actions.CloseParen,
            WhereKeyword = whereKeyword,
            Predicate = predicate,
            CloseParen = closeParen,
        };
    }

    internal readonly record struct TsqlEventActions(
        SyntaxToken? ActionKeyword,
        SyntaxToken? OpenParen,
        IReadOnlyList<Expression> Actions,
        SyntaxToken? CloseParen);

    // [ACTION (package.action, ...)]
    private TsqlEventActions ParseTsqlEventActions()
    {
        if (!IdentifierEquals(Keyword.Action))
        {
            return new TsqlEventActions(null, null, [], null);
        }

        var actionKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var actions = ParseExpressionList();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlEventActions(actionKeyword, openParen, actions, closeParen);
    }
}
