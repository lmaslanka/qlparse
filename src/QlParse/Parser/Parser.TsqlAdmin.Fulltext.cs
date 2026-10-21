namespace QlParse;

// Workstream G: full-text catalogs, indexes, stoplists and search property lists.
internal sealed partial class Parser
{
    private Query? TryParseTsqlFulltextCreate()
    {
        if (NextEquals(Keyword.AdminFulltext))
        {
            if (IsKeywordAt(_index + 1, Keyword.Catalog))
            {
                return ParseTsqlCreateFulltextCatalog();
            }

            if (IsKeywordAt(_index + 1, Keyword.Index))
            {
                return ParseTsqlCreateFulltextIndex();
            }

            return IsKeywordAt(_index + 1, Keyword.AdminStoplist) ? ParseTsqlCreateFulltextStoplist() : null;
        }

        return IsTsqlSearchPropertyList() ? ParseTsqlCreateSearchPropertyList() : null;
    }

    private Query? TryParseTsqlFulltextAlter()
    {
        if (NextEquals(Keyword.AdminFulltext))
        {
            if (IsKeywordAt(_index + 1, Keyword.Catalog))
            {
                return ParseTsqlAlterFulltextCatalog();
            }

            return IsKeywordAt(_index + 1, Keyword.AdminStoplist) ? ParseTsqlAlterFulltextStoplist() : null;
        }

        return IsTsqlSearchPropertyList() ? ParseTsqlAlterSearchPropertyList() : null;
    }

    private bool IsTsqlSearchPropertyList() =>
        NextEquals(Keyword.Search) && IsKeywordAt(_index + 1, Keyword.Property) && IsKeywordAt(_index + 2, Keyword.AdminList);

