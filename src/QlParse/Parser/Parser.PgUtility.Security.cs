namespace QlParse;

internal sealed partial class Parser
{
    private readonly record struct PolicyHead(
        SyntaxToken Verb,
        SyntaxToken PolicyKeyword,
        SyntaxToken Name,
        SyntaxToken OnKeyword,
        IReadOnlyList<SyntaxToken> Table);

    private readonly record struct PolicyTail(
        SyntaxToken? ToKeyword,
        IReadOnlyList<SyntaxToken> Roles,
        PolicyCondition? Using,
        PolicyCondition? WithCheck);

    private readonly record struct PolicyKindClause(
        SyntaxToken? AsKeyword,
        SyntaxToken? PolicyType,
        SyntaxToken? ForKeyword,
        SyntaxToken? Command);

    private readonly record struct CommentTargetClause(
        SyntaxToken? OnKeyword,
        IReadOnlyList<SyntaxToken>? TableName);

    // CREATE POLICY name ON table [AS {PERMISSIVE | RESTRICTIVE}] [FOR command] [TO role, ...]
    //     [USING (expr)] [WITH CHECK (expr)]
    internal CreatePolicyStatement ParseCreatePolicy()
    {
        var head = ParsePolicyHead();
        var kind = ParsePolicyKindClause();
        var tail = ParsePolicyTail();
        return new CreatePolicyStatement
        {
            Span = SourceSpan.From(head.Verb, PolicyEnd(head, kind, tail)),
            CreateKeyword = head.Verb,
            PolicyKeyword = head.PolicyKeyword,
            Name = head.Name,
            OnKeyword = head.OnKeyword,
            Table = head.Table,
            AsKeyword = kind.AsKeyword,
            PolicyType = kind.PolicyType,
            ForKeyword = kind.ForKeyword,
            Command = kind.Command,
            ToKeyword = tail.ToKeyword,
            Roles = tail.Roles,
            Using = tail.Using,
            WithCheck = tail.WithCheck,
        };
    }

    // ALTER POLICY name ON table {RENAME TO new | [TO role, ...] [USING (expr)] [WITH CHECK (expr)]}
    internal AlterPolicyStatement ParseAlterPolicy()
    {
        var head = ParsePolicyHead();
        if (IdentifierEquals(Keyword.Rename))
        {
            var rename = ParseRoleRenameClause();
            return new AlterPolicyStatement
            {
                Span = SourceSpan.From(head.Verb, rename.NewName),
                AlterKeyword = head.Verb,
                PolicyKeyword = head.PolicyKeyword,
                Name = head.Name,
                OnKeyword = head.OnKeyword,
                Table = head.Table,
                RenameKeyword = rename.RenameKeyword,
                RenameToKeyword = rename.ToKeyword,
                NewName = rename.NewName,
            };
        }

        var tail = ParsePolicyTail();
        return new AlterPolicyStatement
        {
            Span = SourceSpan.From(head.Verb, PolicyEnd(head, default, tail)),
            AlterKeyword = head.Verb,
            PolicyKeyword = head.PolicyKeyword,
            Name = head.Name,
            OnKeyword = head.OnKeyword,
            Table = head.Table,
            ToKeyword = tail.ToKeyword,
            Roles = tail.Roles,
            Using = tail.Using,
            WithCheck = tail.WithCheck,
        };
    }

    // DROP POLICY [IF EXISTS] name ON table [CASCADE | RESTRICT]
    internal DropPolicyStatement ParseDropPolicy()
    {
        var dropKeyword = Advance();
        var policyKeyword = Advance();
        var (ifKeyword, existsKeyword) = ParseIfExists();
        var name = Expect(SyntaxKind.Identifier);
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var table = ParseQualifiedName();
        var behavior = TryParseDropBehavior();
        return new DropPolicyStatement
        {
            Span = SourceSpan.From(dropKeyword, behavior ?? table[^1]),
            DropKeyword = dropKeyword,
            PolicyKeyword = policyKeyword,
            IfKeyword = ifKeyword,
            ExistsKeyword = existsKeyword,
            Name = name,
            OnKeyword = onKeyword,
            Table = table,
            Behavior = behavior,
        };
    }

    private PolicyHead ParsePolicyHead()
    {
        var verb = Advance();
        var policyKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        return new PolicyHead(verb, policyKeyword, name, onKeyword, ParseQualifiedName());
    }

    private PolicyKindClause ParsePolicyKindClause()
    {
        SyntaxToken? asKeyword = null;
        SyntaxToken? policyType = null;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            if (!IdentifierEquals(Keyword.UtilPermissive) && !IdentifierEquals(Keyword.UtilRestrictive))
            {
                throw new SqlParseException($"Expected PERMISSIVE or RESTRICTIVE, found {_current.Kind}", _current.Position);
            }

            policyType = Advance();
        }

