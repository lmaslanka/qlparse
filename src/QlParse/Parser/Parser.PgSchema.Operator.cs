namespace QlParse;

// Postgres CREATE OPERATOR, CREATE OPERATOR CLASS / FAMILY (and their member items), and CREATE AGGREGATE.
internal sealed partial class Parser
{
    private Query ParsePgCreateOperatorAny()
    {
        var kindIndex = _index + 1;
        return IsKeywordAt(kindIndex, Keyword.PgClass) || IsKeywordAt(kindIndex, Keyword.PgFamily)
            ? ParsePgCreateOperatorClass()
            : ParsePgCreateOperator();
    }

    // CREATE OPERATOR [schema.]op (FUNCTION = f, LEFTARG = t, RIGHTARG = t, COMMUTATOR = op, ...)
    private PgCreateOperatorStatement ParsePgCreateOperator()
    {
        var createKeyword = Advance();
        var operatorKeyword = Advance();
        var name = ParsePgOperatorName();
        var definition = ParsePgDefinitionList();
        return new PgCreateOperatorStatement
        {
            Span = SourceSpan.From(createKeyword, definition.Span),
            CreateKeyword = createKeyword,
            OperatorKeyword = operatorKeyword,
            Name = name,
            Definition = definition,
        };
    }

    private readonly record struct PgOperatorClassFor(
        SyntaxToken? DefaultKeyword,
        SyntaxToken? ForKeyword,
        SyntaxToken? TypeKeyword,
        DataType? Type);

    // CREATE OPERATOR CLASS name [DEFAULT] FOR TYPE t USING am [FAMILY f] AS item, ...
    // CREATE OPERATOR FAMILY name USING am
    private PgCreateOperatorClassStatement ParsePgCreateOperatorClass()
    {
        var createKeyword = Advance();
        var operatorKeyword = Advance();
        var kindKeyword = Advance();
        var isClass = TokenEquals(kindKeyword, Keyword.PgClass);
        var name = ParseQualifiedName();
        var forType = isClass ? ParsePgOperatorClassFor() : default;
        var usingKeyword = Expect(SyntaxKind.UsingKeyword);
        var accessMethod = Expect(SyntaxKind.Identifier);
        SyntaxToken? familyKeyword = isClass && IdentifierEquals(Keyword.PgFamily) ? Advance() : null;
        IReadOnlyList<SyntaxToken>? family = familyKeyword is null ? null : ParseQualifiedName();
        SyntaxToken? asKeyword = isClass ? Expect(SyntaxKind.AsKeyword) : null;
        IReadOnlyList<PgOperatorClassItem> items = isClass ? ParsePgOperatorClassItems() : [];
        return new PgCreateOperatorClassStatement
        {
            Span = SourceSpan.From(createKeyword, items.Count > 0 ? items[^1].Span : accessMethod.Span),
            CreateKeyword = createKeyword,
            OperatorKeyword = operatorKeyword,
            KindKeyword = kindKeyword,
            Name = name,
            DefaultKeyword = forType.DefaultKeyword,
            ForKeyword = forType.ForKeyword,
            TypeKeyword = forType.TypeKeyword,
            Type = forType.Type,
            UsingKeyword = usingKeyword,
            AccessMethod = accessMethod,
            FamilyKeyword = familyKeyword,
            Family = family,
            AsKeyword = asKeyword,
            Items = items,
        };
    }

    private PgOperatorClassFor ParsePgOperatorClassFor()
    {
        SyntaxToken? defaultKeyword = IdentifierEquals(Keyword.Default) ? Advance() : null;
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var typeKeyword = ExpectIdent(Keyword.Type);
        return new PgOperatorClassFor(defaultKeyword, forKeyword, typeKeyword, ParseDataType());
    }

    private List<PgOperatorClassItem> ParsePgOperatorClassItems()
    {
        var items = new List<PgOperatorClassItem> { ParsePgOperatorClassItem(true) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParsePgOperatorClassItem(true));
        }

