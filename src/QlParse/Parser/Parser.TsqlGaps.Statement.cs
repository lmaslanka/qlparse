namespace QlParse;

// SQL Server statements the other T-SQL hooks leave to ISO or reject (workstream H): ALTER VIEW with the
// CREATE VIEW grammar, ALTER SEQUENCE and the T-SQL sequence options, ALTER FULLTEXT INDEX, DROP for the
// Service Broker, server, full-text and CLR object kinds, and BACKUP / RESTORE of certificates and keys. Chained
// last, so it only sees statements every other T-SQL hook declined.
internal sealed partial class Parser
{
    internal Query? TryParseTsqlGapsStatement()
    {
        if (_current.Kind != SyntaxKind.Identifier || _index >= _tokens.Count)
        {
            return null;
        }

        if (IdentifierEquals(Keyword.Alter))
        {
            return TryParseTsqlGapsAlter();
        }

        if (IdentifierEquals(Keyword.Drop))
        {
            return TryParseTsqlGapsDrop();
        }

        return IdentifierEquals(Keyword.AdminBackup) || IdentifierEquals(Keyword.AdminRestore) ? TryParseTsqlKeyBackup() : null;
    }

    // {BACKUP | RESTORE} {CERTIFICATE name | [SERVICE] MASTER KEY | SYMMETRIC KEY name} {TO | FROM} ... clauses, in
    // the shape of the other key and certificate statements.
    private TsqlSecurityObjectStatement? TryParseTsqlKeyBackup()
    {
        if (NextEquals(Keyword.SecCertificate))
        {
            return ParseTsqlSecurityObject(1, named: true);
        }

        if (NextEquals(Keyword.Symmetric) && IsKeywordAt(_index + 1, Keyword.Key))
        {
            return ParseTsqlSecurityObject(Len2, named: true);
        }

        if (NextEquals(Keyword.SecMaster) && IsKeywordAt(_index + 1, Keyword.Key))
        {
            return ParseTsqlSecurityObject(Len2, named: false);
        }

        return NextEquals(Keyword.SecService) && IsKeywordAt(_index + 1, Keyword.SecMaster) && IsKeywordAt(_index + Len2, Keyword.Key)
            ? ParseTsqlSecurityObject(Len3, named: false)
            : null;
    }

    private Query? TryParseTsqlGapsAlter()
    {
        if (NextEquals(Keyword.View))
        {
            var view = ParseTsqlCreateView();
            return new TsqlAlterViewStatement { Span = view.Span, View = view };
        }

        if (NextEquals(Keyword.Sequence))
        {
            return ParseTsqlAlterSequence();
        }

        return NextEquals(Keyword.AdminFulltext) && IsKeywordAt(_index + 1, Keyword.Index) ? ParseTsqlAlterFulltextIndex() : null;
    }

    // ---- Sequences ----

    // ALTER SEQUENCE name [RESTART [WITH n]] [INCREMENT BY n] [{MINVALUE n | NO MINVALUE}] [{MAXVALUE n | NO MAXVALUE}]
    //     [[NO] CYCLE] [{CACHE [n] | NO CACHE}]
    private AlterSequenceStatement ParseTsqlAlterSequence()
    {
        var alterKeyword = Advance();
        var sequenceKeyword = Advance();
        var name = ParseQualifiedName();
        var options = new List<SequenceOption>();
        while (IsSequenceOption() || IsTsqlSequenceOption())
        {
            options.Add(ParseSequenceOption());
        }

        return new AlterSequenceStatement
        {
            Span = SourceSpan.From(alterKeyword, options.Count > 0 ? options[^1].Span : name[^1].Span),
            AlterKeyword = alterKeyword,
            SequenceKeyword = sequenceKeyword,
            Name = name,
            Options = options,
        };
    }

    internal bool IsDialectSequenceOption() => IsPostgres ? IsPgSequenceOption() : IsTsqlSequenceOption();

    // The sequence options T-SQL adds to ISO's: CYCLE (not a keyword here), CACHE [n], NO CYCLE, NO CACHE, RESTART.
    private bool IsTsqlSequenceOption() =>
        IdentifierEquals(Keyword.Cycle)
        || IdentifierEquals(Keyword.DdlCache)
        || IdentifierEquals(Keyword.Restart)
        || (IdentifierEquals(Keyword.No) && IsTsqlNoSequenceWord(_tokens[_index]));

