namespace QlParse;

// Postgres CREATE TYPE (shell, base, ENUM, RANGE), the CREATE DOMAIN NOT NULL / NULL constraints,
// and the CREATE CAST conversion forms ISO lacks (WITHOUT FUNCTION, WITH INOUT, argument types).
internal sealed partial class Parser
{
    // CREATE TYPE name | CREATE TYPE name (INPUT = f, ...) | CREATE TYPE name AS ENUM ('a', ...)
    // | CREATE TYPE name AS RANGE (SUBTYPE = t, ...)
    private PgCreateTypeStatement ParsePgCreateType()
    {
        var createKeyword = Advance();
        var typeKeyword = Advance();
        var name = ParseQualifiedName();
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            var baseDefinition = ParsePgDefinitionList();
            return new PgCreateTypeStatement
            {
                Span = SourceSpan.From(createKeyword, baseDefinition.Span),
                CreateKeyword = createKeyword,
                TypeKeyword = typeKeyword,
                Name = name,
                Definition = baseDefinition,
            };
        }

        if (_current.Kind != SyntaxKind.AsKeyword)
        {
            return new PgCreateTypeStatement
            {
                Span = SourceSpan.From(createKeyword, name[^1]),
                CreateKeyword = createKeyword,
                TypeKeyword = typeKeyword,
                Name = name,
            };
        }

