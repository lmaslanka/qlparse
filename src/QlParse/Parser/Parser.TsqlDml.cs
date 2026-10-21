namespace QlParse;

// Workstream C: SQL Server query expressions and DML. This hook runs first in the T-SQL statement dispatch and
// claims INSERT / UPDATE / DELETE / MERGE (their own Tsql*Statement nodes: optional INTO / FROM, TOP, OUTPUT,
// table hints, UPDATE / DELETE ... FROM, OPTION), BULK INSERT, TRUNCATE TABLE ... WITH (PARTITIONS ...), and a
// statement-level WITH [XMLNAMESPACES] in front of a query or DML statement. SELECT stays on the ISO path,
// which branches into ParseTsqlSelectStatement. UPDATE STATISTICS is left to the DDL hook.
internal sealed partial class Parser
{
    internal Query? TryParseTsqlDmlStatement() => _current.Kind switch
    {
        SyntaxKind.WithKeyword => ParseTsqlWithStatement(),
        SyntaxKind.UpdateKeyword => NextEquals(Keyword.DmlStatistics) ? null : ParseTsqlUpdate(),
        SyntaxKind.Identifier => TryParseTsqlDmlVerb(),
        _ => null,
    };

    private Query? TryParseTsqlDmlVerb()
    {
        if (IdentifierEquals(Keyword.Insert))
        {
            return ParseTsqlInsert();
        }

        if (IdentifierEquals(Keyword.Delete))
        {
            return ParseTsqlDelete();
        }

        if (IdentifierEquals(Keyword.Merge))
        {
            return ParseTsqlMerge();
        }

        if (IdentifierEquals(Keyword.DmlBulk) && NextEquals(Keyword.Insert))
        {
            return ParseTsqlBulkInsert();
        }

        return IdentifierEquals(Keyword.Truncate) && NextEquals(Keyword.Table) ? ParseTsqlTruncate() : null;
    }

    // ---- WITH [XMLNAMESPACES (...)] [cte, ...] statement ----

    private Query ParseTsqlWithStatement()
    {
        if (NextEquals(Keyword.DmlXmlNamespaces))
        {
            return ParseTsqlXmlNamespacesQuery();
        }

        var withKeyword = Advance();
        var ctes = ParseTsqlCtes();
        var query = ParseTsqlWithBody();
        return new WithQuery
        {
            Span = SourceSpan.From(withKeyword, query.Span),
            WithKeyword = withKeyword,
            Ctes = ctes,
            Query = query,
        };
    }

    private List<CommonTableExpression> ParseTsqlCtes()
    {
        var ctes = new List<CommonTableExpression> { ParseCte() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            ctes.Add(ParseCte());
        }

        return ctes;
    }

    // The statement after WITH: a query, or INSERT / UPDATE / DELETE / MERGE.
    private Query ParseTsqlWithBody()
    {
        if (_current.Kind == SyntaxKind.UpdateKeyword)
        {
            return ParseTsqlUpdate();
        }

        if (IdentifierEquals(Keyword.Insert))
        {
            return ParseTsqlInsert();
        }

        if (IdentifierEquals(Keyword.Delete))
        {
            return ParseTsqlDelete();
        }

        return IdentifierEquals(Keyword.Merge) ? ParseTsqlMerge() : ParseQuery(1);
    }

