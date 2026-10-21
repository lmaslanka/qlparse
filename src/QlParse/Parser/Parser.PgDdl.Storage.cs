namespace QlParse;

internal sealed partial class Parser
{
    // `CREATE TABLESPACE name [OWNER role] LOCATION 'directory' [WITH (option = value, ...)]`.
    internal CreateTablespaceStatement ParseCreateTablespace()
    {
        var createKeyword = Advance();
        var tablespaceKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var ownerKeyword = IdentifierEquals(Keyword.DdlOwner) ? Advance() : (SyntaxToken?)null;
        var owner = ownerKeyword is null ? (SyntaxToken?)null : ExpectRoleName();
        var locationKeyword = ExpectIdent(Keyword.DdlLocation);
        var location = Expect(SyntaxKind.String);
        var (withKeyword, options) = TryParsePgWithParameters();
        return new CreateTablespaceStatement
        {
            Span = SourceSpan.From(createKeyword, options?.Span ?? location.Span),
            CreateKeyword = createKeyword,
            TablespaceKeyword = tablespaceKeyword,
            Name = name,
            OwnerKeyword = ownerKeyword,
            Owner = owner,
            LocationKeyword = locationKeyword,
            Location = location,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    private SyntaxToken ExpectRoleName()
    {
        if (_current.Kind is not (SyntaxKind.Identifier or SyntaxKind.CurrentUserKeyword or SyntaxKind.SessionUserKeyword or SyntaxKind.CurrentRoleKeyword))
        {
            throw new SqlParseException($"Expected role name, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    // `ALTER TABLESPACE name {RENAME TO new | OWNER TO role | SET (...) | RESET (...)}`.
    internal AlterTablespaceStatement ParseAlterTablespace()
    {
        var alterKeyword = Advance();
        var tablespaceKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var action = TryParsePgDdlAlterTableAction()
            ?? throw new SqlParseException($"Expected ALTER TABLESPACE action, found {_current.Kind}", _current.Position);
        return new AlterTablespaceStatement
        {
            Span = SourceSpan.From(alterKeyword, action.Span),
            AlterKeyword = alterKeyword,
            TablespaceKeyword = tablespaceKeyword,
            Name = name,
            Action = action,
        };
    }

    // `DROP TABLESPACE [IF EXISTS] name`.
    internal DropTablespaceStatement ParseDropTablespace()
    {
        var dropKeyword = Advance();
        var tablespaceKeyword = Advance();
        var ifExists = TryParsePgExistenceCheck();
        var name = Expect(SyntaxKind.Identifier);
        return new DropTablespaceStatement
        {
            Span = SourceSpan.From(dropKeyword, name),
            DropKeyword = dropKeyword,
            TablespaceKeyword = tablespaceKeyword,
            IfExists = ifExists,
            Name = name,
        };
    }

    // `REINDEX [(option [value], ...)] {INDEX | TABLE | SCHEMA | DATABASE | SYSTEM} [CONCURRENTLY] [name]`.
    internal ReindexStatement ParseReindex()
    {
        var reindexKeyword = Advance();
        var options = _current.Kind == SyntaxKind.OpenParen ? ParseStorageParameterList() : null;
        var kind = Expect(SyntaxKind.Identifier);
        var concurrently = IdentifierEquals(Keyword.Concurrently) ? Advance() : (SyntaxToken?)null;
        IReadOnlyList<SyntaxToken> name = _current.Kind == SyntaxKind.Identifier ? ParseQualifiedName() : [];
        var end = name.Count > 0 ? name[^1] : concurrently ?? kind;
        return new ReindexStatement
        {
            Span = SourceSpan.From(reindexKeyword, end),
            ReindexKeyword = reindexKeyword,
            Options = options,
            Kind = kind,
            ConcurrentlyKeyword = concurrently,
            Name = name,
        };
    }

    // `ALTER SEQUENCE [IF EXISTS] name [AS type] [options ...]`, or one of the relation actions
    // (`RENAME TO`, `SET SCHEMA`, `OWNER TO`, `SET {LOGGED | UNLOGGED}`).
    internal AlterSequenceStatement ParseAlterSequence()
    {
        var alterKeyword = Advance();
        var sequenceKeyword = Advance();
        var ifExists = TryParsePgExistenceCheck();
        var name = ParseQualifiedName();
        var action = TryParsePgDdlAlterTableAction();
        SyntaxToken? asKeyword = null;
        DataType? dataType = null;
        var options = new List<SequenceOption>();
        if (action is null)
        {
            if (_current.Kind == SyntaxKind.AsKeyword)
            {
                asKeyword = Advance();
                dataType = ParseDataType();
            }

            while (IsSequenceOption() || IsPgSequenceOption())
            {
                options.Add(ParseSequenceOption());
            }
        }

        var end = action?.Span ?? (options.Count > 0 ? options[^1].Span : dataType?.Span ?? name[^1].Span);
        return new AlterSequenceStatement
        {
            Span = SourceSpan.From(alterKeyword, end),
            AlterKeyword = alterKeyword,
            SequenceKeyword = sequenceKeyword,
            IfExists = ifExists,
            Name = name,
            AsKeyword = asKeyword,
            DataType = dataType,
            Options = options,
            Action = action,
        };
    }
}
