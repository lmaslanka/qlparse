namespace QlParse;

// Postgres logical replication: CREATE PUBLICATION (and its table / schema object list, shared with
// ALTER PUBLICATION) and CREATE SUBSCRIPTION.
internal sealed partial class Parser
{
    // CREATE PUBLICATION name [FOR ALL TABLES | FOR object, ...] [WITH (...)]
    private PgCreatePublicationStatement ParsePgCreatePublication()
    {
        var createKeyword = Advance();
        var publicationKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        SyntaxToken? forKeyword = null;
        SyntaxToken? allKeyword = null;
        SyntaxToken? tablesKeyword = null;
        IReadOnlyList<PgPublicationObject> objects = [];
        var end = name.Span;
        if (_current.Kind == SyntaxKind.ForKeyword)
        {
            forKeyword = Advance();
            if (_current.Kind == SyntaxKind.AllKeyword)
            {
                allKeyword = Advance();
                var tables = ExpectIdent(Keyword.Tables);
                tablesKeyword = tables;
                end = tables.Span;
            }
            else
            {
                var list = ParsePgPublicationObjects();
                objects = list;
                end = list[^1].Span;
            }
        }

        SyntaxToken? withKeyword = _current.Kind == SyntaxKind.WithKeyword ? Advance() : null;
        PgDefinitionList? definition = withKeyword is null ? null : ParsePgDefinitionList();
        return new PgCreatePublicationStatement
        {
            Span = SourceSpan.From(createKeyword, definition?.Span ?? end),
            CreateKeyword = createKeyword,
            PublicationKeyword = publicationKeyword,
            Name = name,
            ForKeyword = forKeyword,
            AllKeyword = allKeyword,
            TablesKeyword = tablesKeyword,
            Objects = objects,
            WithKeyword = withKeyword,
            Definition = definition,
        };
    }

    // TABLE t [, ...] | TABLES IN SCHEMA s [, ...], mixed; an item without a leading keyword repeats the previous kind.
    internal List<PgPublicationObject> ParsePgPublicationObjects()
    {
        if (!IdentifierEquals(Keyword.Table) && !IdentifierEquals(Keyword.Tables))
        {
            throw new SqlParseException($"Expected TABLE or TABLES, found {_current.Kind}", _current.Position);
        }

        var objects = new List<PgPublicationObject>();
        var inSchema = false;
        while (true)
        {
            if (IdentifierEquals(Keyword.Tables) && NextKind == SyntaxKind.InKeyword)
            {
                inSchema = true;
            }
            else
            {
                if (IdentifierEquals(Keyword.Table) && NextKind != SyntaxKind.Comma)
                {
                    inSchema = false;
                }
            }

            objects.Add(inSchema ? ParsePgPublicationSchema() : ParsePgPublicationTable());
            if (_current.Kind != SyntaxKind.Comma)
            {
                return objects;
            }

            Advance();
        }
    }

    private PgPublicationObject ParsePgPublicationSchema()
    {
        var start = _current;
        SyntaxToken? tablesKeyword = null;
        SyntaxToken? inKeyword = null;
        SyntaxToken? schemaKeyword = null;
        if (IdentifierEquals(Keyword.Tables))
        {
            tablesKeyword = Advance();
            inKeyword = Expect(SyntaxKind.InKeyword);
            schemaKeyword = ExpectIdent(Keyword.Schema);
        }

        if (_current.Kind is not SyntaxKind.Identifier and not SyntaxKind.CurrentSchemaKeyword)
        {
            throw new SqlParseException($"Expected schema, found {_current.Kind}", _current.Position);
        }

        var schema = Advance();
        return new PgPublicationObject
        {
            Span = SourceSpan.From(start, schema),
            TableKeyword = tablesKeyword,
            InKeyword = inKeyword,
            SchemaKeyword = schemaKeyword,
            Name = [schema],
        };
    }

    // [TABLE] [ONLY] name [*] [(columns)] [WHERE (condition)]
    private PgPublicationObject ParsePgPublicationTable()
    {
        var start = _current;
        SyntaxToken? tableKeyword = IdentifierEquals(Keyword.Table) && NextKind != SyntaxKind.Comma ? Advance() : null;
        SyntaxToken? onlyKeyword = _current.Kind == SyntaxKind.OnlyKeyword ? Advance() : null;
        var name = ParseQualifiedName();
        SyntaxToken? star = _current.Kind == SyntaxKind.Star ? Advance() : null;
        SyntaxToken? openParen = null;
        IReadOnlyList<SyntaxToken> columns = [];
        SyntaxToken? closeParen = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            openParen = Advance();
            columns = ParseNameList();
            closeParen = Expect(SyntaxKind.CloseParen);
        }

        SyntaxToken? whereKeyword = null;
        SyntaxToken? whereOpen = null;
        Expression? where = null;
        SyntaxToken? whereClose = null;
        if (_current.Kind == SyntaxKind.WhereKeyword)
        {
            whereKeyword = Advance();
            whereOpen = Expect(SyntaxKind.OpenParen);
            where = ParseExpression();
            whereClose = Expect(SyntaxKind.CloseParen);
        }

        return new PgPublicationObject
        {
            Span = SourceSpan.From(start, whereClose ?? closeParen ?? star ?? name[^1]),
            TableKeyword = tableKeyword,
            OnlyKeyword = onlyKeyword,
            Name = name,
            Star = star,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = closeParen,
            WhereKeyword = whereKeyword,
            WhereOpenParen = whereOpen,
            Where = where,
            WhereCloseParen = whereClose,
        };
    }

    // CREATE SUBSCRIPTION name CONNECTION 'conninfo' PUBLICATION p [, ...] [WITH (...)]
    private PgCreateSubscriptionStatement ParsePgCreateSubscription()
    {
        var createKeyword = Advance();
        var subscriptionKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var connectionKeyword = ExpectIdent(Keyword.Connection);
        var connection = Expect(SyntaxKind.String);
        var publicationKeyword = ExpectIdent(Keyword.PgPublication);
        var publications = ParseNameList();
        SyntaxToken? withKeyword = _current.Kind == SyntaxKind.WithKeyword ? Advance() : null;
        PgDefinitionList? definition = withKeyword is null ? null : ParsePgDefinitionList();
        return new PgCreateSubscriptionStatement
        {
            Span = SourceSpan.From(createKeyword, definition?.Span ?? publications[^1].Span),
            CreateKeyword = createKeyword,
            SubscriptionKeyword = subscriptionKeyword,
            Name = name,
            ConnectionKeyword = connectionKeyword,
            Connection = connection,
            PublicationKeyword = publicationKeyword,
            Publications = publications,
            WithKeyword = withKeyword,
            Definition = definition,
        };
    }
}
