namespace QlParse;

internal sealed partial class Parser
{
    private readonly record struct RoleRenameClause(SyntaxToken RenameKeyword, SyntaxToken ToKeyword, SyntaxToken NewName);

    private readonly record struct InDatabaseClause(SyntaxToken? InKeyword, SyntaxToken? DatabaseKeyword, SyntaxToken? Database);

    // CREATE {ROLE | USER | GROUP} name [[WITH] option ...]
    internal CreateRoleStatement ParsePgCreateRole()
    {
        var createKeyword = Advance();
        var roleKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var withKeyword = _current.Kind == SyntaxKind.WithKeyword ? Advance() : (SyntaxToken?)null;
        var options = ParseRoleOptions();
        var end = options.Count > 0 ? options[^1].Span : withKeyword?.Span ?? name.Span;
        return new CreateRoleStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            RoleKeyword = roleKeyword,
            Name = name,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // ALTER {ROLE | USER} name {[WITH] option ... | RENAME TO new | [IN DATABASE d] {SET … | RESET …}}
    internal AlterRoleStatement ParseAlterRole()
    {
        var alterKeyword = Advance();
        var roleKeyword = Advance();
        var name = _current.Kind == SyntaxKind.AllKeyword ? Advance() : ParseGrantee();
        if (IdentifierEquals(Keyword.Rename))
        {
            var rename = ParseRoleRenameClause();
            return new AlterRoleStatement
            {
                Span = SourceSpan.From(alterKeyword, rename.NewName),
                AlterKeyword = alterKeyword,
                RoleKeyword = roleKeyword,
                Name = name,
                RenameKeyword = rename.RenameKeyword,
                ToKeyword = rename.ToKeyword,
                NewName = rename.NewName,
            };
        }

        var inDatabase = ParseInDatabaseClause();
        var setting = TryParseSettingAction();
        var withKeyword = setting is null && _current.Kind == SyntaxKind.WithKeyword ? Advance() : (SyntaxToken?)null;
        var options = setting is null ? ParseRoleOptions() : [];
        if (setting is null && options.Count == 0)
        {
            throw new SqlParseException($"Expected role option, found {_current.Kind}", _current.Position);
        }

        return new AlterRoleStatement
        {
            Span = SourceSpan.From(alterKeyword, setting?.Span ?? options[^1].Span),
            AlterKeyword = alterKeyword,
            RoleKeyword = roleKeyword,
            Name = name,
            WithKeyword = withKeyword,
            Options = options,
            InKeyword = inDatabase.InKeyword,
            DatabaseKeyword = inDatabase.DatabaseKeyword,
            Database = inDatabase.Database,
            Setting = setting,
        };
    }

    private RoleRenameClause ParseRoleRenameClause()
    {
        var renameKeyword = Advance();
        var toKeyword = ExpectIdent(Keyword.To);
        return new RoleRenameClause(renameKeyword, toKeyword, Expect(SyntaxKind.Identifier));
    }

    private InDatabaseClause ParseInDatabaseClause()
    {
        if (_current.Kind != SyntaxKind.InKeyword)
        {
            return default;
        }

        var inKeyword = Advance();
        var databaseKeyword = ExpectIdent(Keyword.UtilDatabase);
        return new InDatabaseClause(inKeyword, databaseKeyword, Expect(SyntaxKind.Identifier));
    }

    // DROP {ROLE | USER | GROUP} [IF EXISTS] name [, ...]
    internal DropRoleStatement ParsePgDropRole()
    {
        var dropKeyword = Advance();
        var roleKeyword = Advance();
        var (ifKeyword, existsKeyword) = ParseIfExists();
        var names = ParseRoleList();
        return new DropRoleStatement
        {
            Span = SourceSpan.From(dropKeyword, names[^1]),
            DropKeyword = dropKeyword,
            RoleKeyword = roleKeyword,
            IfKeyword = ifKeyword,
            ExistsKeyword = existsKeyword,
            Name = names[0],
            Names = names,
        };
    }

    private IReadOnlyList<KeywordOption> ParseRoleOptions()
    {
        List<KeywordOption>? options = null;
        var option = TryParseRoleOption();
        while (option is not null)
        {
            (options ??= []).Add(option);
            option = TryParseRoleOption();
        }

        return options ?? (IReadOnlyList<KeywordOption>)[];
    }

    private KeywordOption? TryParseRoleOption()
    {
        if (_current.Kind == SyntaxKind.InKeyword)
        {
            var inKeyword = Advance();
            var roleOrGroup = IdentifierEquals(Keyword.Role) ? Advance() : Expect(SyntaxKind.GroupKeyword);
            return ParseRoleNamesOption([inKeyword, roleOrGroup]);
        }

        if (_current.Kind == SyntaxKind.UserKeyword)
        {
            return ParseRoleNamesOption([Advance()]);
        }

        if (_current.Kind != SyntaxKind.Identifier)
        {
            return null;
        }

        if (IsRoleFlagWord(_current.TextOf(_source)))
        {
            var flag = Advance();
            return new KeywordOption { Span = flag.Span, Keywords = [flag] };
        }

        return TryParseRoleValueOption() ?? TryParseRoleNamesOption();
    }

    // SUPERUSER, CREATEDB, CREATEROLE, INHERIT, LOGIN, REPLICATION, BYPASSRLS and their NO… negations.
    private static bool IsRoleFlagWord(ReadOnlySpan<char> text)
    {
        var word = text.StartsWith(Keyword.No, StringComparison.OrdinalIgnoreCase) ? text[Keyword.No.Length..] : text;
        return TextEquals(word, Keyword.UtilSuperuser) || TextEquals(word, Keyword.UtilCreateDb)
            || TextEquals(word, Keyword.UtilCreateRole) || TextEquals(word, Keyword.UtilInherit)
            || TextEquals(word, Keyword.UtilLogin) || TextEquals(word, Keyword.UtilReplication)
            || TextEquals(word, Keyword.UtilBypassRls);
    }

    private KeywordOption? TryParseRoleValueOption()
    {
        if (IdentifierEquals(Keyword.Connection))
        {
            var connection = Advance();
            return ParseRoleValueOption([connection, Expect(SyntaxKind.LimitKeyword)]);
        }

        if (IdentifierEquals(Keyword.UtilEncrypted) || IdentifierEquals(Keyword.UtilUnencrypted))
        {
            var encrypted = Advance();
            return ParseRoleValueOption([encrypted, ExpectIdent(Keyword.UtilPassword)]);
        }

        if (IdentifierEquals(Keyword.UtilPassword) || IdentifierEquals(Keyword.UtilSysid))
        {
            return ParseRoleValueOption([Advance()]);
        }

        if (IdentifierEquals(Keyword.UtilValid))
        {
            var valid = Advance();
            return ParseRoleValueOption([valid, ExpectIdent(Keyword.Until)]);
        }

        return null;
    }

    private KeywordOption ParseRoleValueOption(List<SyntaxToken> keywords)
    {
        var value = ParseOptionValue();
        return new KeywordOption
        {
            Span = SourceSpan.From(keywords[0], value.Span),
            Keywords = keywords,
            Value = value,
        };
    }

    private KeywordOption? TryParseRoleNamesOption() =>
        IdentifierEquals(Keyword.Role) || IdentifierEquals(Keyword.Admin) ? ParseRoleNamesOption([Advance()]) : null;

    private KeywordOption ParseRoleNamesOption(List<SyntaxToken> keywords)
    {
        var roles = ParseGrantees();
        return new KeywordOption
        {
            Span = SourceSpan.From(keywords[0], roles[^1]),
            Keywords = keywords,
            Roles = roles,
        };
    }

    // REASSIGN OWNED BY role [, ...] TO role
    internal ReassignOwnedStatement ParseReassignOwned()
    {
        var reassignKeyword = Advance();
        var ownedKeyword = ExpectIdent(Keyword.UtilOwned);
        var byKeyword = Expect(SyntaxKind.ByKeyword);
        var roles = ParseGrantees();
        var toKeyword = ExpectIdent(Keyword.To);
        var newRole = ParseGrantee();
        return new ReassignOwnedStatement
        {
            Span = SourceSpan.From(reassignKeyword, newRole),
            ReassignKeyword = reassignKeyword,
            OwnedKeyword = ownedKeyword,
            ByKeyword = byKeyword,
            Roles = roles,
            ToKeyword = toKeyword,
            NewRole = newRole,
        };
    }

    // DROP OWNED BY role [, ...] [CASCADE | RESTRICT]
    internal DropOwnedStatement ParseDropOwned()
    {
        var dropKeyword = Advance();
        var ownedKeyword = Advance();
        var byKeyword = Expect(SyntaxKind.ByKeyword);
        var roles = ParseGrantees();
        var behavior = TryParseDropBehavior();
        return new DropOwnedStatement
        {
            Span = SourceSpan.From(dropKeyword, behavior?.Span ?? roles[^1].Span),
            DropKeyword = dropKeyword,
            OwnedKeyword = ownedKeyword,
            ByKeyword = byKeyword,
            Roles = roles,
            Behavior = behavior,
        };
    }

    // ALTER DEFAULT PRIVILEGES [FOR {ROLE | USER} r, ...] [IN SCHEMA s, ...] {GRANT … | REVOKE …}
    internal AlterDefaultPrivilegesStatement ParseAlterDefaultPrivileges()
    {
        var alterKeyword = Advance();
        var defaultKeyword = Advance();
        var privilegesKeyword = Advance();
        SyntaxToken? forKeyword = null;
        SyntaxToken? roleKeyword = null;
        IReadOnlyList<SyntaxToken> targetRoles = [];
        if (_current.Kind == SyntaxKind.ForKeyword)
        {
            forKeyword = Advance();
            roleKeyword = _current.Kind == SyntaxKind.UserKeyword ? Advance() : ExpectIdent(Keyword.Role);
            targetRoles = ParseGrantees();
        }

        SyntaxToken? inKeyword = null;
        SyntaxToken? schemaKeyword = null;
        IReadOnlyList<IReadOnlyList<SyntaxToken>> schemas = [];
        if (_current.Kind == SyntaxKind.InKeyword)
        {
            inKeyword = Advance();
            schemaKeyword = ExpectIdent(Keyword.Schema);
            schemas = ParseSchemaNameList();
        }

        Query action = IdentifierEquals(Keyword.Grant) ? ParseGrant() : ParseDefaultPrivilegesRevoke();
        return new AlterDefaultPrivilegesStatement
        {
            Span = SourceSpan.From(alterKeyword, action.Span),
            AlterKeyword = alterKeyword,
            DefaultKeyword = defaultKeyword,
            PrivilegesKeyword = privilegesKeyword,
            ForKeyword = forKeyword,
            RoleKeyword = roleKeyword,
            TargetRoles = targetRoles,
            InKeyword = inKeyword,
            SchemaKeyword = schemaKeyword,
            Schemas = schemas,
            Action = action,
        };
    }

    private Query ParseDefaultPrivilegesRevoke()
    {
        if (!IdentifierEquals(Keyword.Revoke))
        {
            throw new SqlParseException($"Expected GRANT or REVOKE, found {_current.Kind}", _current.Position);
        }

        return ParseRevoke();
    }

    // CREATE DATABASE name [WITH] [option [=] value ...]
    internal CreateDatabaseStatement ParseCreateDatabase()
    {
        var createKeyword = Advance();
        var databaseKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var withKeyword = _current.Kind == SyntaxKind.WithKeyword ? Advance() : (SyntaxToken?)null;
        var options = ParseDatabaseOptions();
        var end = options.Count > 0 ? options[^1].Span : withKeyword?.Span ?? name.Span;
        return new CreateDatabaseStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            DatabaseKeyword = databaseKeyword,
            Name = name,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // ALTER DATABASE name {RENAME TO n | OWNER TO r | SET TABLESPACE t | SET … | RESET … | [WITH] option ...}
    internal AlterDatabaseStatement ParseAlterDatabase()
    {
        var alterKeyword = Advance();
        var databaseKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        if (IdentifierEquals(Keyword.Rename) || IdentifierEquals(Keyword.UtilOwner)
            || (_current.Kind == SyntaxKind.SetKeyword && NextEquals(Keyword.UtilTablespace)))
        {
            var verb = Advance();
            var second = TokenEquals(verb, Keyword.Rename) || TokenEquals(verb, Keyword.UtilOwner) ? ExpectIdent(Keyword.To) : Advance();
            var target = ParseGrantee();
            return new AlterDatabaseStatement
            {
                Span = SourceSpan.From(alterKeyword, target),
                AlterKeyword = alterKeyword,
                DatabaseKeyword = databaseKeyword,
                Name = name,
                ActionKeywords = [verb, second],
                Target = target,
            };
        }

        return ParseAlterDatabaseSettings(alterKeyword, databaseKeyword, name);
    }

    private AlterDatabaseStatement ParseAlterDatabaseSettings(SyntaxToken alterKeyword, SyntaxToken databaseKeyword, SyntaxToken name)
    {
        var setting = TryParseSettingAction();
        var withKeyword = setting is null && _current.Kind == SyntaxKind.WithKeyword ? Advance() : (SyntaxToken?)null;
        var options = setting is null ? ParseDatabaseOptions() : [];
        if (setting is null && options.Count == 0)
        {
            throw new SqlParseException($"Expected database option, found {_current.Kind}", _current.Position);
        }

        return new AlterDatabaseStatement
        {
            Span = SourceSpan.From(alterKeyword, setting?.Span ?? options[^1].Span),
            AlterKeyword = alterKeyword,
            DatabaseKeyword = databaseKeyword,
            Name = name,
            WithKeyword = withKeyword,
            Options = options,
            Setting = setting,
        };
    }

    // DROP DATABASE [IF EXISTS] name [[WITH] (FORCE [, ...])]
    internal DropDatabaseStatement ParseDropDatabase()
    {
        var dropKeyword = Advance();
        var databaseKeyword = Advance();
        var (ifKeyword, existsKeyword) = ParseIfExists();
        var name = Expect(SyntaxKind.Identifier);
        var withKeyword = _current.Kind == SyntaxKind.WithKeyword ? Advance() : (SyntaxToken?)null;
        var options = withKeyword is not null || _current.Kind == SyntaxKind.OpenParen
            ? ParseUtilityOptionList()
            : new UtilityOptionList(null, [], null);
        return new DropDatabaseStatement
        {
            Span = SourceSpan.From(dropKeyword, options.CloseParen ?? name),
            DropKeyword = dropKeyword,
            DatabaseKeyword = databaseKeyword,
            IfKeyword = ifKeyword,
            ExistsKeyword = existsKeyword,
            Name = name,
            WithKeyword = withKeyword,
            OpenParen = options.OpenParen,
            Options = options.Options,
            CloseParen = options.CloseParen,
        };
    }

    private IReadOnlyList<KeywordOption> ParseDatabaseOptions()
    {
        List<KeywordOption>? options = null;
        while (_current.Kind == SyntaxKind.Identifier)
        {
            (options ??= []).Add(ParseDatabaseOption());
        }

        return options ?? (IReadOnlyList<KeywordOption>)[];
    }

    private KeywordOption ParseDatabaseOption()
    {
        var first = Advance();
        List<SyntaxToken> keywords = TokenEquals(first, Keyword.Connection) && _current.Kind == SyntaxKind.LimitKeyword
            ? [first, Advance()]
            : [first];
        var equalsToken = _current.Kind == SyntaxKind.EqualsToken ? Advance() : (SyntaxToken?)null;
        var value = ParseOptionValue();
        return new KeywordOption
        {
            Span = SourceSpan.From(first, value.Span),
            Keywords = keywords,
            EqualsToken = equalsToken,
            Value = value,
        };
    }
}