        return items;
    }

    // OPERATOR n [op] [(t, t)] [FOR SEARCH | FOR ORDER BY family] | FUNCTION n [(t, t)] [f(args)] | STORAGE t.
    // The operator and function are absent in ALTER OPERATOR FAMILY ... DROP.
    private PgOperatorClassItem ParsePgOperatorClassItem(bool isAdd)
    {
        if (IdentifierEquals(Keyword.PgStorage))
        {
            var storageKeyword = Advance();
            var storageType = ParseDataType();
            return new PgOperatorClassItem
            {
                Span = SourceSpan.From(storageKeyword, storageType.Span),
                Kind = storageKeyword,
                StorageType = storageType,
            };
        }

        if (IdentifierEquals(Keyword.Operator))
        {
            return ParsePgOperatorItem(isAdd);
        }

        if (!IdentifierEquals(Keyword.Function))
        {
            throw new SqlParseException($"Expected OPERATOR, FUNCTION, or STORAGE, found {_current.Kind}", _current.Position);
        }

        var functionKeyword = Advance();
        var number = Expect(SyntaxKind.Number);
        var types = ParsePgOptionalItemTypes();
        PgObjectReference? function = isAdd ? ParsePgRoutineReference() : null;
        var end = function?.Span ?? types.CloseParen?.Span ?? number.Span;
        return new PgOperatorClassItem
        {
            Span = SourceSpan.From(functionKeyword, end),
            Kind = functionKeyword,
            Number = number,
            OpenParen = types.OpenParen,
            Types = types.Types,
            CloseParen = types.CloseParen,
            Function = function,
        };
    }

    private PgOperatorClassItem ParsePgOperatorItem(bool isAdd)
    {
        var operatorKeyword = Advance();
        var number = Expect(SyntaxKind.Number);
        PgOperatorName? operatorName = isAdd ? ParsePgOperatorName() : null;
        var types = ParsePgOptionalItemTypes();
        SyntaxToken? forKeyword = null;
        SyntaxToken? purpose = null;
        SyntaxToken? byKeyword = null;
        IReadOnlyList<SyntaxToken>? sortFamily = null;
        if (_current.Kind == SyntaxKind.ForKeyword)
        {
            forKeyword = Advance();
            if (_current.Kind == SyntaxKind.SearchKeyword)
            {
                purpose = Advance();
            }
            else
            {
                purpose = Expect(SyntaxKind.OrderKeyword);
                byKeyword = Expect(SyntaxKind.ByKeyword);
                sortFamily = ParseQualifiedName();
            }
        }

        var end = sortFamily?[^1].Span ?? purpose?.Span ?? types.CloseParen?.Span ?? operatorName?.Span ?? number.Span;
        return new PgOperatorClassItem
        {
            Span = SourceSpan.From(operatorKeyword, end),
            Kind = operatorKeyword,
            Number = number,
            Operator = operatorName,
            OpenParen = types.OpenParen,
            Types = types.Types,
            CloseParen = types.CloseParen,
            ForKeyword = forKeyword,
            Purpose = purpose,
            ByKeyword = byKeyword,
            SortFamily = sortFamily,
        };
    }

    private readonly record struct PgItemTypes(SyntaxToken? OpenParen, IReadOnlyList<DataType> Types, SyntaxToken? CloseParen);

    private PgItemTypes ParsePgOptionalItemTypes()
    {
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new PgItemTypes(null, [], null);
        }

        var openParen = Advance();
        var types = new List<DataType> { ParseDataType() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            types.Add(ParseDataType());
        }

        return new PgItemTypes(openParen, types, Expect(SyntaxKind.CloseParen));
    }

    // CREATE [OR REPLACE] AGGREGATE name (args | * | [args] ORDER BY args) (SFUNC = f, STYPE = t, ...)
    // or the pre-8.2 form CREATE AGGREGATE name (BASETYPE = t, SFUNC = f, STYPE = t, ...).
    private PgCreateAggregateStatement ParsePgCreateAggregate()
    {
        var createKeyword = Advance();
        SyntaxToken? orKeyword = null;
        SyntaxToken? replaceKeyword = null;
        if (_current.Kind == SyntaxKind.OrKeyword)
        {
            orKeyword = Advance();
            replaceKeyword = Advance();
        }

        var aggregateKeyword = ExpectIdent(Keyword.PgAggregate);
        var signature = IsPgOldAggregateSyntax() ? ParsePgNameReference() : ParsePgRoutineReference();
        var definition = ParsePgDefinitionList();
        return new PgCreateAggregateStatement
        {
            Span = SourceSpan.From(createKeyword, definition.Span),
            CreateKeyword = createKeyword,
            OrKeyword = orKeyword,
            ReplaceKeyword = replaceKeyword,
            AggregateKeyword = aggregateKeyword,
            Signature = signature,
            Definition = definition,
        };
    }

    // The old syntax opens its only parenthesis with `name =`.
    private bool IsPgOldAggregateSyntax()
    {
        var index = SkipQualifiedNameAt(_index - 1);
        return index + Len2 < _tokens.Count
            && _tokens[index].Kind == SyntaxKind.OpenParen
            && IsPgWordKind(_tokens[index + 1].Kind)
            && _tokens[index + Len2].Kind == SyntaxKind.EqualsToken;
    }
}