    // CREATE FULLTEXT CATALOG name [ON FILEGROUP fg] [IN PATH 'path'] [WITH ACCENT_SENSITIVITY = {ON | OFF}]
    //     [AS DEFAULT] [AUTHORIZATION owner]
    private TsqlCreateFulltextCatalogStatement ParseTsqlCreateFulltextCatalog()
    {
        var createKeyword = Advance();
        var fulltextKeyword = Advance();
        var catalogKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        SyntaxToken? onKeyword = null;
        SyntaxToken? fileGroupKeyword = null;
        SyntaxToken? fileGroup = null;
        if (_current.Kind == SyntaxKind.OnKeyword)
        {
            onKeyword = Advance();
            fileGroupKeyword = ExpectIdent(Keyword.AdminFileGroup);
            fileGroup = Expect(SyntaxKind.Identifier);
        }

        SyntaxToken? inKeyword = null;
        SyntaxToken? pathKeyword = null;
        SyntaxToken? path = null;
        if (_current.Kind == SyntaxKind.InKeyword)
        {
            inKeyword = Advance();
            pathKeyword = ExpectIdent(Keyword.Path);
            path = Expect(SyntaxKind.String);
        }

        var options = ParseTsqlOptionalWith();
        var (asKeyword, defaultKeyword) = ParseTsqlAsDefault();
        var (authorizationKeyword, owner) = ParseTsqlAuthorization();
        var end = owner?.Span ?? defaultKeyword?.Span ?? options.Options?.Span ?? path?.Span ?? fileGroup?.Span ?? name.Span;
        return new TsqlCreateFulltextCatalogStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            FulltextKeyword = fulltextKeyword,
            CatalogKeyword = catalogKeyword,
            Name = name,
            OnKeyword = onKeyword,
            FileGroupKeyword = fileGroupKeyword,
            FileGroup = fileGroup,
            InKeyword = inKeyword,
            PathKeyword = pathKeyword,
            Path = path,
            WithKeyword = options.WithKeyword,
            Options = options.Options,
            AsKeyword = asKeyword,
            DefaultKeyword = defaultKeyword,
            AuthorizationKeyword = authorizationKeyword,
            Owner = owner,
        };
    }

    // [AS DEFAULT]
    private (SyntaxToken? AsKeyword, SyntaxToken? DefaultKeyword) ParseTsqlAsDefault()
    {
        if (_current.Kind != SyntaxKind.AsKeyword || !NextEquals(Keyword.Default))
        {
            return (null, null);
        }

        var asKeyword = Advance();
        return (asKeyword, Advance());
    }

    // ALTER FULLTEXT CATALOG name {REBUILD [WITH ACCENT_SENSITIVITY = {ON | OFF}] | REORGANIZE | AS DEFAULT}
    private TsqlAlterFulltextCatalogStatement ParseTsqlAlterFulltextCatalog()
    {
        var alterKeyword = Advance();
        var fulltextKeyword = Advance();
        var catalogKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        List<SyntaxToken> action;
        if (_current.Kind == SyntaxKind.AsKeyword && NextEquals(Keyword.Default))
        {
            action = [Advance(), Advance()];
        }
        else
        {
            action = IdentifierEquals(Keyword.Rebuild) || IdentifierEquals(Keyword.Reorganize)
                ? [Advance()]
                : throw new SqlParseException($"Expected REBUILD, REORGANIZE or AS DEFAULT, found {_current.Kind}", _current.Position);
        }

        var options = ParseTsqlOptionalWith();
        return new TsqlAlterFulltextCatalogStatement
        {
            Span = SourceSpan.From(alterKeyword, options.Options?.Span ?? action[^1].Span),
            AlterKeyword = alterKeyword,
            FulltextKeyword = fulltextKeyword,
            CatalogKeyword = catalogKeyword,
            Name = name,
            Action = action,
            WithKeyword = options.WithKeyword,
            Options = options.Options,
        };
    }

    // CREATE FULLTEXT INDEX ON table [(column, ...)] KEY INDEX index [ON {catalog | (catalog, FILEGROUP fg)}]
    //     [WITH [(] option, ... [)]]
    private TsqlCreateFulltextIndexStatement ParseTsqlCreateFulltextIndex()
    {
        var createKeyword = Advance();
        var fulltextKeyword = Advance();
        var indexKeyword = Advance();
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var table = ParseQualifiedName();
        SyntaxToken? openParen = null;
        SyntaxToken? closeParen = null;
        var columns = new List<TsqlFulltextIndexColumn>();
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            openParen = Advance();
            columns.Add(ParseTsqlFulltextIndexColumn());
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                columns.Add(ParseTsqlFulltextIndexColumn());
            }

            closeParen = Expect(SyntaxKind.CloseParen);
        }

        var keyKeyword = ExpectIdent(Keyword.Key);
        var keyIndexKeyword = ExpectIdent(Keyword.Index);
        var keyIndex = Expect(SyntaxKind.Identifier);
        SyntaxToken? catalogOnKeyword = null;
        TsqlOptionList? storage = null;
        if (_current.Kind == SyntaxKind.OnKeyword)
        {
            catalogOnKeyword = Advance();
            storage = _current.Kind == SyntaxKind.OpenParen ? ParseTsqlOptionList() : ParseTsqlFulltextCatalogName();
        }

        SyntaxToken? withKeyword = null;
        TsqlOptionList? options = null;
        if (_current.Kind == SyntaxKind.WithKeyword)
        {
            withKeyword = Advance();
            options = _current.Kind == SyntaxKind.OpenParen ? ParseTsqlOptionList() : ParseTsqlFulltextIndexOptions();
        }

        return new TsqlCreateFulltextIndexStatement
        {
            Span = SourceSpan.From(createKeyword, options?.Span ?? storage?.Span ?? keyIndex.Span),
            CreateKeyword = createKeyword,
            FulltextKeyword = fulltextKeyword,
            IndexKeyword = indexKeyword,
            OnKeyword = onKeyword,
            Table = table,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = closeParen,
            KeyKeyword = keyKeyword,
            KeyIndexKeyword = keyIndexKeyword,
            KeyIndex = keyIndex,
            CatalogOnKeyword = catalogOnKeyword,
            Storage = storage,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // `ON catalog` without parentheses: a one-item list naming the catalog.
    private TsqlOptionList ParseTsqlFulltextCatalogName()
    {
        var catalog = Expect(SyntaxKind.Identifier);
        var option = new TsqlOption { Span = catalog.Span, Name = [catalog] };
        return new TsqlOptionList { Span = catalog.Span, Options = [option] };
    }

    // column [TYPE COLUMN type_column] [LANGUAGE language] [STATISTICAL_SEMANTICS]
    private TsqlFulltextIndexColumn ParseTsqlFulltextIndexColumn()
    {
        var name = Expect(SyntaxKind.Identifier);
        SyntaxToken? typeKeyword = null;
        SyntaxToken? columnKeyword = null;
        SyntaxToken? typeColumn = null;
        if (IdentifierEquals(Keyword.Type))
        {
            typeKeyword = Advance();
            columnKeyword = ExpectIdent(Keyword.Column);
            typeColumn = Expect(SyntaxKind.Identifier);
        }

        SyntaxToken? languageKeyword = null;
        SyntaxToken? language = null;
        if (IdentifierEquals(Keyword.Language))
        {
            languageKeyword = Advance();
            language = ExpectTsqlLanguage();
        }

        var semantics = IdentifierEquals(Keyword.AdminStatisticalSemantics) ? Advance() : (SyntaxToken?)null;
        return new TsqlFulltextIndexColumn
        {
            Span = SourceSpan.From(name, semantics?.Span ?? language?.Span ?? typeColumn?.Span ?? name.Span),
            Name = name,
            TypeKeyword = typeKeyword,
            ColumnKeyword = columnKeyword,
            TypeColumn = typeColumn,
            LanguageKeyword = languageKeyword,
            Language = language,
            StatisticalSemanticsKeyword = semantics,
        };
    }

    // A language: an LCID number, a 0x hex LCID, a name string or an alias.
    private SyntaxToken ExpectTsqlLanguage() =>
        _current.Kind is SyntaxKind.Number or SyntaxKind.String or SyntaxKind.BinaryLiteral or SyntaxKind.Identifier
            ? Advance()
            : throw new SqlParseException($"Expected language, found {_current.Kind}", _current.Position);

    // The unparenthesised WITH list of CREATE FULLTEXT INDEX: CHANGE_TRACKING [=] {MANUAL | AUTO | OFF},
    // NO POPULATION, STOPLIST [=] {OFF | SYSTEM | name}, SEARCH PROPERTY LIST [=] name.
    private TsqlOptionList ParseTsqlFulltextIndexOptions()
    {
        var options = new List<TsqlOption> { ParseTsqlFulltextIndexOption() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            options.Add(ParseTsqlFulltextIndexOption());
        }

        return new TsqlOptionList { Span = SourceSpan.From(options[0].Span, options[^1].Span), Options = options };
    }

    private TsqlOption ParseTsqlFulltextIndexOption()
    {
        if (IdentifierEquals(Keyword.Search))
        {
            List<SyntaxToken> words = [Advance(), ExpectIdent(Keyword.Property), ExpectIdent(Keyword.AdminList)];
            return ParseTsqlFulltextIndexOptionValue(words);
        }

        var name = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        if (TokenEquals(name[0], Keyword.No))
        {
            name.Add(ExpectIdent(Keyword.AdminPopulation));
            return new TsqlOption { Span = SourceSpan.From(name[0], name[1]), Name = name };
        }

        return ParseTsqlFulltextIndexOptionValue(name);
    }

    private TsqlOption ParseTsqlFulltextIndexOptionValue(List<SyntaxToken> name)
    {
        var equalsToken = _current.Kind == SyntaxKind.EqualsToken ? Advance() : (SyntaxToken?)null;
        var value = Expect(SyntaxKind.Identifier);
        return new TsqlOption
        {
            Span = SourceSpan.From(name[0], value),
            Name = name,
            EqualsToken = equalsToken,
            Value = [value],
        };
    }

    // CREATE FULLTEXT STOPLIST name [FROM {[db.]source_stoplist | SYSTEM STOPLIST}] [AUTHORIZATION owner]
    private TsqlCreateFulltextStoplistStatement ParseTsqlCreateFulltextStoplist()
    {
        var createKeyword = Advance();
        var fulltextKeyword = Advance();
        var stoplistKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        SyntaxToken? fromKeyword = null;
        IReadOnlyList<SyntaxToken> source = [];
        SyntaxToken? systemKeyword = null;
        SyntaxToken? systemStoplistKeyword = null;
        if (_current.Kind == SyntaxKind.FromKeyword)
        {
            fromKeyword = Advance();
            if (IdentifierEquals(Keyword.System) && NextEquals(Keyword.AdminStoplist))
            {
                systemKeyword = Advance();
                systemStoplistKeyword = Advance();
            }
            else
            {
                source = ParseQualifiedName();
            }
        }

        var (authorizationKeyword, owner) = ParseTsqlAuthorization();
        var end = owner?.Span ?? systemStoplistKeyword?.Span ?? (source.Count > 0 ? source[^1].Span : name.Span);
        return new TsqlCreateFulltextStoplistStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            FulltextKeyword = fulltextKeyword,
            StoplistKeyword = stoplistKeyword,
            Name = name,
            FromKeyword = fromKeyword,
            Source = source,
            SystemKeyword = systemKeyword,
            SystemStoplistKeyword = systemStoplistKeyword,
            AuthorizationKeyword = authorizationKeyword,
            Owner = owner,
        };
    }

    // ALTER FULLTEXT STOPLIST name {ADD 'word' LANGUAGE l | DROP {'word' LANGUAGE l | ALL LANGUAGE l | ALL}}
    private TsqlAlterFulltextStoplistStatement ParseTsqlAlterFulltextStoplist()
    {
        var alterKeyword = Advance();
        var fulltextKeyword = Advance();
        var stoplistKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var action = IdentifierEquals(Keyword.Add) || IdentifierEquals(Keyword.Drop)
            ? Advance()
            : throw new SqlParseException($"Expected ADD or DROP, found {_current.Kind}", _current.Position);
        var allKeyword = TokenEquals(action, Keyword.Drop) && _current.Kind == SyntaxKind.AllKeyword ? Advance() : (SyntaxToken?)null;
        var word = allKeyword is null ? Expect(SyntaxKind.String) : (SyntaxToken?)null;
        SyntaxToken? languageKeyword = null;
        SyntaxToken? language = null;
        if (word is not null || IdentifierEquals(Keyword.Language))
        {
            languageKeyword = ExpectIdent(Keyword.Language);
            language = ExpectTsqlLanguage();
        }

        return new TsqlAlterFulltextStoplistStatement
        {
            Span = SourceSpan.From(alterKeyword, language ?? allKeyword!.Value),
            AlterKeyword = alterKeyword,
            FulltextKeyword = fulltextKeyword,
            StoplistKeyword = stoplistKeyword,
            Name = name,
            Action = action,
            AllKeyword = allKeyword,
            Word = word,
            LanguageKeyword = languageKeyword,
            Language = language,
        };
    }

    // CREATE SEARCH PROPERTY LIST name [FROM [db.]source_list] [AUTHORIZATION owner]
    private TsqlCreateSearchPropertyListStatement ParseTsqlCreateSearchPropertyList()
    {
        var createKeyword = Advance();
        var searchKeyword = Advance();
        var propertyKeyword = Advance();
        var listKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var fromKeyword = _current.Kind == SyntaxKind.FromKeyword ? Advance() : (SyntaxToken?)null;
        IReadOnlyList<SyntaxToken> source = fromKeyword is null ? [] : ParseQualifiedName();
        var (authorizationKeyword, owner) = ParseTsqlAuthorization();
        return new TsqlCreateSearchPropertyListStatement
        {
            Span = SourceSpan.From(createKeyword, owner?.Span ?? (source.Count > 0 ? source[^1].Span : name.Span)),
            CreateKeyword = createKeyword,
            SearchKeyword = searchKeyword,
            PropertyKeyword = propertyKeyword,
            ListKeyword = listKeyword,
            Name = name,
            FromKeyword = fromKeyword,
            Source = source,
            AuthorizationKeyword = authorizationKeyword,
            Owner = owner,
        };
    }

    // ALTER SEARCH PROPERTY LIST name {ADD 'property' WITH (option, ...) | DROP 'property'}
    private TsqlAlterSearchPropertyListStatement ParseTsqlAlterSearchPropertyList()
    {
        var alterKeyword = Advance();
        var searchKeyword = Advance();
        var propertyKeyword = Advance();
        var listKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var action = IdentifierEquals(Keyword.Add) || IdentifierEquals(Keyword.Drop)
            ? Advance()
            : throw new SqlParseException($"Expected ADD or DROP, found {_current.Kind}", _current.Position);
        var property = Expect(SyntaxKind.String);
        SyntaxToken? withKeyword = null;
        TsqlOptionList? options = null;
        if (TokenEquals(action, Keyword.Add))
        {
            withKeyword = Expect(SyntaxKind.WithKeyword);
            options = ParseTsqlOptionList();
        }

        return new TsqlAlterSearchPropertyListStatement
        {
            Span = SourceSpan.From(alterKeyword, options?.Span ?? property.Span),
            AlterKeyword = alterKeyword,
            SearchKeyword = searchKeyword,
            PropertyKeyword = propertyKeyword,
            ListKeyword = listKeyword,
            Name = name,
            Action = action,
            Property = property,
            WithKeyword = withKeyword,
            Options = options,
        };
    }
}
