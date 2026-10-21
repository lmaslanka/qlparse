namespace QlParse;

// Postgres extensions, procedural languages, and foreign data: CREATE EXTENSION, CREATE LANGUAGE,
// CREATE FOREIGN DATA WRAPPER, CREATE SERVER, CREATE FOREIGN TABLE, and IMPORT FOREIGN SCHEMA.
internal sealed partial class Parser
{
    private readonly record struct PgExtensionOptions(
        SyntaxToken? SchemaKeyword,
        SyntaxToken? Schema,
        SyntaxToken? VersionKeyword,
        SyntaxToken? Version,
        SyntaxToken? CascadeKeyword,
        SourceSpan? End);

    // CREATE EXTENSION [IF NOT EXISTS] name [WITH] [SCHEMA s] [VERSION v] [CASCADE]
    private PgCreateExtensionStatement ParsePgCreateExtension()
    {
        var createKeyword = Advance();
        var extensionKeyword = Advance();
        var ifNotExists = ParsePgIfNotExists();
        var name = Expect(SyntaxKind.Identifier);
        SyntaxToken? withKeyword = _current.Kind == SyntaxKind.WithKeyword ? Advance() : null;
        var options = ParsePgExtensionOptions();
        return new PgCreateExtensionStatement
        {
            Span = SourceSpan.From(createKeyword, options.End ?? withKeyword?.Span ?? name.Span),
            CreateKeyword = createKeyword,
            ExtensionKeyword = extensionKeyword,
            IfKeyword = ifNotExists.IfKeyword,
            NotKeyword = ifNotExists.NotKeyword,
            ExistsKeyword = ifNotExists.ExistsKeyword,
            Name = name,
            WithKeyword = withKeyword,
            SchemaKeyword = options.SchemaKeyword,
            Schema = options.Schema,
            VersionKeyword = options.VersionKeyword,
            Version = options.Version,
            CascadeKeyword = options.CascadeKeyword,
        };
    }

    private PgExtensionOptions ParsePgExtensionOptions()
    {
        SyntaxToken? schemaKeyword = null;
        SyntaxToken? schema = null;
        SyntaxToken? versionKeyword = null;
        SyntaxToken? version = null;
        SyntaxToken? cascadeKeyword = null;
        SourceSpan? end = null;
        while (true)
        {
            if (schemaKeyword is null && IdentifierEquals(Keyword.Schema))
            {
                schemaKeyword = Advance();
                var schemaName = Expect(SyntaxKind.Identifier);
                schema = schemaName;
                end = schemaName.Span;
                continue;
            }

            if (versionKeyword is null && IdentifierEquals(Keyword.PgVersion))
            {
                versionKeyword = Advance();
                var versionName = ExpectPgNameOrString();
                version = versionName;
                end = versionName.Span;
                continue;
            }

            if (cascadeKeyword is null && IdentifierEquals(Keyword.Cascade))
            {
                var cascade = Advance();
                cascadeKeyword = cascade;
                end = cascade.Span;
                continue;
            }

            return new PgExtensionOptions(schemaKeyword, schema, versionKeyword, version, cascadeKeyword, end);
        }
    }

    // CREATE [OR REPLACE] [TRUSTED] [PROCEDURAL] LANGUAGE name [HANDLER h [INLINE i] [VALIDATOR v]]
    private PgCreateLanguageStatement ParsePgCreateLanguage()
    {
        var createKeyword = Advance();
        SyntaxToken? orKeyword = null;
        SyntaxToken? replaceKeyword = null;
        if (_current.Kind == SyntaxKind.OrKeyword)
        {
            orKeyword = Advance();
            replaceKeyword = Advance();
        }

        SyntaxToken? trustedKeyword = IdentifierEquals(Keyword.PgTrusted) ? Advance() : null;
        SyntaxToken? proceduralKeyword = IdentifierEquals(Keyword.PgProcedural) ? Advance() : null;
        var languageKeyword = ExpectIdent(Keyword.Language);
        var name = ExpectPgNameOrString();
        var handlers = ParsePgHandlerClauses();
        return new PgCreateLanguageStatement
        {
            Span = SourceSpan.From(createKeyword, handlers.Count > 0 ? handlers[^1].Span : name.Span),
            CreateKeyword = createKeyword,
            OrKeyword = orKeyword,
            ReplaceKeyword = replaceKeyword,
            TrustedKeyword = trustedKeyword,
            ProceduralKeyword = proceduralKeyword,
            LanguageKeyword = languageKeyword,
            Name = name,
            Handlers = handlers,
        };
    }

    // HANDLER f | NO HANDLER | INLINE f | VALIDATOR f | NO VALIDATOR, in any order.
    private IReadOnlyList<PgHandlerClause> ParsePgHandlerClauses()
    {
        List<PgHandlerClause>? clauses = null;
        while (IsPgHandlerClauseStart())
        {
            var start = _current;
            SyntaxToken? noKeyword = IdentifierEquals(Keyword.No) ? Advance() : null;
            var keyword = Advance();
            IReadOnlyList<SyntaxToken> function = noKeyword is null ? ParseQualifiedName() : [];
            clauses ??= [];
            clauses.Add(new PgHandlerClause
            {
                Span = SourceSpan.From(start, function.Count > 0 ? function[^1] : keyword),
                NoKeyword = noKeyword,
                Keyword = keyword,
                Function = function,
            });
        }

        return clauses ?? (IReadOnlyList<PgHandlerClause>)[];
    }