        var asKeyword = Advance();
        var kindKeyword = Advance();
        return TokenEquals(kindKeyword, Keyword.PgEnum)
            ? ParsePgEnumTypeTail(createKeyword, typeKeyword, name, asKeyword, kindKeyword)
            : ParsePgRangeTypeTail(createKeyword, typeKeyword, name, asKeyword, kindKeyword);
    }

    private PgCreateTypeStatement ParsePgEnumTypeTail(
        SyntaxToken createKeyword,
        SyntaxToken typeKeyword,
        IReadOnlyList<SyntaxToken> name,
        SyntaxToken asKeyword,
        SyntaxToken kindKeyword)
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        IReadOnlyList<SyntaxToken> labels = _current.Kind == SyntaxKind.CloseParen ? [] : ParsePgStringList();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PgCreateTypeStatement
        {
            Span = SourceSpan.From(createKeyword, closeParen),
            CreateKeyword = createKeyword,
            TypeKeyword = typeKeyword,
            Name = name,
            AsKeyword = asKeyword,
            KindKeyword = kindKeyword,
            OpenParen = openParen,
            Labels = labels,
            CloseParen = closeParen,
        };
    }

    private PgCreateTypeStatement ParsePgRangeTypeTail(
        SyntaxToken createKeyword,
        SyntaxToken typeKeyword,
        IReadOnlyList<SyntaxToken> name,
        SyntaxToken asKeyword,
        SyntaxToken kindKeyword)
    {
        var definition = ParsePgDefinitionList();
        return new PgCreateTypeStatement
        {
            Span = SourceSpan.From(createKeyword, definition.Span),
            CreateKeyword = createKeyword,
            TypeKeyword = typeKeyword,
            Name = name,
            AsKeyword = asKeyword,
            KindKeyword = kindKeyword,
            Definition = definition,
        };
    }

    // [CONSTRAINT name] {NOT NULL | NULL}, reached from the ISO domain tail under the Postgres flag only.
    internal bool IsPgDomainNullConstraintStart()
    {
        if (!IsPostgres)
        {
            return false;
        }

        if (_current.Kind is SyntaxKind.NotKeyword or SyntaxKind.NullKeyword)
        {
            return true;
        }

        return IdentifierEquals(Keyword.Constraint)
            && _index + 1 < _tokens.Count
            && _tokens[_index + 1].Kind is SyntaxKind.NotKeyword or SyntaxKind.NullKeyword;
    }

    internal PgDomainNullConstraint ParsePgDomainNullConstraint()
    {
        var start = _current;
        SyntaxToken? constraintKeyword = null;
        SyntaxToken? constraintName = null;
        if (IdentifierEquals(Keyword.Constraint))
        {
            constraintKeyword = Advance();
            constraintName = Expect(SyntaxKind.Identifier);
        }

        SyntaxToken? notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : null;
        var nullKeyword = Expect(SyntaxKind.NullKeyword);
        return new PgDomainNullConstraint
        {
            Span = SourceSpan.From(start, nullKeyword),
            ConstraintKeyword = constraintKeyword,
            ConstraintName = constraintName,
            NotKeyword = notKeyword,
            NullKeyword = nullKeyword,
        };
    }

    // WITHOUT FUNCTION | WITH INOUT | WITH FUNCTION f [(argument types)]
    internal CastConversion ParsePgCastConversion(SyntaxToken withKeyword)
    {
        if (TokenEquals(withKeyword, Keyword.Without))
        {
            var functionKeyword = ExpectIdent(Keyword.Function);
            return new CastConversion(null, functionKeyword, null, null, [], null, functionKeyword.Span);
        }

        if (IdentifierEquals(Keyword.Inout))
        {
            var inout = Advance();
            return new CastConversion(null, null, inout, null, [], null, inout.Span);
        }

        var routine = ParseRoutineDesignator();
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new CastConversion(routine, null, null, null, [], null, routine.Span);
        }

        var openParen = Advance();
        IReadOnlyList<PgRoutineArgument> arguments = _current.Kind == SyntaxKind.CloseParen ? [] : ParsePgRoutineArguments();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new CastConversion(routine, null, null, openParen, arguments, closeParen, closeParen.Span);
    }

    // CREATE COLLATION [IF NOT EXISTS] name {(LOCALE = ..., PROVIDER = ..., ...) | FROM existing}
    private PgCreateCollationStatement ParsePgCreateCollation()
    {
        var createKeyword = Advance();
        var collationKeyword = Advance();
        var ifNotExists = ParsePgIfNotExists();
        var name = ParseQualifiedName();
        PgDefinitionList? definition = null;
        SyntaxToken? fromKeyword = null;
        IReadOnlyList<SyntaxToken>? source = null;
        SourceSpan end;
        if (_current.Kind == SyntaxKind.FromKeyword)
        {
            fromKeyword = Advance();
            var sourceName = ParseQualifiedName();
            source = sourceName;
            end = sourceName[^1].Span;
        }
        else
        {
            var list = ParsePgDefinitionList();
            definition = list;
            end = list.Span;
        }

        return new PgCreateCollationStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            CollationKeyword = collationKeyword,
            IfKeyword = ifNotExists.IfKeyword,
            NotKeyword = ifNotExists.NotKeyword,
            ExistsKeyword = ifNotExists.ExistsKeyword,
            Name = name,
            Definition = definition,
            FromKeyword = fromKeyword,
            Source = source,
        };
    }

    // CREATE [DEFAULT] CONVERSION name FOR 'source' TO 'target' FROM function
    private PgCreateConversionStatement ParsePgCreateConversion()
    {
        var createKeyword = Advance();
        SyntaxToken? defaultKeyword = IdentifierEquals(Keyword.Default) ? Advance() : null;
        var conversionKeyword = ExpectIdent(Keyword.PgConversion);
        var name = ParseQualifiedName();
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var sourceEncoding = Expect(SyntaxKind.String);
        var toKeyword = ExpectIdent(Keyword.To);
        var targetEncoding = Expect(SyntaxKind.String);
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var function = ParseQualifiedName();
        return new PgCreateConversionStatement
        {
            Span = SourceSpan.From(createKeyword, function[^1]),
            CreateKeyword = createKeyword,
            DefaultKeyword = defaultKeyword,
            ConversionKeyword = conversionKeyword,
            Name = name,
            ForKeyword = forKeyword,
            SourceEncoding = sourceEncoding,
            ToKeyword = toKeyword,
            TargetEncoding = targetEncoding,
            FromKeyword = fromKeyword,
            Function = function,
        };
    }

    // CREATE ACCESS METHOD name TYPE {TABLE | INDEX} HANDLER function
    private PgCreateAccessMethodStatement ParsePgCreateAccessMethod()
    {
        var createKeyword = Advance();
        var accessKeyword = Advance();
        var methodKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var typeKeyword = ExpectIdent(Keyword.Type);
        if (!IdentifierEquals(Keyword.Table) && !IdentifierEquals(Keyword.Index))
        {
            throw new SqlParseException($"Expected TABLE or INDEX, found {_current.Kind}", _current.Position);
        }

        var kind = Advance();
        var handlerKeyword = ExpectIdent(Keyword.Handler);
        var handler = ParseQualifiedName();
        return new PgCreateAccessMethodStatement
        {
            Span = SourceSpan.From(createKeyword, handler[^1]),
            CreateKeyword = createKeyword,
            AccessKeyword = accessKeyword,
            MethodKeyword = methodKeyword,
            Name = name,
            TypeKeyword = typeKeyword,
            Kind = kind,
            HandlerKeyword = handlerKeyword,
            Handler = handler,
        };
    }

    // CREATE TEXT SEARCH {CONFIGURATION | DICTIONARY | PARSER | TEMPLATE} name (definition)
    private PgCreateTextSearchStatement ParsePgCreateTextSearch()
    {
        var createKeyword = Advance();
        var textKeyword = Advance();
        var searchKeyword = Advance();
        if (!IdentifierEquals(Keyword.PgConfiguration) && !IdentifierEquals(Keyword.PgDictionary)
            && !IdentifierEquals(Keyword.PgParser) && !IdentifierEquals(Keyword.PgTemplate))
        {
            throw new SqlParseException($"Expected CONFIGURATION, DICTIONARY, PARSER, or TEMPLATE, found {_current.Kind}", _current.Position);
        }

        var kindKeyword = Advance();
        var name = ParseQualifiedName();
        var definition = ParsePgDefinitionList();
        return new PgCreateTextSearchStatement
        {
            Span = SourceSpan.From(createKeyword, definition.Span),
            CreateKeyword = createKeyword,
            TextKeyword = textKeyword,
            SearchKeyword = searchKeyword,
            KindKeyword = kindKeyword,
            Name = name,
            Definition = definition,
        };
    }
}
