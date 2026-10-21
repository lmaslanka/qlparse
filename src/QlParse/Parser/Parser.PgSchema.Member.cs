namespace QlParse;

// ADD / DROP / ALTER actions of the generic Postgres ALTER statement: enum values and attributes
// (ALTER TYPE), members (ALTER EXTENSION, ALTER OPERATOR FAMILY), publication tables and
// subscription publications, and text search configuration mappings.
internal sealed partial class Parser
{
    private PgAlterAction? TryParsePgMemberChange(PgAlterContext context)
    {
        var isAdd = IdentifierEquals(Keyword.Add);
        if (!isAdd && !IdentifierEquals(Keyword.Drop) && !IdentifierEquals(Keyword.Alter))
        {
            return null;
        }

        if (context == PgAlterContext.Extension)
        {
            return ParsePgExtensionMemberAction();
        }

        if (context == PgAlterContext.OperatorFamily)
        {
            return ParsePgOperatorFamilyAction(isAdd);
        }

        return context == PgAlterContext.ForeignTable ? ParsePgForeignTableAction() : ParsePgGeneralMemberChange(isAdd);
    }

    private PgAlterAction ParsePgGeneralMemberChange(bool isAdd)
    {
        if (isAdd && NextEquals(Keyword.Value))
        {
            return ParsePgAddEnumValue();
        }

        if (NextEquals(Keyword.PgAttribute))
        {
            return ParsePgAttributeAction();
        }

        if (NextEquals(Keyword.PgMapping))
        {
            return ParsePgTextSearchMappingAction();
        }

        if (NextEquals(Keyword.PgPublication))
        {
            return ParsePgPublicationListAction();
        }

        if (NextEquals(Keyword.Table) || NextEquals(Keyword.Tables))
        {
            return ParsePgPublicationObjectsAction();
        }

        throw new SqlParseException($"Unexpected {_current.Kind} in ALTER action", _current.Position);
    }

    // ADD VALUE [IF NOT EXISTS] 'v' [{BEFORE | AFTER} 'x']
    private PgAddEnumValueAction ParsePgAddEnumValue()
    {
        var addKeyword = Advance();
        var valueKeyword = Advance();
        var ifNotExists = ParsePgIfNotExists();
        var value = Expect(SyntaxKind.String);
        SyntaxToken? placement = null;
        SyntaxToken? neighbor = null;
        if (IdentifierEquals(Keyword.Before) || IdentifierEquals(Keyword.After))
        {
            placement = Advance();
            neighbor = Expect(SyntaxKind.String);
        }

        return new PgAddEnumValueAction
        {
            Span = SourceSpan.From(addKeyword, neighbor ?? value),
            AddKeyword = addKeyword,
            ValueKeyword = valueKeyword,
            IfKeyword = ifNotExists.IfKeyword,
            NotKeyword = ifNotExists.NotKeyword,
            ExistsKeyword = ifNotExists.ExistsKeyword,
            Value = value,
            Placement = placement,
            Neighbor = neighbor,
        };
    }

    private readonly record struct PgAttributeType(
        SyntaxToken? SetKeyword,
        SyntaxToken? DataKeyword,
        SyntaxToken? TypeKeyword,
        DataType? Type,
        SyntaxToken? CollateKeyword,
        IReadOnlyList<SyntaxToken>? Collation);

    // ADD ATTRIBUTE a t [COLLATE c] | DROP ATTRIBUTE [IF EXISTS] a | ALTER ATTRIBUTE a [SET DATA] TYPE t [COLLATE c],
    // each followed by an optional CASCADE | RESTRICT.
    private PgAttributeAction ParsePgAttributeAction()
    {
        var action = Advance();
        var attributeKeyword = Advance();
        var ifExists = TokenEquals(action, Keyword.Drop) ? ParsePgIfExists() : default;
        var name = Expect(SyntaxKind.Identifier);
        var type = TokenEquals(action, Keyword.Drop) ? default : ParsePgAttributeType(TokenEquals(action, Keyword.Alter));
        var behavior = ParsePgOptionalBehavior();
        var end = behavior?.Span ?? type.Collation?[^1].Span ?? type.Type?.Span ?? name.Span;
        return new PgAttributeAction
        {
            Span = SourceSpan.From(action, end),
            Action = action,
            AttributeKeyword = attributeKeyword,
            IfKeyword = ifExists.IfKeyword,
            ExistsKeyword = ifExists.ExistsKeyword,
            Name = name,
            SetKeyword = type.SetKeyword,
            DataKeyword = type.DataKeyword,
            TypeKeyword = type.TypeKeyword,
            Type = type.Type,
            CollateKeyword = type.CollateKeyword,
            Collation = type.Collation,
            Behavior = behavior,
        };
    }