    private bool IsPgHandlerClauseStart()
    {
        if (IdentifierEquals(Keyword.No))
        {
            return NextEquals(Keyword.Handler) || NextEquals(Keyword.PgValidator);
        }

        return IdentifierEquals(Keyword.Handler) || IdentifierEquals(Keyword.PgInline) || IdentifierEquals(Keyword.PgValidator);
    }

    // CREATE FOREIGN DATA WRAPPER name [HANDLER h | NO HANDLER] [VALIDATOR v | NO VALIDATOR] [OPTIONS (...)]
    private PgCreateForeignDataWrapperStatement ParsePgCreateForeignDataWrapper()
    {
        var createKeyword = Advance();
        var foreignKeyword = Advance();
        var dataKeyword = Advance();
        var wrapperKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var handlers = ParsePgHandlerClauses();
        var options = ParsePgOptionalOptionList();
        var end = options?.Span ?? (handlers.Count > 0 ? handlers[^1].Span : name.Span);
        return new PgCreateForeignDataWrapperStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            ForeignKeyword = foreignKeyword,
            DataKeyword = dataKeyword,
            WrapperKeyword = wrapperKeyword,
            Name = name,
            Handlers = handlers,
            Options = options,
        };
    }

    // CREATE SERVER [IF NOT EXISTS] name [TYPE 't'] [VERSION 'v'] FOREIGN DATA WRAPPER fdw [OPTIONS (...)]
    private PgCreateServerStatement ParsePgCreateServer()
    {
        var createKeyword = Advance();
        var serverKeyword = Advance();
        var ifNotExists = ParsePgIfNotExists();
        var name = Expect(SyntaxKind.Identifier);
        SyntaxToken? typeKeyword = IdentifierEquals(Keyword.Type) ? Advance() : null;
        SyntaxToken? type = typeKeyword is null ? null : Expect(SyntaxKind.String);
        SyntaxToken? versionKeyword = IdentifierEquals(Keyword.PgVersion) ? Advance() : null;
        SyntaxToken? version = versionKeyword is null ? null : Expect(SyntaxKind.String);
        var foreignKeyword = ExpectIdent(Keyword.Foreign);
        var dataKeyword = ExpectIdent(Keyword.Data);
        var wrapperKeyword = ExpectIdent(Keyword.Wrapper);
        var wrapper = Expect(SyntaxKind.Identifier);
        var options = ParsePgOptionalOptionList();
        return new PgCreateServerStatement
        {
            Span = SourceSpan.From(createKeyword, options?.Span ?? wrapper.Span),
            CreateKeyword = createKeyword,
            ServerKeyword = serverKeyword,
            IfKeyword = ifNotExists.IfKeyword,
            NotKeyword = ifNotExists.NotKeyword,
            ExistsKeyword = ifNotExists.ExistsKeyword,
            Name = name,
            TypeKeyword = typeKeyword,
            Type = type,
            VersionKeyword = versionKeyword,
            Version = version,
            ForeignKeyword = foreignKeyword,
            DataKeyword = dataKeyword,
            WrapperKeyword = wrapperKeyword,
            Wrapper = wrapper,
            Options = options,
        };
    }

    // IMPORT FOREIGN SCHEMA remote [{LIMIT TO | EXCEPT} (t, ...)] FROM SERVER s INTO local [OPTIONS (...)]
    private PgImportForeignSchemaStatement ParsePgImportForeignSchema()
    {
        var importKeyword = Advance();
        var foreignKeyword = Advance();
        var schemaKeyword = ExpectIdent(Keyword.Schema);
        var remoteSchema = Expect(SyntaxKind.Identifier);
        SyntaxToken? filterKeyword = null;
        SyntaxToken? toKeyword = null;
        SyntaxToken? openParen = null;
        IReadOnlyList<SyntaxToken> tables = [];
        SyntaxToken? closeParen = null;
        if (_current.Kind is SyntaxKind.LimitKeyword or SyntaxKind.ExceptKeyword)
        {
            var filter = Advance();
            filterKeyword = filter;
            toKeyword = filter.Kind == SyntaxKind.LimitKeyword ? ExpectIdent(Keyword.To) : null;
            openParen = Expect(SyntaxKind.OpenParen);
            tables = ParseNameList();
            closeParen = Expect(SyntaxKind.CloseParen);
        }

        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var serverKeyword = ExpectIdent(Keyword.PgServer);
        var server = Expect(SyntaxKind.Identifier);
        var intoKeyword = ExpectIdent(Keyword.Into);
        var localSchema = Expect(SyntaxKind.Identifier);
        var options = ParsePgOptionalOptionList();
        return new PgImportForeignSchemaStatement
        {
            Span = SourceSpan.From(importKeyword, options?.Span ?? localSchema.Span),
            ImportKeyword = importKeyword,
            ForeignKeyword = foreignKeyword,
            SchemaKeyword = schemaKeyword,
            RemoteSchema = remoteSchema,
            FilterKeyword = filterKeyword,
            ToKeyword = toKeyword,
            OpenParen = openParen,
            Tables = tables,
            CloseParen = closeParen,
            FromKeyword = fromKeyword,
            ServerKeyword = serverKeyword,
            Server = server,
            IntoKeyword = intoKeyword,
            LocalSchema = localSchema,
            Options = options,
        };
    }
}
