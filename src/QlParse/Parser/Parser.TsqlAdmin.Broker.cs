namespace QlParse;

// Workstream G: Service Broker objects (message types, contracts, queues, services, routes) and conversation
// statements. ParseTsqlReceive and ParseTsqlGetConversationGroup are also the bodies of WAITFOR (...).
internal sealed partial class Parser
{
    private Query? TryParseTsqlBrokerCreate()
    {
        if (NextEquals(Keyword.AdminMessage) && IsKeywordAt(_index + 1, Keyword.Type))
        {
            return ParseTsqlMessageType();
        }

        if (NextEquals(Keyword.AdminContract))
        {
            return ParseTsqlCreateContract();
        }

        if (NextEquals(Keyword.AdminQueue))
        {
            return ParseTsqlCreateQueue();
        }

        if (NextEquals(Keyword.AdminService))
        {
            return ParseTsqlService();
        }

        return NextEquals(Keyword.AdminRoute) ? ParseTsqlRoute() : null;
    }

    private Query? TryParseTsqlBrokerAlter()
    {
        if (NextEquals(Keyword.AdminMessage) && IsKeywordAt(_index + 1, Keyword.Type))
        {
            return ParseTsqlMessageType();
        }

        if (NextEquals(Keyword.AdminQueue))
        {
            return ParseTsqlAlterQueue();
        }

        // ALTER SERVICE MASTER KEY is a security statement.
        if (NextEquals(Keyword.AdminService) && !(IsKeywordAt(_index + 1, Keyword.AdminMaster) && IsKeywordAt(_index + 2, Keyword.Key)))
        {
            return ParseTsqlService();
        }

        return NextEquals(Keyword.AdminRoute) ? ParseTsqlRoute() : null;
    }

    // BEGIN DIALOG ... and BEGIN CONVERSATION TIMER ...; every other BEGIN belongs to the procedural hook.
    internal bool IsTsqlBrokerBegin() =>
        NextEquals(Keyword.AdminDialog) || (NextEquals(Keyword.AdminConversation) && IsKeywordAt(_index + 1, Keyword.AdminTimer));

    private Query? TryParseTsqlBeginBroker()
    {
        if (NextEquals(Keyword.AdminDialog))
        {
            return ParseTsqlBeginDialog();
        }

        return IsTsqlBrokerBegin() ? ParseTsqlBeginConversationTimer() : null;
    }

    // {CREATE | ALTER} MESSAGE TYPE name [AUTHORIZATION owner]
    //     [VALIDATION = {NONE | EMPTY | WELL_FORMED_XML | VALID_XML WITH SCHEMA COLLECTION name}]
    private TsqlMessageTypeStatement ParseTsqlMessageType()
    {
        var verb = Advance();
        var messageKeyword = Advance();
        var typeKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var (authorizationKeyword, owner) = ParseTsqlAuthorization();
        if (!IdentifierEquals(Keyword.AdminValidation))
        {
            return new TsqlMessageTypeStatement
            {
                Span = SourceSpan.From(verb, owner ?? name),
                CreateOrAlterKeyword = verb,
                MessageKeyword = messageKeyword,
                TypeKeyword = typeKeyword,
                Name = name,
                AuthorizationKeyword = authorizationKeyword,
                Owner = owner,
            };
        }

        var validationKeyword = Advance();
        var equalsToken = Expect(SyntaxKind.EqualsToken);
        var validation = Expect(SyntaxKind.Identifier);
        SyntaxToken? withKeyword = null;
        SyntaxToken? schemaKeyword = null;
        SyntaxToken? collectionKeyword = null;
        IReadOnlyList<SyntaxToken> schemaCollection = [];
        if (TokenEquals(validation, Keyword.AdminValidXml))
        {
            withKeyword = Expect(SyntaxKind.WithKeyword);
            schemaKeyword = ExpectIdent(Keyword.Schema);
            collectionKeyword = ExpectIdent(Keyword.AdminCollection);
            schemaCollection = ParseQualifiedName();
        }

        return new TsqlMessageTypeStatement
        {
            Span = SourceSpan.From(verb, schemaCollection.Count > 0 ? schemaCollection[^1] : validation),
            CreateOrAlterKeyword = verb,
            MessageKeyword = messageKeyword,
            TypeKeyword = typeKeyword,
            Name = name,
            AuthorizationKeyword = authorizationKeyword,
            Owner = owner,
            ValidationKeyword = validationKeyword,
            EqualsToken = equalsToken,
            Validation = validation,
            WithKeyword = withKeyword,
            SchemaKeyword = schemaKeyword,
            CollectionKeyword = collectionKeyword,
            SchemaCollection = schemaCollection,
        };
    }