        if (_current.Kind != SyntaxKind.ForKeyword)
        {
            return new PolicyKindClause(asKeyword, policyType, null, null);
        }

        var forKeyword = Advance();
        if (_current.Kind is not (SyntaxKind.AllKeyword or SyntaxKind.SelectKeyword or SyntaxKind.UpdateKeyword)
            && !IdentifierEquals(Keyword.Insert) && !IdentifierEquals(Keyword.Delete))
        {
            throw new SqlParseException($"Expected ALL, SELECT, INSERT, UPDATE, or DELETE, found {_current.Kind}", _current.Position);
        }

        return new PolicyKindClause(asKeyword, policyType, forKeyword, Advance());
    }

    private PolicyTail ParsePolicyTail()
    {
        SyntaxToken? toKeyword = null;
        IReadOnlyList<SyntaxToken> roles = [];
        if (IdentifierEquals(Keyword.To))
        {
            toKeyword = Advance();
            roles = ParseGrantees();
        }

        PolicyCondition? usingCondition = null;
        if (_current.Kind == SyntaxKind.UsingKeyword)
        {
            usingCondition = ParsePolicyCondition([Advance()]);
        }

        PolicyCondition? withCheck = null;
        if (_current.Kind == SyntaxKind.WithKeyword && NextEquals(Keyword.Check))
        {
            var withKeyword = Advance();
            withCheck = ParsePolicyCondition([withKeyword, Advance()]);
        }

        return new PolicyTail(toKeyword, roles, usingCondition, withCheck);
    }

    private PolicyCondition ParsePolicyCondition(List<SyntaxToken> keywords)
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var expression = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PolicyCondition
        {
            Span = SourceSpan.From(keywords[0], closeParen),
            Keywords = keywords,
            OpenParen = openParen,
            Expression = expression,
            CloseParen = closeParen,
        };
    }

    private static SourceSpan PolicyEnd(PolicyHead head, PolicyKindClause kind, PolicyTail tail)
    {
        if (tail.WithCheck is not null)
        {
            return tail.WithCheck.Span;
        }

        if (tail.Using is not null)
        {
            return tail.Using.Span;
        }

        if (tail.Roles.Count > 0)
        {
            return tail.Roles[^1].Span;
        }

        return kind.Command?.Span ?? kind.PolicyType?.Span ?? head.Table[^1].Span;
    }

    // ALTER TABLE … {ENABLE | DISABLE | FORCE | NO FORCE} ROW LEVEL SECURITY; null for anything else.
    internal RowLevelSecurityAction? TryParseRowLevelSecurityAction()
    {
        var hasNo = IdentifierEquals(Keyword.No) && NextEquals(Keyword.UtilForce);
        if (!hasNo && !IdentifierEquals(Keyword.UtilEnable) && !IdentifierEquals(Keyword.Disable)
            && !IdentifierEquals(Keyword.UtilForce))
        {
            return null;
        }

        if (!IsKeywordAt(hasNo ? _index + 1 : _index, Keyword.Row))
        {
            return null;
        }

        var noKeyword = hasNo ? Advance() : (SyntaxToken?)null;
        var verb = Advance();
        var rowKeyword = Advance();
        var levelKeyword = ExpectIdent(Keyword.Level);
        var securityKeyword = ExpectIdent(Keyword.UtilSecurity);
        return new RowLevelSecurityAction
        {
            Span = SourceSpan.From(noKeyword ?? verb, securityKeyword),
            NoKeyword = noKeyword,
            VerbKeyword = verb,
            RowKeyword = rowKeyword,
            LevelKeyword = levelKeyword,
            SecurityKeyword = securityKeyword,
        };
    }

    // SECURITY LABEL [FOR provider] ON object_kind name IS {'label' | NULL}
    internal SecurityLabelStatement ParseSecurityLabel()
    {
        var securityKeyword = Advance();
        var labelKeyword = Advance();
        SyntaxToken? forKeyword = null;
        SyntaxToken? provider = null;
        if (_current.Kind == SyntaxKind.ForKeyword)
        {
            forKeyword = Advance();
            provider = _current.Kind == SyntaxKind.String ? Advance() : Expect(SyntaxKind.Identifier);
        }

        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var kind = ParseObjectKind();
        var target = ParseObjectSignature();
        var isKeyword = Expect(SyntaxKind.IsKeyword);
        var value = ParseStringOrNull();
        return new SecurityLabelStatement
        {
            Span = SourceSpan.From(securityKeyword, value),
            SecurityKeyword = securityKeyword,
            LabelKeyword = labelKeyword,
            ForKeyword = forKeyword,
            Provider = provider,
            OnKeyword = onKeyword,
            ObjectKind = kind,
            Object = target,
            IsKeyword = isKeyword,
            Value = value,
        };
    }

    // COMMENT ON object_kind name [(arg types)] [ON table] IS {'text' | NULL}
    internal CommentStatement ParsePgComment()
    {
        var commentKeyword = Advance();
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var kind = ParseObjectKind();
        var target = ParseObjectSignature();
        var tableClause = ParseCommentTargetClause(kind[0]);
        var isKeyword = Expect(SyntaxKind.IsKeyword);
        var value = ParseStringOrNull();
        return new CommentStatement
        {
            Span = SourceSpan.From(commentKeyword, value),
            CommentKeyword = commentKeyword,
            OnKeyword = onKeyword,
            Kind = kind[0],
            Name = target.Name,
            IsKeyword = isKeyword,
            Value = value,
            Postgres = BuildPgCommentExtras(kind, target, tableClause),
        };
    }

    // Null for the ISO shape `COMMENT ON kind name IS ...`.
    private static PgCommentExtras? BuildPgCommentExtras(List<SyntaxToken> kind, ObjectSignature target, CommentTargetClause tableClause)
    {
        if (kind.Count == 1 && target.OpenParen is null && tableClause.OnKeyword is null)
        {
            return null;
        }

        return new PgCommentExtras
        {
            ExtraKindKeywords = kind.Count > 1 ? kind[1..] : [],
            OpenParen = target.OpenParen,
            ArgumentTypes = target.ArgumentTypes,
            CloseParen = target.CloseParen,
            TargetOnKeyword = tableClause.OnKeyword,
            TableName = tableClause.TableName,
        };
    }

    // TRIGGER, POLICY, RULE and table CONSTRAINT names are scoped by an ON table clause.
    private CommentTargetClause ParseCommentTargetClause(SyntaxToken kind)
    {
        if (_current.Kind != SyntaxKind.OnKeyword)
        {
            return default;
        }

        var onKeyword = Advance();
        if (TokenEquals(kind, Keyword.Constraint) && IdentifierEquals(Keyword.Domain))
        {
            var domain = Advance();
            var domainName = ParseQualifiedName();
            domainName.Insert(0, domain);
            return new CommentTargetClause(onKeyword, domainName);
        }

        return new CommentTargetClause(onKeyword, ParseQualifiedName());
    }

    private SyntaxToken ParseStringOrNull()
    {
        if (_current.Kind is not (SyntaxKind.String or SyntaxKind.NullKeyword))
        {
            throw new SqlParseException($"Expected string or NULL, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    // One object-kind word, or a multi-word kind: MATERIALIZED VIEW, FOREIGN TABLE, FOREIGN DATA WRAPPER,
    // EVENT TRIGGER, LARGE OBJECT, ACCESS METHOD, PROCEDURAL LANGUAGE.
    private List<SyntaxToken> ParseObjectKind()
    {
        if (_current.Kind is SyntaxKind.EndOfFile or SyntaxKind.IsKeyword)
        {
            throw new SqlParseException($"Expected object kind, found {_current.Kind}", _current.Position);
        }

        var first = Advance();
        if (TokenEquals(first, Keyword.Foreign) && IdentifierEquals(Keyword.Data))
        {
            var data = Advance();
            return [first, data, ExpectIdent(Keyword.Wrapper)];
        }

        return IsTwoWordObjectKind(first) ? [first, Advance()] : [first];
    }

    private bool IsTwoWordObjectKind(SyntaxToken first) =>
        TokenEquals(first, Keyword.UtilMaterialized) || TokenEquals(first, Keyword.Foreign)
        || TokenEquals(first, Keyword.UtilEvent) || TokenEquals(first, Keyword.Large)
        || TokenEquals(first, Keyword.UtilAccess) || TokenEquals(first, Keyword.UtilProcedural);

    // name [(arg type, ...)] — a large object is named by its OID instead.
    internal ObjectSignature ParseObjectSignature()
    {
        if (_current.Kind == SyntaxKind.Number)
        {
            var oid = Advance();
            return new ObjectSignature { Span = oid.Span, Name = [oid] };
        }

        var name = ParseQualifiedName();
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new ObjectSignature { Span = SourceSpan.From(name[0], name[^1]), Name = name };
        }

        var openParen = Advance();
        IReadOnlyList<DataType> types = _current.Kind == SyntaxKind.CloseParen ? [] : ParseDataTypeList();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new ObjectSignature
        {
            Span = SourceSpan.From(name[0], closeParen),
            Name = name,
            OpenParen = openParen,
            ArgumentTypes = types,
            CloseParen = closeParen,
        };
    }
}