    private PgAttributeType ParsePgAttributeType(bool isAlter)
    {
        SyntaxToken? setKeyword = null;
        SyntaxToken? dataKeyword = null;
        SyntaxToken? typeKeyword = null;
        if (isAlter)
        {
            if (_current.Kind == SyntaxKind.SetKeyword)
            {
                setKeyword = Advance();
                dataKeyword = ExpectIdent(Keyword.Data);
            }

            typeKeyword = ExpectIdent(Keyword.Type);
        }

        var type = ParseDataType();
        SyntaxToken? collateKeyword = null;
        IReadOnlyList<SyntaxToken>? collation = null;
        if (_current.Kind == SyntaxKind.CollateKeyword)
        {
            collateKeyword = Advance();
            collation = ParseQualifiedName();
        }

        return new PgAttributeType(setKeyword, dataKeyword, typeKeyword, type, collateKeyword, collation);
    }

    // ALTER EXTENSION e {ADD | DROP} <object kind> <object>
    private PgMemberAction ParsePgExtensionMemberAction()
    {
        var action = Advance();
        var kind = MatchPgMemberKind(_index - 1);
        if (kind.Shape == PgObjectShape.None)
        {
            throw new SqlParseException($"Expected extension member kind, found {_current.Kind}", _current.Position);
        }

        var objectKind = ParsePgObjectKindTokens(kind.Count);
        var member = ParsePgObjectReference(kind.Shape);
        return new PgMemberAction
        {
            Span = SourceSpan.From(action, member.Span),
            Action = action,
            ObjectKind = objectKind,
            Member = member,
        };
    }

    // ALTER OPERATOR FAMILY f USING am {ADD | DROP} {OPERATOR ... | FUNCTION ...} [, ...]
    private PgOperatorFamilyAction ParsePgOperatorFamilyAction(bool isAdd)
    {
        var action = Advance();
        var items = new List<PgOperatorClassItem> { ParsePgOperatorClassItem(isAdd) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParsePgOperatorClassItem(isAdd));
        }

        return new PgOperatorFamilyAction
        {
            Span = SourceSpan.From(action, items[^1].Span),
            Action = action,
            Items = items,
        };
    }

    // {ADD | SET | DROP} {TABLE [ONLY] t [*] [(cols)] [WHERE (e)] | TABLES IN SCHEMA s} [, ...]
    private PgPublicationObjectsAction ParsePgPublicationObjectsAction()
    {
        var action = Advance();
        var objects = ParsePgPublicationObjects();
        return new PgPublicationObjectsAction
        {
            Span = SourceSpan.From(action, objects[^1].Span),
            Action = action,
            Objects = objects,
        };
    }

    // {ADD | ALTER} MAPPING FOR tt [, ...] WITH d [, ...] | ALTER MAPPING [FOR tt [, ...]] REPLACE old WITH new
    // | DROP MAPPING [IF EXISTS] FOR tt [, ...]
    private PgTextSearchMappingAction ParsePgTextSearchMappingAction()
    {
        var action = Advance();
        var mappingKeyword = Advance();
        var ifExists = TokenEquals(action, Keyword.Drop) ? ParsePgIfExists() : default;
        SyntaxToken? forKeyword = null;
        IReadOnlyList<SyntaxToken> tokenTypes = [];
        if (_current.Kind == SyntaxKind.ForKeyword)
        {
            forKeyword = Advance();
            tokenTypes = ParseNameList();
        }

        SyntaxToken? replaceKeyword = null;
        IReadOnlyList<SyntaxToken>? oldDictionary = null;
        if (IdentifierEquals(Keyword.PgReplace))
        {
            replaceKeyword = Advance();
            oldDictionary = ParseQualifiedName();
        }

        SyntaxToken? withKeyword = null;
        IReadOnlyList<IReadOnlyList<SyntaxToken>> dictionaries = [];
        if (_current.Kind == SyntaxKind.WithKeyword)
        {
            withKeyword = Advance();
            dictionaries = ParseSchemaNameList();
        }

        var end = tokenTypes.Count > 0 ? tokenTypes[^1].Span : mappingKeyword.Span;
        if (dictionaries.Count > 0)
        {
            end = dictionaries[^1][^1].Span;
        }

        return new PgTextSearchMappingAction
        {
            Span = SourceSpan.From(action, end),
            Action = action,
            MappingKeyword = mappingKeyword,
            IfKeyword = ifExists.IfKeyword,
            ExistsKeyword = ifExists.ExistsKeyword,
            ForKeyword = forKeyword,
            TokenTypes = tokenTypes,
            ReplaceKeyword = replaceKeyword,
            OldDictionary = oldDictionary,
            WithKeyword = withKeyword,
            Dictionaries = dictionaries,
        };
    }
}
