namespace QlParse;

// The generic Postgres ALTER statement for the schema-object kinds. Every kind shares the rename,
// owner, schema, foreign-data options and definition-list actions; the action dispatch below also
// covers enum values, type attributes, extension members, publication tables, subscription commands,
// text search mappings and operator family members.
internal sealed partial class Parser
{
    internal enum PgAlterContext
    {
        General,
        Extension,
        OperatorFamily,
        ForeignTable,
    }

    private PgAlterStatement ParsePgAlter(PgObjectKind kind)
    {
        var alterKeyword = Advance();
        var objectKind = ParsePgObjectKindTokens(kind.Count);
        var ifExists = ParsePgIfExists();
        var isTrigger = kind.Shape == PgObjectShape.Trigger;
        var target = ParsePgObjectReference(isTrigger ? PgObjectShape.Name : kind.Shape);
        SyntaxToken? onKeyword = isTrigger ? Expect(SyntaxKind.OnKeyword) : null;
        IReadOnlyList<SyntaxToken>? table = isTrigger ? ParseQualifiedName() : null;
        var actions = ParsePgAlterActions(PgAlterContextOf(objectKind));
        return new PgAlterStatement
        {
            Span = SourceSpan.From(alterKeyword, actions[^1].Span),
            AlterKeyword = alterKeyword,
            ObjectKind = objectKind,
            IfKeyword = ifExists.IfKeyword,
            ExistsKeyword = ifExists.ExistsKeyword,
            Target = target,
            OnKeyword = onKeyword,
            Table = table,
            Actions = actions,
        };
    }

    private PgAlterContext PgAlterContextOf(List<SyntaxToken> objectKind)
    {
        if (TokenEquals(objectKind[0], Keyword.PgExtension))
        {
            return PgAlterContext.Extension;
        }

        return objectKind.Count == Len2 && TokenEquals(objectKind[1], Keyword.PgFamily)
            ? PgAlterContext.OperatorFamily
            : PgForeignTableContextOf(objectKind);
    }

    // Actions are comma-separated (ALTER TYPE attributes) or simply juxtaposed (ALTER SERVER VERSION ... OPTIONS ...).
    private List<PgAlterAction> ParsePgAlterActions(PgAlterContext context)
    {
        var actions = new List<PgAlterAction> { ParsePgAlterAction(context) };
        while (true)
        {
            if (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                actions.Add(ParsePgAlterAction(context));
                continue;
            }

            if (!IsPgJuxtaposedActionStart())
            {
                break;
            }

            actions.Add(ParsePgAlterAction(context));
        }

        return actions;
    }

    private bool IsPgJuxtaposedActionStart() =>
        IsPgOptionsStart() || IdentifierEquals(Keyword.PgVersion) || IdentifierEquals(Keyword.Handler)
        || IdentifierEquals(Keyword.PgValidator) || IdentifierEquals(Keyword.No);

    private PgAlterAction ParsePgAlterAction(PgAlterContext context)
    {
        if (_current.Kind == SyntaxKind.SetKeyword)
        {
            return ParsePgAlterSet();
        }

        if (_current.Kind == SyntaxKind.OpenParen)
        {
            return ParsePgDefinitionAction(null);
        }

        if (_current.Kind == SyntaxKind.UpdateKeyword)
        {
            return ParsePgUpdateAction();
        }

        return TryParsePgCommonAction()
            ?? TryParsePgMemberChange(context)
            ?? TryParsePgCommandAction()
            ?? throw new SqlParseException($"Expected ALTER action, found {_current.Kind}", _current.Position);
    }

