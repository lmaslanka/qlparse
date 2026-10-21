namespace QlParse;

// Other SQL Server objects (workstream F): XML schema collections, assemblies, legacy defaults and rules, and the
// PolyBase / AI external objects.
internal sealed partial class Parser
{
    // CREATE XML SCHEMA COLLECTION name AS expr | ALTER XML SCHEMA COLLECTION name ADD expr
    private TsqlXmlSchemaCollectionStatement ParseTsqlXmlSchemaCollection()
    {
        var verb = Advance();
        var xml = Advance();
        var schema = Advance();
        var collection = Advance();
        var name = ParseQualifiedName();
        var content = TokenEquals(verb, Keyword.Create) ? Expect(SyntaxKind.AsKeyword) : ExpectIdent(Keyword.Add);
        var expression = ParseExpression();
        return new TsqlXmlSchemaCollectionStatement
        {
            Span = SourceSpan.From(verb, expression.Span),
            VerbKeyword = verb,
            XmlKeyword = xml,
            SchemaKeyword = schema,
            CollectionKeyword = collection,
            Name = name,
            ContentKeyword = content,
            Content = expression,
        };
    }

    // CREATE ASSEMBLY name [AUTHORIZATION owner] FROM {'path' | 0x...}, ... [WITH PERMISSION_SET = {SAFE | EXTERNAL_ACCESS | UNSAFE}]
    private TsqlCreateAssemblyStatement ParseTsqlCreateAssembly()
    {
        var create = Advance();
        var assembly = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var authorization = IdentifierEquals(Keyword.Authorization) ? Advance() : (SyntaxToken?)null;
        var owner = authorization is null ? (SyntaxToken?)null : ParseGrantee();
        var from = Expect(SyntaxKind.FromKeyword);
        var sources = new List<Expression> { ParseExpression() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            sources.Add(ParseExpression());
        }

        var withKeyword = _current.Kind == SyntaxKind.WithKeyword ? Advance() : (SyntaxToken?)null;
        var option = withKeyword is null ? null : ParseTsqlOption();
        return new TsqlCreateAssemblyStatement
        {
            Span = SourceSpan.From(create.Span, option?.Span ?? sources[^1].Span),
            CreateKeyword = create,
            AssemblyKeyword = assembly,
            Name = name,
            AuthorizationKeyword = authorization,
            Owner = owner,
            FromKeyword = from,
            Sources = sources,
            WithKeyword = withKeyword,
            Option = option,
        };
    }

    // CREATE DEFAULT name AS constant_expression | CREATE RULE name AS condition_expression
    private TsqlCreateDefaultOrRuleStatement ParseTsqlCreateDefaultOrRule()
    {
        var create = Advance();
        var kind = Advance();
        var name = ParseQualifiedName();
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var expression = ParseExpression();
        return new TsqlCreateDefaultOrRuleStatement
        {
            Span = SourceSpan.From(create, expression.Span),
            CreateKeyword = create,
            KindKeyword = kind,
            Name = name,
            AsKeyword = asKeyword,
            Expression = expression,
        };
    }

    // CREATE EXTERNAL {DATA SOURCE | FILE FORMAT | MODEL} name [AUTHORIZATION owner] WITH (option, ...)
    private TsqlCreateExternalObjectStatement ParseTsqlCreateExternalObject(int kindLength)
    {
        var create = Advance();
        var external = Advance();
        var kind = TakeTsqlWords(kindLength);
        var name = ParseQualifiedName();
        var authorization = IdentifierEquals(Keyword.Authorization) ? Advance() : (SyntaxToken?)null;
        var owner = authorization is null ? (SyntaxToken?)null : ParseGrantee();
        var withKeyword = Expect(SyntaxKind.WithKeyword);
        var options = ParseTsqlOptionList();
        return new TsqlCreateExternalObjectStatement
        {
            Span = SourceSpan.From(create, options.Span),
            CreateKeyword = create,
            ExternalKeyword = external,
            KindKeywords = kind,
            Name = name,
            AuthorizationKeyword = authorization,
            Owner = owner,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // CREATE EXTERNAL TABLE name [(column, ...)] WITH (option, ...) [AS select]
    private TsqlCreateExternalTableStatement ParseTsqlCreateExternalTable()
    {
        var create = Advance();
        var external = Advance();
        var table = Advance();
        var name = ParseQualifiedName();
        SyntaxToken? openParen = null;
        IReadOnlyList<TableElement> elements = [];
        SyntaxToken? closeParen = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            ParseTableElements(out var elementOpen, out var elementList, out var elementClose);
            openParen = elementOpen;
            elements = elementList;
            closeParen = elementClose;
        }

        var withKeyword = Expect(SyntaxKind.WithKeyword);
        var options = ParseTsqlOptionList();
        var asKeyword = _current.Kind == SyntaxKind.AsKeyword ? Advance() : (SyntaxToken?)null;
        var query = asKeyword is null ? null : ParseQuery();
        return new TsqlCreateExternalTableStatement
        {
            Span = SourceSpan.From(create.Span, query?.Span ?? options.Span),
            CreateKeyword = create,
            ExternalKeyword = external,
            TableKeyword = table,
            Name = name,
            OpenParen = openParen,
            Elements = elements,
            CloseParen = closeParen,
            WithKeyword = withKeyword,
            Options = options,
            AsKeyword = asKeyword,
            Query = query,
        };
    }
}