    internal bool IsTsqlNoSequenceWord(SyntaxToken token) =>
        IsSqlServer && (TokenEquals(token, Keyword.Cycle) || TokenEquals(token, Keyword.DdlCache));

    // CYCLE, CACHE [n] or RESTART [WITH n]; reached from ParseSequenceOption once the ISO options declined.
    internal SequenceOption ParseTsqlSequenceOption()
    {
        if (IdentifierEquals(Keyword.Restart))
        {
            return ParsePgRestartOption();
        }

        var name = IdentifierEquals(Keyword.Cycle) ? Advance() : ExpectIdent(Keyword.DdlCache);
        var value = TokenEquals(name, Keyword.DdlCache) && _current.Kind == SyntaxKind.Number ? Literal() : null;
        return new SequenceOption { Span = SourceSpan.From(name.Span, value?.Span ?? name.Span), Name = name, Value = value };
    }

    // ---- ALTER FULLTEXT INDEX ----

    private TsqlAlterFulltextIndexStatement ParseTsqlAlterFulltextIndex()
    {
        var alterKeyword = Advance();
        var fulltextKeyword = Advance();
        var indexKeyword = Advance();
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var table = ParseQualifiedName();
        var action = ParseTsqlFulltextIndexAction();
        SyntaxToken? withKeyword = null;
        IReadOnlyList<SyntaxToken> noPopulation = [];
        if (_current.Kind == SyntaxKind.WithKeyword && NextEquals(Keyword.No))
        {
            withKeyword = Advance();
            noPopulation = [Advance(), ExpectIdent(Keyword.AdminPopulation)];
        }

        return new TsqlAlterFulltextIndexStatement
        {
            Span = SourceSpan.From(alterKeyword, TsqlLastConsumed()),
            AlterKeyword = alterKeyword,
            FulltextKeyword = fulltextKeyword,
            IndexKeyword = indexKeyword,
            OnKeyword = onKeyword,
            Table = table,
            Action = action.Words,
            Option = action.Option,
            OpenParen = action.OpenParen,
            Columns = action.Columns,
            CloseParen = action.CloseParen,
            WithKeyword = withKeyword,
            NoPopulation = noPopulation,
        };
    }

    private readonly record struct TsqlFulltextIndexAction(
        IReadOnlyList<SyntaxToken> Words,
        TsqlOption? Option,
        SyntaxToken? OpenParen,
        IReadOnlyList<TsqlFulltextIndexColumn> Columns,
        SyntaxToken? CloseParen);

    private TsqlFulltextIndexAction ParseTsqlFulltextIndexAction()
    {
        if (_current.Kind == SyntaxKind.SetKeyword)
        {
            var setKeyword = Advance();
            return new TsqlFulltextIndexAction([setKeyword], ParseTsqlFulltextIndexOption(), null, [], null);
        }

        if ((IdentifierEquals(Keyword.Add) || IdentifierEquals(Keyword.Drop)) && NextKind == SyntaxKind.OpenParen)
        {
            var verb = Advance();
            var openParen = Advance();
            var columns = new List<TsqlFulltextIndexColumn> { ParseTsqlFulltextIndexColumn() };
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                columns.Add(ParseTsqlFulltextIndexColumn());
            }

            return new TsqlFulltextIndexAction([verb], null, openParen, columns, Expect(SyntaxKind.CloseParen));
        }