    private TsqlWithXmlNamespacesQuery ParseTsqlXmlNamespacesQuery()
    {
        var withKeyword = Advance();
        var xmlNamespacesKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var namespaces = new List<TsqlXmlNamespace> { ParseTsqlXmlNamespace() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            namespaces.Add(ParseTsqlXmlNamespace());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        var comma = _current.Kind == SyntaxKind.Comma ? Advance() : (SyntaxToken?)null;
        IReadOnlyList<CommonTableExpression> ctes = comma is null ? [] : ParseTsqlCtes();
        var query = ParseTsqlWithBody();
        return new TsqlWithXmlNamespacesQuery
        {
            Span = SourceSpan.From(withKeyword, query.Span),
            WithKeyword = withKeyword,
            XmlNamespacesKeyword = xmlNamespacesKeyword,
            OpenParen = openParen,
            Namespaces = namespaces,
            CloseParen = closeParen,
            Comma = comma,
            Ctes = ctes,
            Query = query,
        };
    }

    // 'uri' AS prefix | DEFAULT 'uri'
    private TsqlXmlNamespace ParseTsqlXmlNamespace()
    {
        if (IdentifierEquals(Keyword.Default))
        {
            var defaultKeyword = Advance();
            var defaultUri = Expect(SyntaxKind.String);
            return new TsqlXmlNamespace { Span = SourceSpan.From(defaultKeyword, defaultUri), DefaultKeyword = defaultKeyword, Uri = defaultUri };
        }

        var uri = Expect(SyntaxKind.String);
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var prefix = Expect(SyntaxKind.Identifier);
        return new TsqlXmlNamespace { Span = SourceSpan.From(uri, prefix), Uri = uri, AsKeyword = asKeyword, Prefix = prefix };
    }

    // ---- INSERT ----

    private TsqlInsertStatement ParseTsqlInsert()
    {
        var insertKeyword = Advance();
        var top = ParseOptionalTsqlDmlTop();
        var intoKeyword = IdentifierEquals(Keyword.Into) ? Advance() : (SyntaxToken?)null;
        var target = ParseTsqlDmlTarget(allowAlias: false);
        SyntaxToken? columnOpen = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        SyntaxToken? columnClose = null;
        if (_current.Kind == SyntaxKind.OpenParen && !IsQueryStart(NextKind))
        {
            ParseOptionalColumnList(out var open, out var names, out var close);
            columnOpen = open;
            columns = names;
            columnClose = close;
        }

        var output = ParseTsqlOutputClauses();
        var source = ParseTsqlInsertValues();
        var option = ParseOptionalTsqlOption();
        return new TsqlInsertStatement
        {
            Span = SourceSpan.From(insertKeyword.Span, option?.Span ?? source.End),
            InsertKeyword = insertKeyword,
            Top = top,
            IntoKeyword = intoKeyword,
            Target = target,
            ColumnOpenParen = columnOpen,
            Columns = columns,
            ColumnCloseParen = columnClose,
            Output = output,
            DefaultKeyword = source.DefaultKeyword,
            ValuesKeyword = source.ValuesKeyword,
            Source = source.Query,
            Option = option,
        };
    }

    private readonly record struct TsqlInsertValues(SyntaxToken? DefaultKeyword, SyntaxToken? ValuesKeyword, Query? Query, SourceSpan End);

    // DEFAULT VALUES | VALUES (...), ... | query | EXEC[UTE] ... (ParseTsqlInsertSource, workstream E)
    private TsqlInsertValues ParseTsqlInsertValues()
    {
        if (!IdentifierEquals(Keyword.Default) || NextKind != SyntaxKind.ValuesKeyword)
        {
            var query = ParseTsqlInsertSource();
            return new TsqlInsertValues(null, null, query, query.Span);
        }

        var defaultKeyword = Advance();
        var valuesKeyword = Advance();
        return new TsqlInsertValues(defaultKeyword, valuesKeyword, null, valuesKeyword.Span);
    }

    // ---- UPDATE ----

    private TsqlUpdateStatement ParseTsqlUpdate()
    {
        var updateKeyword = Advance();
        var top = ParseOptionalTsqlDmlTop();
        var target = ParseTsqlDmlTarget(allowAlias: true);
        var setKeyword = Expect(SyntaxKind.SetKeyword);
        var assignments = ParseTsqlSetClauses();
        var output = ParseTsqlOutputClauses();
        var from = ParseFromClause();
        var where = ParseTsqlDmlWhere();
        var option = ParseOptionalTsqlOption();
        var end = option?.Span ?? where.End ?? from.End ?? (output.Count > 0 ? output[^1].Span : assignments[^1].Span);
        return new TsqlUpdateStatement
        {
            Span = SourceSpan.From(updateKeyword, end),
            UpdateKeyword = updateKeyword,
            Top = top,
            Target = target,
            SetKeyword = setKeyword,
            Assignments = assignments,
            Output = output,
            FromKeyword = from.FromKeyword,
            From = from.From,
            Joins = from.Joins,
            ExtraFrom = from.ExtraFrom,
            Where = where.Where,
            CurrentOf = where.CurrentOf,
            Option = option,
        };
    }

    // ---- DELETE ----

    private TsqlDeleteStatement ParseTsqlDelete()
    {
        var deleteKeyword = Advance();
        var top = ParseOptionalTsqlDmlTop();
        var fromKeyword = _current.Kind == SyntaxKind.FromKeyword ? Advance() : (SyntaxToken?)null;
        var target = ParseTsqlDmlTarget(allowAlias: true);
        var output = ParseTsqlOutputClauses();
        var from = ParseFromClause();
        var where = ParseTsqlDmlWhere();
        var option = ParseOptionalTsqlOption();
        var end = option?.Span ?? where.End ?? from.End ?? (output.Count > 0 ? output[^1].Span : target.Span);
        return new TsqlDeleteStatement
        {
            Span = SourceSpan.From(deleteKeyword, end),
            DeleteKeyword = deleteKeyword,
            Top = top,
            FromKeyword = fromKeyword,
            Target = target,
            Output = output,
            SourceFromKeyword = from.FromKeyword,
            From = from.From,
            Joins = from.Joins,
            ExtraFrom = from.ExtraFrom,
            Where = where.Where,
            CurrentOf = where.CurrentOf,
            Option = option,
        };
    }

    // ---- MERGE ----

    private TsqlMergeStatement ParseTsqlMerge()
    {
        var mergeKeyword = Advance();
        var top = ParseOptionalTsqlDmlTop();
        var intoKeyword = IdentifierEquals(Keyword.Into) ? Advance() : (SyntaxToken?)null;
        var target = ParseTsqlDmlTarget(allowAlias: true);
        var usingKeyword = ExpectTsqlWord(SyntaxKind.UsingKeyword, Keyword.Using);
        var source = ParseTableSource();
        var joins = ParseJoins();
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var condition = ParseExpression();
        if (_current.Kind != SyntaxKind.WhenKeyword)
        {
            throw new SqlParseException($"Expected {SyntaxKind.WhenKeyword}, found {_current.Kind}", _current.Position);
        }

        var whens = new List<MergeWhenClause>();
        while (_current.Kind == SyntaxKind.WhenKeyword)
        {
            whens.Add(ParseTsqlMergeWhen());
        }

        var output = ParseTsqlOutputClauses();
        var option = ParseOptionalTsqlOption();
        var end = option?.Span ?? (output.Count > 0 ? output[^1].Span : whens[^1].Span);
        return new TsqlMergeStatement
        {
            Span = SourceSpan.From(mergeKeyword, end),
            MergeKeyword = mergeKeyword,
            Top = top,
            IntoKeyword = intoKeyword,
            Target = target,
            UsingKeyword = usingKeyword,
            Source = source,
            SourceJoins = joins,
            OnKeyword = onKeyword,
            Condition = condition,
            Whens = whens,
            Output = output,
            Option = option,
        };
    }

    // ---- Shared DML pieces ----

    // name | @table | rowset function, then [WITH (hints)] and (not for INSERT) [[AS] alias]; OUTPUT is never an alias.
    private TableSource ParseTsqlDmlTarget(bool allowAlias)
    {
        var name = ParseTsqlMultiPartName();
        if (_current.Kind == SyntaxKind.OpenParen && name.Count == 1 && Keyword.IsTsqlRowsetFunction(name[0].TextOf(_source)))
        {
            return ParseTsqlRowsetFunction(name);
        }

        var reference = new TableReference { Span = SourceSpan.From(name[0], name[^1]), NameParts = name };
        TableSource table = reference;
        if (_current.Kind == SyntaxKind.WithKeyword && NextKind == SyntaxKind.OpenParen)
        {
            table = ParseTsqlHintedTable(table);
        }

        if (!allowAlias)
        {
            return table;
        }

        var alias = IdentifierEquals(Keyword.Output) ? default : ParseTsqlAlias();
        if (alias.Alias is not { } aliasToken)
        {
            return table;
        }

        var aliased = new TableReference
        {
            Span = SourceSpan.From(name[0], aliasToken),
            NameParts = name,
            AsKeyword = alias.AsKeyword,
            Alias = aliasToken,
        };
        return table is TsqlHintedTable hinted
            ? new TsqlHintedTable
            {
                Span = aliased.Span,
                Table = aliased,
                WithKeyword = hinted.WithKeyword,
                OpenParen = hinted.OpenParen,
                Hints = hinted.Hints,
                CloseParen = hinted.CloseParen,
            }
            : aliased;
    }

    private readonly record struct TsqlDmlWhere(WhereClause? Where, TsqlCurrentOfClause? CurrentOf, SourceSpan? End);

    // WHERE condition | WHERE CURRENT OF [GLOBAL] cursor | WHERE CURRENT OF @cursor_variable
    private TsqlDmlWhere ParseTsqlDmlWhere()
    {
        if (_current.Kind != SyntaxKind.WhereKeyword)
        {
            return default;
        }

        if (!IsPositionedWhere())
        {
            var where = ParseWhereClause();
            return new TsqlDmlWhere(where, null, where.Span);
        }

        var whereKeyword = Advance();
        var currentKeyword = Advance();
        var ofKeyword = Advance();
        var globalKeyword = IdentifierEquals(Keyword.Global) && NextKind == SyntaxKind.Identifier ? Advance() : (SyntaxToken?)null;
        var cursor = _current.Kind == SyntaxKind.EmbeddedHost ? Advance() : Expect(SyntaxKind.Identifier);
        var currentOf = new TsqlCurrentOfClause
        {
            Span = SourceSpan.From(whereKeyword, cursor),
            WhereKeyword = whereKeyword,
            CurrentKeyword = currentKeyword,
            OfKeyword = ofKeyword,
            GlobalKeyword = globalKeyword,
            Cursor = cursor,
        };
        return new TsqlDmlWhere(null, currentOf, currentOf.Span);
    }
}