    private PgAlterAction? TryParsePgCommonAction()
    {
        if (IdentifierEquals(Keyword.Rename))
        {
            return ParsePgObjectRenameAction();
        }

        if (IdentifierEquals(Keyword.PgOwner))
        {
            var ownerKeyword = Advance();
            var toKeyword = ExpectIdent(Keyword.To);
            var owner = ExpectPgRole();
            return new PgOwnerAction
            {
                Span = SourceSpan.From(ownerKeyword, owner),
                OwnerKeyword = ownerKeyword,
                ToKeyword = toKeyword,
                Owner = owner,
            };
        }

        if (IsPgOptionsStart())
        {
            var options = ParsePgOptionList();
            return new PgOptionsAction { Span = options.Span, Options = options };
        }

        if ((IdentifierEquals(Keyword.PgReset) || IdentifierEquals(Keyword.Skip)) && NextKind == SyntaxKind.OpenParen)
        {
            return ParsePgDefinitionAction(Advance());
        }

        return null;
    }

    private PgDefinitionAction ParsePgDefinitionAction(SyntaxToken? keyword)
    {
        var definition = ParsePgDefinitionList();
        return new PgDefinitionAction
        {
            Span = keyword is { } start ? SourceSpan.From(start, definition.Span) : definition.Span,
            Keyword = keyword,
            Definition = definition,
        };
    }

    // RENAME TO n | RENAME VALUE 'a' TO 'b' | RENAME {ATTRIBUTE|COLUMN} a TO b [CASCADE|RESTRICT] | RENAME a TO b
    private PgRenameAction ParsePgObjectRenameAction()
    {
        var renameKeyword = Advance();
        SyntaxToken? kind = null;
        SyntaxToken? oldName = null;
        if (!IdentifierEquals(Keyword.To))
        {
            if ((IdentifierEquals(Keyword.Value) || IdentifierEquals(Keyword.PgAttribute) || IdentifierEquals(Keyword.Column))
                && !IsKeywordAt(_index, Keyword.To))
            {
                kind = Advance();
            }

            oldName = ExpectPgNameOrString();
        }

        var toKeyword = ExpectIdent(Keyword.To);
        var newName = ExpectPgNameOrString();
        var behavior = ParsePgOptionalBehavior();
        return new PgRenameAction
        {
            Span = SourceSpan.From(renameKeyword, behavior ?? newName),
            RenameKeyword = renameKeyword,
            Kind = kind,
            OldName = oldName,
            ToKeyword = toKeyword,
            NewName = newName,
            Behavior = behavior,
        };
    }

    // SET SCHEMA s | SET (...) | SET PUBLICATION ... | SET TABLE ... | SET TABLES IN SCHEMA ... | SET STATISTICS n
    private PgAlterAction ParsePgAlterSet()
    {
        if (NextKind == SyntaxKind.OpenParen)
        {
            return ParsePgDefinitionAction(Advance());
        }

        if (NextEquals(Keyword.Schema))
        {
            var setKeyword = Advance();
            var schemaKeyword = Advance();
            var schema = Expect(SyntaxKind.Identifier);
            return new PgSetSchemaAction
            {
                Span = SourceSpan.From(setKeyword, schema),
                SetKeyword = setKeyword,
                SchemaKeyword = schemaKeyword,
                Schema = schema,
            };
        }

        if (NextEquals(Keyword.Table) || NextEquals(Keyword.Tables))
        {
            return ParsePgPublicationObjectsAction();
        }

        if (NextEquals(Keyword.PgPublication))
        {
            return ParsePgPublicationListAction();
        }

        if (NextEquals(Keyword.PgStatistics))
        {
            var keywords = ParsePgObjectKindTokens(Len2);
            return BuildPgKeywordAction(keywords, [Expect(SyntaxKind.Number)], null, null);
        }

        throw new SqlParseException($"Expected SET action, found {_current.Kind}", _current.Position);
    }

    // UPDATE [TO version]
    private PgKeywordAction ParsePgUpdateAction()
    {
        var keywords = new List<SyntaxToken> { Advance() };
        if (!IdentifierEquals(Keyword.To))
        {
            return BuildPgKeywordAction(keywords, [], null, null);
        }

        keywords.Add(Advance());
        return BuildPgKeywordAction(keywords, [ExpectPgNameOrString()], null, null);
    }

