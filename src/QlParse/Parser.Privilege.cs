namespace QlParse;

internal sealed partial class Parser
{
    private Query ParseGrant() => NextIsPrivilege() ? ParseGrantPrivilege() : ParseGrantRole();

    private Query ParseRevoke()
    {
        if (NextEquals(Keyword.Admin))
        {
            return ParseRevokeRole();
        }

        if (NextEquals(Keyword.Grant) || NextEquals(Keyword.Hierarchy) || NextIsPrivilege())
        {
            return ParseRevokePrivilege();
        }

        return ParseRevokeRole();
    }

    private GrantPrivilegeStatement ParseGrantPrivilege()
    {
        var grantKeyword = Advance();
        var actions = ParsePrivilegeActions();
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var target = ParsePrivilegeObject();
        var toKeyword = ExpectIdent(Keyword.To);
        var grantees = ParseGrantees();
        ParsePrivilegeOptions(out var hierarchy, out var grantOption);
        ParseGrantedBy(out var granted, out var by, out var grantor);
        var end = grantees[^1].Span;
        if (hierarchy is not null)
        {
            end = hierarchy.Span;
        }

        if (grantOption is not null)
        {
            end = grantOption.Span;
        }

        if (grantor is SyntaxToken grantorToken)
        {
            end = grantorToken.Span;
        }

        return new GrantPrivilegeStatement
        {
            Span = SourceSpan.From(grantKeyword, end),
            GrantKeyword = grantKeyword,
            Actions = actions,
            OnKeyword = onKeyword,
            Object = target,
            ToKeyword = toKeyword,
            Grantees = grantees,
            HierarchyOption = hierarchy,
            GrantOption = grantOption,
            GrantedKeyword = granted,
            ByKeyword = by,
            Grantor = grantor,
        };
    }

    private GrantRoleStatement ParseGrantRole()
    {
        var grantKeyword = Advance();
        var roles = ParseRoleList();
        var toKeyword = ExpectIdent(Keyword.To);
        var grantees = ParseGrantees();
        GrantOptionClause? admin = null;
        if (IsWithOption(Keyword.Admin))
        {
            admin = ParseWithOption();
        }

        ParseGrantedBy(out var granted, out var by, out var grantor);
        var end = grantees[^1].Span;
        if (admin is not null)
        {
            end = admin.Span;
        }

        if (grantor is SyntaxToken grantorToken)
        {
            end = grantorToken.Span;
        }

        return new GrantRoleStatement
        {
            Span = SourceSpan.From(grantKeyword, end),
            GrantKeyword = grantKeyword,
            Roles = roles,
            ToKeyword = toKeyword,
            Grantees = grantees,
            AdminOption = admin,
            GrantedKeyword = granted,
            ByKeyword = by,
            Grantor = grantor,
        };
    }

    private RevokePrivilegeStatement ParseRevokePrivilege()
    {
        var revokeKeyword = Advance();
        var option = ParseRevokeOption();
        var actions = ParsePrivilegeActions();
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var target = ParsePrivilegeObject();
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var grantees = ParseGrantees();
        ParseGrantedBy(out var granted, out var by, out var grantor);
        var behavior = ParseDropBehavior();
        return new RevokePrivilegeStatement
        {
            Span = SourceSpan.From(revokeKeyword, behavior),
            RevokeKeyword = revokeKeyword,
            Option = option,
            Actions = actions,
            OnKeyword = onKeyword,
            Object = target,
            FromKeyword = fromKeyword,
            Grantees = grantees,
            GrantedKeyword = granted,
            ByKeyword = by,
            Grantor = grantor,
            Behavior = behavior,
        };
    }

    private RevokeRoleStatement ParseRevokeRole()
    {
        var revokeKeyword = Advance();
        var option = ParseRevokeOption();
        var roles = ParseRoleList();
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var grantees = ParseGrantees();
        ParseGrantedBy(out var granted, out var by, out var grantor);
        var behavior = ParseDropBehavior();
        return new RevokeRoleStatement
        {
            Span = SourceSpan.From(revokeKeyword, behavior),
            RevokeKeyword = revokeKeyword,
            Option = option,
            Roles = roles,
            FromKeyword = fromKeyword,
            Grantees = grantees,
            GrantedKeyword = granted,
            ByKeyword = by,
            Grantor = grantor,
            Behavior = behavior,
        };
    }