    // CREATE CONTRACT name [AUTHORIZATION owner] ({message_type | [DEFAULT]} SENT BY {INITIATOR | TARGET | ANY}, ...)
    private TsqlCreateContractStatement ParseTsqlCreateContract()
    {
        var createKeyword = Advance();
        var contractKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var (authorizationKeyword, owner) = ParseTsqlAuthorization();
        var openParen = Expect(SyntaxKind.OpenParen);
        var messages = new List<TsqlContractMessage> { ParseTsqlContractMessage() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            messages.Add(ParseTsqlContractMessage());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlCreateContractStatement
        {
            Span = SourceSpan.From(createKeyword, closeParen),
            CreateKeyword = createKeyword,
            ContractKeyword = contractKeyword,
            Name = name,
            AuthorizationKeyword = authorizationKeyword,
            Owner = owner,
            OpenParen = openParen,
            Messages = messages,
            CloseParen = closeParen,
        };
    }

    private TsqlContractMessage ParseTsqlContractMessage()
    {
        var messageType = Expect(SyntaxKind.Identifier);
        var sentKeyword = ExpectIdent(Keyword.AdminSent);
        var byKeyword = Expect(SyntaxKind.ByKeyword);
        var sender = _current.Kind is SyntaxKind.Identifier or SyntaxKind.AnyKeyword
            ? Advance()
            : throw new SqlParseException($"Expected INITIATOR, TARGET or ANY, found {_current.Kind}", _current.Position);
        return new TsqlContractMessage
        {
            Span = SourceSpan.From(messageType, sender),
            MessageType = messageType,
            SentKeyword = sentKeyword,
            ByKeyword = byKeyword,
            Sender = sender,
        };
    }

    // CREATE QUEUE name [WITH option, ...] [ON {filegroup | [DEFAULT]}]
    private TsqlCreateQueueStatement ParseTsqlCreateQueue()
    {
        var createKeyword = Advance();
        var queueKeyword = Advance();
        var name = ParseQualifiedName();
        var options = ParseTsqlOptionalWith();
        SyntaxToken? onKeyword = null;
        SyntaxToken? fileGroup = null;
        if (_current.Kind == SyntaxKind.OnKeyword)
        {
            onKeyword = Advance();
            fileGroup = Expect(SyntaxKind.Identifier);
        }

        return new TsqlCreateQueueStatement
        {
            Span = SourceSpan.From(createKeyword, fileGroup?.Span ?? options.Options?.Span ?? name[^1].Span),
            CreateKeyword = createKeyword,
            QueueKeyword = queueKeyword,
            Name = name,
            WithKeyword = options.WithKeyword,
            Options = options.Options,
            OnKeyword = onKeyword,
            FileGroup = fileGroup,
        };
    }

    // ALTER QUEUE name {WITH option, ... | REBUILD [WITH (option, ...)] | REORGANIZE [WITH (option, ...)]
    //     | MOVE TO {filegroup | [DEFAULT]}}
    private TsqlAlterQueueStatement ParseTsqlAlterQueue()
    {
        var alterKeyword = Advance();
        var queueKeyword = Advance();
        var name = ParseQualifiedName();
        if (_current.Kind == SyntaxKind.WithKeyword)
        {
            var withClause = ParseTsqlOptionalWith();
            return new TsqlAlterQueueStatement
            {
                Span = SourceSpan.From(alterKeyword, withClause.Options!.Span),
                AlterKeyword = alterKeyword,
                QueueKeyword = queueKeyword,
                Name = name,
                WithKeyword = withClause.WithKeyword,
                Options = withClause.Options,
            };
        }

        var action = new List<SyntaxToken>();
        SyntaxToken? fileGroup = null;
        if (IdentifierEquals(Keyword.AdminMove))
        {
            action.Add(Advance());
            action.Add(ExpectIdent(Keyword.To));
            fileGroup = Expect(SyntaxKind.Identifier);
        }
        else
        {
            action.Add(IdentifierEquals(Keyword.Rebuild) || IdentifierEquals(Keyword.Reorganize)
                ? Advance()
                : throw new SqlParseException($"Expected queue action, found {_current.Kind}", _current.Position));
        }

        var (withKeyword, options) = ParseTsqlParenthesizedWith();
        return new TsqlAlterQueueStatement
        {
            Span = SourceSpan.From(alterKeyword, options?.Span ?? fileGroup?.Span ?? action[^1].Span),
            AlterKeyword = alterKeyword,
            QueueKeyword = queueKeyword,
            Name = name,
            Action = action,
            FileGroup = fileGroup,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // CREATE SERVICE name [AUTHORIZATION owner] ON QUEUE queue [(contract, ...)]
    // ALTER SERVICE name [ON QUEUE queue] [({ADD | DROP} CONTRACT contract, ...)]
    private TsqlServiceStatement ParseTsqlService()
    {
        var verb = Advance();
        var serviceKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var (authorizationKeyword, owner) = ParseTsqlAuthorization();
        SyntaxToken? onKeyword = null;
        SyntaxToken? queueKeyword = null;
        IReadOnlyList<SyntaxToken> queue = [];
        if (_current.Kind == SyntaxKind.OnKeyword)
        {
            onKeyword = Advance();
            queueKeyword = ExpectIdent(Keyword.AdminQueue);
            queue = ParseQualifiedName();
        }

        SyntaxToken? openParen = null;
        SyntaxToken? closeParen = null;
        var contracts = new List<TsqlServiceContract>();
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            openParen = Advance();
            contracts.Add(ParseTsqlServiceContract());
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                contracts.Add(ParseTsqlServiceContract());
            }

            closeParen = Expect(SyntaxKind.CloseParen);
        }

        var end = closeParen?.Span ?? (queue.Count > 0 ? queue[^1].Span : owner?.Span ?? name.Span);
        return new TsqlServiceStatement
        {
            Span = SourceSpan.From(verb, end),
            CreateOrAlterKeyword = verb,
            ServiceKeyword = serviceKeyword,
            Name = name,
            AuthorizationKeyword = authorizationKeyword,
            Owner = owner,
            OnKeyword = onKeyword,
            QueueKeyword = queueKeyword,
            Queue = queue,
            OpenParen = openParen,
            Contracts = contracts,
            CloseParen = closeParen,
        };
    }

    // contract | [DEFAULT] | {ADD | DROP} CONTRACT contract
    private TsqlServiceContract ParseTsqlServiceContract()
    {
        if (!(IdentifierEquals(Keyword.Add) || IdentifierEquals(Keyword.Drop)) || !NextEquals(Keyword.AdminContract))
        {
            var contract = Expect(SyntaxKind.Identifier);
            return new TsqlServiceContract { Span = contract.Span, Name = contract };
        }

        var action = Advance();
        var contractKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        return new TsqlServiceContract
        {
            Span = SourceSpan.From(action, name),
            Action = action,
            ContractKeyword = contractKeyword,
            Name = name,
        };
    }

    // {CREATE | ALTER} ROUTE name [AUTHORIZATION owner] WITH option, ...
    private TsqlRouteStatement ParseTsqlRoute()
    {
        var verb = Advance();
        var routeKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var (authorizationKeyword, owner) = ParseTsqlAuthorization();
        var withKeyword = Expect(SyntaxKind.WithKeyword);
        var options = ParseTsqlBareOptionList(allowConnector: false);
        return new TsqlRouteStatement
        {
            Span = SourceSpan.From(verb, options.Span),
            CreateOrAlterKeyword = verb,
            RouteKeyword = routeKeyword,
            Name = name,
            AuthorizationKeyword = authorizationKeyword,
            Owner = owner,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // BEGIN DIALOG [CONVERSATION] @handle FROM SERVICE initiator TO SERVICE 'target' [, 'instance']
    //     [ON CONTRACT contract] [WITH option, ...]
    private TsqlBeginDialogStatement ParseTsqlBeginDialog()
    {
        var beginKeyword = Advance();
        var dialogKeyword = Advance();
        var conversationKeyword = IdentifierEquals(Keyword.AdminConversation) ? Advance() : (SyntaxToken?)null;
        var handle = ExpectTsqlVariable();
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var fromServiceKeyword = ExpectIdent(Keyword.AdminService);
        var initiatorService = Expect(SyntaxKind.Identifier);
        var toKeyword = ExpectIdent(Keyword.To);
        var toServiceKeyword = ExpectIdent(Keyword.AdminService);
        var targetService = _current.Kind is SyntaxKind.String or SyntaxKind.EmbeddedHost
            ? Advance()
            : throw new SqlParseException($"Expected target service name, found {_current.Kind}", _current.Position);
        SyntaxToken? brokerInstance = null;
        if (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            brokerInstance = _current.Kind is SyntaxKind.String or SyntaxKind.EmbeddedHost
                ? Advance()
                : throw new SqlParseException($"Expected broker instance, found {_current.Kind}", _current.Position);
        }

        SyntaxToken? onKeyword = null;
        SyntaxToken? contractKeyword = null;
        SyntaxToken? contract = null;
        if (_current.Kind == SyntaxKind.OnKeyword)
        {
            onKeyword = Advance();
            contractKeyword = ExpectIdent(Keyword.AdminContract);
            contract = Expect(SyntaxKind.Identifier);
        }

        var options = ParseTsqlOptionalWith();
        var end = options.Options?.Span ?? contract?.Span ?? brokerInstance?.Span ?? targetService.Span;
        return new TsqlBeginDialogStatement
        {
            Span = SourceSpan.From(beginKeyword, end),
            BeginKeyword = beginKeyword,
            DialogKeyword = dialogKeyword,
            ConversationKeyword = conversationKeyword,
            Handle = handle,
            FromKeyword = fromKeyword,
            FromServiceKeyword = fromServiceKeyword,
            InitiatorService = initiatorService,
            ToKeyword = toKeyword,
            ToServiceKeyword = toServiceKeyword,
            TargetService = targetService,
            BrokerInstance = brokerInstance,
            OnKeyword = onKeyword,
            ContractKeyword = contractKeyword,
            Contract = contract,
            WithKeyword = options.WithKeyword,
            Options = options.Options,
        };
    }

    // BEGIN CONVERSATION TIMER (@handle) TIMEOUT = seconds
    private TsqlBeginConversationTimerStatement ParseTsqlBeginConversationTimer()
    {
        var beginKeyword = Advance();
        var conversationKeyword = Advance();
        var timerKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var handle = ExpectTsqlVariable();
        var closeParen = Expect(SyntaxKind.CloseParen);
        var timeoutKeyword = ExpectIdent(Keyword.AdminTimeout);
        var equalsToken = Expect(SyntaxKind.EqualsToken);
        var timeout = _current.Kind is SyntaxKind.Number or SyntaxKind.EmbeddedHost
            ? Advance()
            : throw new SqlParseException($"Expected timeout, found {_current.Kind}", _current.Position);
        return new TsqlBeginConversationTimerStatement
        {
            Span = SourceSpan.From(beginKeyword, timeout),
            BeginKeyword = beginKeyword,
            ConversationKeyword = conversationKeyword,
            TimerKeyword = timerKeyword,
            OpenParen = openParen,
            Handle = handle,
            CloseParen = closeParen,
            TimeoutKeyword = timeoutKeyword,
            EqualsToken = equalsToken,
            Timeout = timeout,
        };
    }

    // SEND ON CONVERSATION [(]@handle, ...[)] [MESSAGE TYPE type] [(body)]
    private TsqlSendStatement ParseTsqlSend()
    {
        var sendKeyword = Advance();
        var onKeyword = Advance();
        var conversationKeyword = ExpectIdent(Keyword.AdminConversation);
        var handlesOpenParen = _current.Kind == SyntaxKind.OpenParen ? Advance() : (SyntaxToken?)null;
        var handles = new List<SyntaxToken> { ExpectTsqlVariable() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            handles.Add(ExpectTsqlVariable());
        }

        var handlesCloseParen = handlesOpenParen is null ? (SyntaxToken?)null : Expect(SyntaxKind.CloseParen);
        SyntaxToken? messageKeyword = null;
        SyntaxToken? typeKeyword = null;
        SyntaxToken? messageType = null;
        if (IdentifierEquals(Keyword.AdminMessage))
        {
            messageKeyword = Advance();
            typeKeyword = ExpectIdent(Keyword.Type);
            messageType = Expect(SyntaxKind.Identifier);
        }

        SyntaxToken? bodyOpenParen = null;
        Expression? body = null;
        SyntaxToken? bodyCloseParen = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            bodyOpenParen = Advance();
            body = ParseExpression();
            bodyCloseParen = Expect(SyntaxKind.CloseParen);
        }

        var end = bodyCloseParen?.Span ?? messageType?.Span ?? handlesCloseParen?.Span ?? handles[^1].Span;
        return new TsqlSendStatement
        {
            Span = SourceSpan.From(sendKeyword, end),
            SendKeyword = sendKeyword,
            OnKeyword = onKeyword,
            ConversationKeyword = conversationKeyword,
            HandlesOpenParen = handlesOpenParen,
            Handles = handles,
            HandlesCloseParen = handlesCloseParen,
            MessageKeyword = messageKeyword,
            TypeKeyword = typeKeyword,
            MessageType = messageType,
            BodyOpenParen = bodyOpenParen,
            Body = body,
            BodyCloseParen = bodyCloseParen,
        };
    }

    // RECEIVE [TOP (n)] column, ... FROM queue [INTO @table] [WHERE {conversation_handle = @h | conversation_group_id = @g}]
    internal TsqlReceiveStatement ParseTsqlReceive()
    {
        var receiveKeyword = Advance();
        SyntaxToken? topKeyword = null;
        SyntaxToken? topOpenParen = null;
        Expression? top = null;
        SyntaxToken? topCloseParen = null;
        if (IdentifierEquals(Keyword.AdminTop) && NextKind == SyntaxKind.OpenParen)
        {
            topKeyword = Advance();
            topOpenParen = Advance();
            top = ParseExpression();
            topCloseParen = Expect(SyntaxKind.CloseParen);
        }

        var columns = new List<TsqlReceiveColumn> { ParseTsqlReceiveColumn() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            columns.Add(ParseTsqlReceiveColumn());
        }

        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var queue = ParseQualifiedName();
        SyntaxToken? intoKeyword = null;
        SyntaxToken? into = null;
        if (IdentifierEquals(Keyword.Into))
        {
            intoKeyword = Advance();
            into = ExpectTsqlVariable();
        }

        var whereKeyword = _current.Kind == SyntaxKind.WhereKeyword ? Advance() : (SyntaxToken?)null;
        var where = whereKeyword is null ? null : ParseExpression();
        return new TsqlReceiveStatement
        {
            Span = SourceSpan.From(receiveKeyword, where?.Span ?? into?.Span ?? queue[^1].Span),
            ReceiveKeyword = receiveKeyword,
            TopKeyword = topKeyword,
            TopOpenParen = topOpenParen,
            Top = top,
            TopCloseParen = topCloseParen,
            Columns = columns,
            FromKeyword = fromKeyword,
            Queue = queue,
            IntoKeyword = intoKeyword,
            Into = into,
            WhereKeyword = whereKeyword,
            Where = where,
        };
    }

    // `*` | expression [[AS] alias] | {alias | @variable} = expression
    private TsqlReceiveColumn ParseTsqlReceiveColumn()
    {
        if ((_current.Kind is SyntaxKind.Identifier or SyntaxKind.EmbeddedHost) && NextKind == SyntaxKind.EqualsToken)
        {
            var target = Advance();
            var equalsToken = Advance();
            var value = ParseExpression();
            return new TsqlReceiveColumn
            {
                Span = SourceSpan.From(target, value.Span),
                Target = target,
                EqualsToken = equalsToken,
                Expression = value,
            };
        }

        var expression = _current.Kind == SyntaxKind.Star ? ParseStar() : ParseExpression();
        var asKeyword = _current.Kind == SyntaxKind.AsKeyword ? Advance() : (SyntaxToken?)null;
        var alias = asKeyword is not null || _current.Kind == SyntaxKind.Identifier ? Expect(SyntaxKind.Identifier) : (SyntaxToken?)null;
        return new TsqlReceiveColumn
        {
            Span = alias is SyntaxToken last ? SourceSpan.From(expression.Span, last) : expression.Span,
            Expression = expression,
            AsKeyword = asKeyword,
            Alias = alias,
        };
    }

    // END CONVERSATION @handle [WITH ERROR = code DESCRIPTION = 'text' | WITH CLEANUP]
    private TsqlEndConversationStatement ParseTsqlEndConversation()
    {
        var endKeyword = Advance();
        var conversationKeyword = Advance();
        var handle = ExpectTsqlVariable();
        if (_current.Kind != SyntaxKind.WithKeyword)
        {
            return new TsqlEndConversationStatement
            {
                Span = SourceSpan.From(endKeyword, handle),
                EndKeyword = endKeyword,
                ConversationKeyword = conversationKeyword,
                Handle = handle,
            };
        }

        var withKeyword = Advance();
        if (IdentifierEquals(Keyword.AdminCleanup))
        {
            var cleanupKeyword = Advance();
            return new TsqlEndConversationStatement
            {
                Span = SourceSpan.From(endKeyword, cleanupKeyword),
                EndKeyword = endKeyword,
                ConversationKeyword = conversationKeyword,
                Handle = handle,
                WithKeyword = withKeyword,
                CleanupKeyword = cleanupKeyword,
            };
        }

        var errorKeyword = ExpectIdent(Keyword.Error);
        var errorEquals = Expect(SyntaxKind.EqualsToken);
        var errorCode = ExpectTsqlErrorValue(SyntaxKind.Number);
        var descriptionKeyword = ExpectIdent(Keyword.AdminDescription);
        var descriptionEquals = Expect(SyntaxKind.EqualsToken);
        var description = ExpectTsqlErrorValue(SyntaxKind.String);
        return new TsqlEndConversationStatement
        {
            Span = SourceSpan.From(endKeyword, description),
            EndKeyword = endKeyword,
            ConversationKeyword = conversationKeyword,
            Handle = handle,
            WithKeyword = withKeyword,
            ErrorKeyword = errorKeyword,
            ErrorEquals = errorEquals,
            ErrorCode = errorCode,
            DescriptionKeyword = descriptionKeyword,
            DescriptionEquals = descriptionEquals,
            Description = description,
        };
    }

    // A literal of the given kind or a variable.
    private SyntaxToken ExpectTsqlErrorValue(SyntaxKind literal) =>
        _current.Kind == literal || _current.Kind == SyntaxKind.EmbeddedHost
            ? Advance()
            : throw new SqlParseException($"Expected {literal} or variable, found {_current.Kind}", _current.Position);

    // GET CONVERSATION GROUP @group FROM queue
    internal TsqlGetConversationGroupStatement ParseTsqlGetConversationGroup()
    {
        var getKeyword = Advance();
        var conversationKeyword = Advance();
        var groupKeyword = Expect(SyntaxKind.GroupKeyword);
        var group = ExpectTsqlVariable();
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var queue = ParseQualifiedName();
        return new TsqlGetConversationGroupStatement
        {
            Span = SourceSpan.From(getKeyword, queue[^1]),
            GetKeyword = getKeyword,
            ConversationKeyword = conversationKeyword,
            GroupKeyword = groupKeyword,
            Group = group,
            FromKeyword = fromKeyword,
            Queue = queue,
        };
    }

    // MOVE CONVERSATION @handle TO @group
    private TsqlMoveConversationStatement ParseTsqlMoveConversation()
    {
        var moveKeyword = Advance();
        var conversationKeyword = Advance();
        var handle = ExpectTsqlVariable();
        var toKeyword = ExpectIdent(Keyword.To);
        var group = ExpectTsqlVariable();
        return new TsqlMoveConversationStatement
        {
            Span = SourceSpan.From(moveKeyword, group),
            MoveKeyword = moveKeyword,
            ConversationKeyword = conversationKeyword,
            Handle = handle,
            ToKeyword = toKeyword,
            Group = group,
        };
    }
}