        return new TsqlFulltextIndexAction(ParseTsqlFulltextIndexActionWords(), null, null, [], null);
    }

    // ENABLE | DISABLE | ALTER COLUMN c {ADD | DROP} STATISTICAL_SEMANTICS | START {FULL | INCREMENTAL | UPDATE}
    // POPULATION | {STOP | PAUSE | RESUME} POPULATION
    private List<SyntaxToken> ParseTsqlFulltextIndexActionWords()
    {
        if (IdentifierEquals(Keyword.DdlEnable) || IdentifierEquals(Keyword.Disable))
        {
            return [Advance()];
        }

        if (IdentifierEquals(Keyword.Alter))
        {
            return [Advance(), ExpectIdent(Keyword.Column), Expect(SyntaxKind.Identifier), ExpectTsqlAddOrDrop(),
                ExpectIdent(Keyword.AdminStatisticalSemantics)];
        }

        if (IdentifierEquals(Keyword.Start))
        {
            var start = Advance();
            var kind = _current.Kind is SyntaxKind.FullKeyword or SyntaxKind.UpdateKeyword || IdentifierEquals(Keyword.GapsIncremental)
                ? Advance()
                : throw new SqlParseException($"Expected FULL, INCREMENTAL or UPDATE, found {_current.Kind}", _current.Position);
            return [start, kind, ExpectIdent(Keyword.AdminPopulation)];
        }

        if (IdentifierEquals(Keyword.GapsStop) || IdentifierEquals(Keyword.TsqlDdlPause) || IdentifierEquals(Keyword.TsqlDdlResume))
        {
            return [Advance(), ExpectIdent(Keyword.AdminPopulation)];
        }

        throw new SqlParseException($"Expected full-text index action, found {_current.Kind}", _current.Position);
    }

    private SyntaxToken ExpectTsqlAddOrDrop() =>
        IdentifierEquals(Keyword.Add) ? Advance() : ExpectIdent(Keyword.Drop);

    // ---- DROP ----

    private TsqlDropObjectsStatement? TryParseTsqlGapsDrop()
    {
        var length = TsqlGapsDropKindLength(_tokens[_index]);
        return length == 0 ? null : ParseTsqlGapsDrop(length);
    }

    // The number of object-kind words after DROP for the kinds this hook owns, otherwise 0.
    private int TsqlGapsDropKindLength(SyntaxToken first)
    {
        if (first.Kind != SyntaxKind.Identifier)
        {
            return 0;
        }

        var text = first.TextOf(_source);
        if (IsTsqlGapsSingleDropKind(text))
        {
            return 1;
        }

        var second = _index + 1;
        return TsqlGapsBrokerDropKindLength(text, second) ?? TsqlGapsServerDropKindLength(text, second)
            ?? TsqlGapsFulltextDropKindLength(text, second) ?? 0;
    }

    private static bool IsTsqlGapsSingleDropKind(ReadOnlySpan<char> text) =>
        TextEquals(text, Keyword.AdminQueue)
        || TextEquals(text, Keyword.AdminService)
        || TextEquals(text, Keyword.AdminContract)
        || TextEquals(text, Keyword.AdminRoute)
        || TextEquals(text, Keyword.AdminEndpoint)
        || TextEquals(text, Keyword.ProcAggregate);

    // MESSAGE TYPE, REMOTE SERVICE BINDING, BROKER PRIORITY; null for other words.
    private int? TsqlGapsBrokerDropKindLength(ReadOnlySpan<char> text, int second)
    {
        if (TextEquals(text, Keyword.AdminMessage))
        {
            return IsKeywordAt(second, Keyword.Type) ? Len2 : 0;
        }

        if (TextEquals(text, Keyword.DmlRemote))
        {
            return IsKeywordAt(second, Keyword.AdminService) && IsKeywordAt(second + 1, Keyword.GapsBinding) ? Len3 : 0;
        }

        return TextEquals(text, Keyword.GapsBroker) ? DropKindLengthIf(IsKeywordAt(second, Keyword.GapsPriority), Len2) : null;
    }

    // AVAILABILITY GROUP, WORKLOAD {GROUP | CLASSIFIER}, RESOURCE POOL, EVENT {SESSION | NOTIFICATION}; null for
    // other words.
    private int? TsqlGapsServerDropKindLength(ReadOnlySpan<char> text, int second)
    {
        if (TextEquals(text, Keyword.AdminAvailability) || TextEquals(text, Keyword.AdminWorkload))
        {
            return DropKindLengthIf(IsGroupKindAt(second) || IsKeywordAt(second, Keyword.GapsClassifier), Len2);
        }

        if (TextEquals(text, Keyword.AdminResource))
        {
            return DropKindLengthIf(IsKeywordAt(second, Keyword.AdminPool), Len2);
        }

        return TextEquals(text, Keyword.AdminEvent)
            ? DropKindLengthIf(IsKeywordAt(second, Keyword.AdminSession) || IsKeywordAt(second, Keyword.AdminNotification), Len2)
            : null;
    }

    // FULLTEXT {CATALOG | INDEX | STOPLIST}, SEARCH PROPERTY LIST, EXTERNAL {LANGUAGE | LIBRARY | RESOURCE POOL}; null
    // for other words.
    private int? TsqlGapsFulltextDropKindLength(ReadOnlySpan<char> text, int second)
    {
        if (TextEquals(text, Keyword.AdminFulltext))
        {
            return DropKindLengthIf(
                IsKeywordAt(second, Keyword.Catalog) || IsKeywordAt(second, Keyword.Index) || IsKeywordAt(second, Keyword.AdminStoplist),
                Len2);
        }

        if (TextEquals(text, Keyword.Search))
        {
            return DropKindLengthIf(IsKeywordAt(second, Keyword.Property) && IsKeywordAt(second + 1, Keyword.AdminList), Len3);
        }

        return TextEquals(text, Keyword.External) ? TsqlGapsExternalDropKindLength(second) : null;
    }

    private int TsqlGapsExternalDropKindLength(int second)
    {
        if (IsKeywordAt(second, Keyword.AdminResource))
        {
            return DropKindLengthIf(IsKeywordAt(second + 1, Keyword.AdminPool), Len3);
        }

        return DropKindLengthIf(IsKeywordAt(second, Keyword.Language) || IsKeywordAt(second, Keyword.GapsLibrary), Len2);
    }

    private static int DropKindLengthIf(bool matches, int length) => matches ? length : 0;

    private bool IsGroupKindAt(int index) => index < _tokens.Count && _tokens[index].Kind == SyntaxKind.GroupKeyword;

    // DROP kind [IF EXISTS] name, ... [ON {SERVER | DATABASE | QUEUE queue}], or DROP FULLTEXT INDEX ON table.
    private TsqlDropObjectsStatement ParseTsqlGapsDrop(int kindLength)
    {
        var dropKeyword = Advance();
        var kind = TakeTsqlWords(kindLength);
        var ifExists = TryParseTsqlIfExists();
        var fulltextIndex = TokenEquals(kind[0], Keyword.AdminFulltext) && TokenEquals(kind[1], Keyword.Index);
        IReadOnlyList<IReadOnlyList<SyntaxToken>> names = fulltextIndex ? [] : ParseSchemaNameList();
        SyntaxToken? onKeyword = null;
        IReadOnlyList<SyntaxToken> scope = [];
        if (fulltextIndex || (_current.Kind == SyntaxKind.OnKeyword && TokenEquals(kind[0], Keyword.AdminEvent)))
        {
            onKeyword = Expect(SyntaxKind.OnKeyword);
            scope = fulltextIndex ? ParseQualifiedName() : ParseTsqlEventScope();
        }

        return new TsqlDropObjectsStatement
        {
            Span = SourceSpan.From(dropKeyword, TsqlLastConsumed()),
            DropKeyword = dropKeyword,
            ObjectKind = kind,
            IfExists = ifExists,
            Names = names,
            OnKeyword = onKeyword,
            Scope = scope,
        };
    }

    // The words after the names of DROP ASSEMBLY (WITH NO DEPENDENTS) and DROP SYMMETRIC KEY (REMOVE PROVIDER KEY).
    internal IReadOnlyList<SyntaxToken> ParseTsqlDropSecurableTail(List<SyntaxToken> kind)
    {
        if (_current.Kind == SyntaxKind.WithKeyword && NextEquals(Keyword.No) && TokenEquals(kind[0], Keyword.SecAssembly))
        {
            return [Advance(), Advance(), ExpectIdent(Keyword.GapsDependents)];
        }

        if (IdentifierEquals(Keyword.GapsRemove) && NextEquals(Keyword.GapsProvider) && TokenEquals(kind[0], Keyword.Symmetric))
        {
            return [Advance(), Advance(), ExpectIdent(Keyword.Key)];
        }

        return [];
    }

    // SERVER | DATABASE | QUEUE [schema.]queue: the scope of an event session or event notification.
    private List<SyntaxToken> ParseTsqlEventScope()
    {
        if (!IdentifierEquals(Keyword.AdminQueue))
        {
            return [IdentifierEquals(Keyword.AdminServer) ? Advance() : ExpectIdent(Keyword.AdminDatabase)];
        }

        var scope = new List<SyntaxToken> { Advance() };
        scope.AddRange(ParseQualifiedName());
        return scope;
    }
}