    private CreateRoleStatement ParseCreateRole()
    {
        var createKeyword = Advance();
        var roleKeyword = ExpectIdent(Keyword.Role);
        var name = Expect(SyntaxKind.Identifier);
        SyntaxToken? withKeyword = null;
        SyntaxToken? adminKeyword = null;
        SyntaxToken? grantor = null;
        var end = name.Span;
        if (_current.Kind == SyntaxKind.WithKeyword)
        {
            withKeyword = Advance();
            adminKeyword = ExpectIdent(Keyword.Admin);
            var grantorToken = ParseGrantee();
            grantor = grantorToken;
            end = grantorToken.Span;
        }

        return new CreateRoleStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            RoleKeyword = roleKeyword,
            Name = name,
            WithKeyword = withKeyword,
            AdminKeyword = adminKeyword,
            Grantor = grantor,
        };
    }

    private DropRoleStatement ParseDropRole()
    {
        var dropKeyword = Advance();
        var roleKeyword = ExpectIdent(Keyword.Role);
        var name = Expect(SyntaxKind.Identifier);
        return new DropRoleStatement
        {
            Span = SourceSpan.From(dropKeyword, name),
            DropKeyword = dropKeyword,
            RoleKeyword = roleKeyword,
            Name = name,
        };
    }

    private SetRoleStatement ParseSetRole()
    {
        var setKeyword = Advance();
        var roleKeyword = ExpectIdent(Keyword.Role);
        var value = ParseRoleSpecification();
        return new SetRoleStatement
        {
            Span = SourceSpan.From(setKeyword, value),
            SetKeyword = setKeyword,
            RoleKeyword = roleKeyword,
            Value = value,
        };
    }

    private List<PrivilegeAction> ParsePrivilegeActions()
    {
        if (_current.Kind == SyntaxKind.AllKeyword)
        {
            var all = Advance();
            var privileges = ExpectIdent(Keyword.Privileges);
            return
            [
                new PrivilegeAction
                {
                    Span = SourceSpan.From(all, privileges),
                    Name = all,
                    PrivilegesKeyword = privileges,
                },
            ];
        }

        var actions = new List<PrivilegeAction> { ParsePrivilegeAction() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            actions.Add(ParsePrivilegeAction());
        }

        return actions;
    }

    private PrivilegeAction ParsePrivilegeAction()
    {
        if (!IsPrivilegeAction())
        {
            throw new SqlParseException($"Expected privilege, found {_current.Kind}", _current.Position);
        }

        var name = Advance();
        SyntaxToken? openParen = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        SyntaxToken? closeParen = null;
        var end = name.Span;
        if (AllowsPrivilegeColumns(name) && _current.Kind == SyntaxKind.OpenParen)
        {
            ParseRequiredNameList(out var columnOpen, out var columnNames, out var columnClose);
            openParen = columnOpen;
            columns = columnNames;
            closeParen = columnClose;
            end = columnClose.Span;
        }

        return new PrivilegeAction
        {
            Span = SourceSpan.From(name, end),
            Name = name,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = closeParen,
        };
    }

    private PrivilegeObject ParsePrivilegeObject()
    {
        if (IdentifierEquals(Keyword.Domain) || IdentifierEquals(Keyword.Collation)
            || IdentifierEquals(Keyword.Translation) || IdentifierEquals(Keyword.Type)
            || IdentifierEquals(Keyword.Sequence))
        {
            var kind = Advance();
            var name = ParseQualifiedName();
            return new PrivilegeObject
            {
                Span = SourceSpan.From(kind, name[^1]),
                Kind = kind,
                Name = name,
            };
        }

        if (IdentifierEquals(Keyword.Character) && NextKind == SyntaxKind.SetKeyword)
        {
            var character = Advance();
            var setKeyword = Advance();
            var name = ParseQualifiedName();
            return new PrivilegeObject
            {
                Span = SourceSpan.From(character, name[^1]),
                Kind = character,
                SetKeyword = setKeyword,
                Name = name,
            };
        }

        if (IsRoutineDesignatorStart())
        {
            var routine = ParseRoutineDesignator();
            return new PrivilegeObject { Span = routine.Span, Routine = routine };
        }

        SyntaxToken? table = null;
        if (IdentifierEquals(Keyword.Table))
        {
            table = Advance();
        }

        var objectName = ParseQualifiedName();
        var start = table ?? objectName[0];
        return new PrivilegeObject
        {
            Span = SourceSpan.From(start, objectName[^1]),
            Kind = table,
            Name = objectName,
        };
    }

    private List<SyntaxToken> ParseRoleList()
    {
        var roles = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            roles.Add(Expect(SyntaxKind.Identifier));
        }

        return roles;
    }

    private List<SyntaxToken> ParseGrantees()
    {
        var grantees = new List<SyntaxToken> { ParseGrantee() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            grantees.Add(ParseGrantee());
        }

        return grantees;
    }

    private SyntaxToken ParseGrantee()
    {
        if (!IsGrantee())
        {
            throw new SqlParseException($"Expected grantee, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    private SyntaxToken ParseRoleSpecification()
    {
        if (IdentifierEquals(Keyword.None) || IsGrantee()
            || _current.Kind is SyntaxKind.String or SyntaxKind.QuestionMark or SyntaxKind.EmbeddedHost)
        {
            return Advance();
        }

        throw new SqlParseException($"Expected role, found {_current.Kind}", _current.Position);
    }

    private void ParsePrivilegeOptions(out GrantOptionClause? hierarchy, out GrantOptionClause? grantOption)
    {
        hierarchy = null;
        grantOption = null;
        while (IsWithOption(Keyword.Hierarchy) || IsWithOption(Keyword.Grant))
        {
            if (IsWithOption(Keyword.Hierarchy))
            {
                if (hierarchy is not null)
                {
                    throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
                }

                hierarchy = ParseWithOption();
                continue;
            }

            if (grantOption is not null)
            {
                throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
            }

            grantOption = ParseWithOption();
        }
    }

    private GrantOptionClause ParseWithOption()
    {
        var withKeyword = Advance();
        var kind = Advance();
        var optionKeyword = ExpectIdent(Keyword.Option);
        return new GrantOptionClause
        {
            Span = SourceSpan.From(withKeyword, optionKeyword),
            WithKeyword = withKeyword,
            Kind = kind,
            OptionKeyword = optionKeyword,
        };
    }

    private RevokeOptionClause? ParseRevokeOption()
    {
        if (!IdentifierEquals(Keyword.Grant) && !IdentifierEquals(Keyword.Hierarchy) && !IdentifierEquals(Keyword.Admin))
        {
            return null;
        }

        var kind = Advance();
        var optionKeyword = ExpectIdent(Keyword.Option);
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        return new RevokeOptionClause
        {
            Span = SourceSpan.From(kind, forKeyword),
            Kind = kind,
            OptionKeyword = optionKeyword,
            ForKeyword = forKeyword,
        };
    }

    private void ParseGrantedBy(out SyntaxToken? grantedKeyword, out SyntaxToken? byKeyword, out SyntaxToken? grantor)
    {
        grantedKeyword = null;
        byKeyword = null;
        grantor = null;
        if (!IdentifierEquals(Keyword.Granted))
        {
            return;
        }

        grantedKeyword = Advance();
        byKeyword = Expect(SyntaxKind.ByKeyword);
        grantor = ParseGrantee();
    }

    private bool NextIsPrivilege() =>
        _index < _tokens.Count && TokenIsPrivilege(_tokens[_index]);

    private bool IsPrivilegeAction() => TokenIsPrivilege(_current);

    private bool TokenIsPrivilege(SyntaxToken token) =>
        token.Kind is SyntaxKind.AllKeyword or SyntaxKind.SelectKeyword or SyntaxKind.UpdateKeyword
        || TokenEquals(token, Keyword.Insert)
        || TokenEquals(token, Keyword.Delete)
        || TokenEquals(token, Keyword.References)
        || TokenEquals(token, Keyword.Usage)
        || TokenEquals(token, Keyword.Trigger)
        || TokenEquals(token, Keyword.Under)
        || TokenEquals(token, Keyword.Execute);

    private bool AllowsPrivilegeColumns(SyntaxToken name) =>
        name.Kind is SyntaxKind.SelectKeyword or SyntaxKind.UpdateKeyword
        || TokenEquals(name, Keyword.Insert)
        || TokenEquals(name, Keyword.References);

    private bool IsWithOption(string kind) =>
        _current.Kind == SyntaxKind.WithKeyword && NextEquals(kind);

    private bool IsGrantee() =>
        _current.Kind is SyntaxKind.Identifier or SyntaxKind.CurrentUserKeyword or SyntaxKind.CurrentRoleKeyword
            or SyntaxKind.SessionUserKeyword or SyntaxKind.SystemUserKeyword or SyntaxKind.UserKeyword;
}