    // {SET|ADD|DROP} PUBLICATION name [, ...] [WITH (...)]
    private PgKeywordAction ParsePgPublicationListAction()
    {
        var keywords = ParsePgObjectKindTokens(Len2);
        var names = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            names.Add(Expect(SyntaxKind.Identifier));
        }

        return ParsePgKeywordActionWith(keywords, names);
    }

    private PgKeywordAction ParsePgKeywordActionWith(List<SyntaxToken> keywords, List<SyntaxToken> values)
    {
        if (_current.Kind != SyntaxKind.WithKeyword)
        {
            return BuildPgKeywordAction(keywords, values, null, null);
        }

        var withKeyword = Advance();
        return BuildPgKeywordAction(keywords, values, withKeyword, ParsePgDefinitionList());
    }

    private static PgKeywordAction BuildPgKeywordAction(
        List<SyntaxToken> keywords,
        List<SyntaxToken> values,
        SyntaxToken? withKeyword,
        PgDefinitionList? definition)
    {
        var end = definition?.Span ?? (values.Count > 0 ? values[^1].Span : keywords[^1].Span);
        return new PgKeywordAction
        {
            Span = SourceSpan.From(keywords[0], end),
            Keywords = keywords,
            Values = values,
            WithKeyword = withKeyword,
            Definition = definition,
        };
    }

    // ENABLE [REPLICA|ALWAYS] | DISABLE | CONNECTION 'c' | REFRESH {PUBLICATION [WITH (...)] | VERSION}
    // | VERSION 'v' | [NO] HANDLER f | [NO] VALIDATOR f | [NO] DEPENDS ON EXTENSION e
    private PgKeywordAction? TryParsePgCommandAction()
    {
        if (IdentifierEquals(Keyword.PgEnable))
        {
            var keywords = new List<SyntaxToken> { Advance() };
            if (IdentifierEquals(Keyword.PgReplica) || IdentifierEquals(Keyword.Always))
            {
                keywords.Add(Advance());
            }

            return BuildPgKeywordAction(keywords, [], null, null);
        }

        if (IdentifierEquals(Keyword.Disable))
        {
            return BuildPgKeywordAction([Advance()], [], null, null);
        }

        if (IdentifierEquals(Keyword.Connection) || IdentifierEquals(Keyword.PgVersion))
        {
            var keyword = Advance();
            return BuildPgKeywordAction([keyword], [ExpectPgNameOrString()], null, null);
        }

        if (IdentifierEquals(Keyword.PgRefresh))
        {
            var refresh = Advance();
            return IdentifierEquals(Keyword.PgVersion)
                ? BuildPgKeywordAction([refresh, Advance()], [], null, null)
                : ParsePgKeywordActionWith([refresh, ExpectIdent(Keyword.PgPublication)], []);
        }

        return TryParsePgFunctionCommand();
    }

    private PgKeywordAction? TryParsePgFunctionCommand()
    {
        var keywords = new List<SyntaxToken>();
        if (IdentifierEquals(Keyword.No))
        {
            keywords.Add(Advance());
        }

        if (IdentifierEquals(Keyword.Handler) || IdentifierEquals(Keyword.PgValidator))
        {
            keywords.Add(Advance());
            return BuildPgKeywordAction(keywords, keywords.Count == 1 ? ParseQualifiedName() : [], null, null);
        }

        if (IdentifierEquals(Keyword.PgDepends))
        {
            keywords.Add(Advance());
            keywords.Add(Expect(SyntaxKind.OnKeyword));
            keywords.Add(ExpectIdent(Keyword.PgExtension));
            return BuildPgKeywordAction(keywords, [Expect(SyntaxKind.Identifier)], null, null);
        }

        if (keywords.Count > 0)
        {
            throw new SqlParseException($"Expected HANDLER, VALIDATOR, or DEPENDS, found {_current.Kind}", _current.Position);
        }

        return null;
    }
}
