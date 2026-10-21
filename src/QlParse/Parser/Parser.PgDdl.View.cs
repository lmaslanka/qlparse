namespace QlParse;

internal sealed partial class Parser
{
    internal readonly record struct PgViewHead(SyntaxToken? OrKeyword, SyntaxToken? ReplaceKeyword, SyntaxToken? TemporaryKeyword);

    // `CREATE [OR REPLACE] [TEMP | TEMPORARY] [RECURSIVE] VIEW`: the part between CREATE and RECURSIVE.
    internal PgViewHead ParsePgViewHead()
    {
        if (!IsPostgres)
        {
            return default;
        }

        SyntaxToken? orKeyword = null;
        SyntaxToken? replaceKeyword = null;
        if (_current.Kind == SyntaxKind.OrKeyword)
        {
            orKeyword = Advance();
            replaceKeyword = ExpectIdent(Keyword.DdlReplace);
        }

        var temporary = IdentifierEquals(Keyword.DdlTemp) || IdentifierEquals(Keyword.Temporary) ? Advance() : (SyntaxToken?)null;
        return new PgViewHead(orKeyword, replaceKeyword, temporary);
    }

    // Postgres allows a view column list without the ISO `) AS` lookahead, e.g. before WITH (...).
    internal bool IsPgViewColumnList() => IsPostgres && _current.Kind == SyntaxKind.OpenParen;

    // Null unless OR REPLACE, TEMPORARY or WITH (options) is present.
    internal static PgCreateViewExtras? BuildPgCreateViewExtras(PgViewHead head, SyntaxToken? optionsWith, StorageParameterList? options) =>
        head.OrKeyword is null && head.TemporaryKeyword is null && optionsWith is null
            ? null
            : new PgCreateViewExtras
            {
                OrKeyword = head.OrKeyword,
                ReplaceKeyword = head.ReplaceKeyword,
                TemporaryKeyword = head.TemporaryKeyword,
                OptionsWithKeyword = optionsWith,
                Options = options,
            };

    internal (SyntaxToken? WithKeyword, StorageParameterList? Options) TryParsePgViewOptions() =>
        IsPostgres ? TryParsePgWithParameters() : default;

    internal AlterViewRelationAction ParsePgAlterViewAction()
    {
        var action = TryParsePgDdlAlterTableAction()
            ?? throw new SqlParseException($"Expected ALTER VIEW action, found {_current.Kind}", _current.Position);
        return new AlterViewRelationAction { Span = action.Span, Action = action };
    }

    private readonly record struct PgMaterializedViewStorage(
        SyntaxToken? UsingKeyword,
        SyntaxToken? Method,
        SyntaxToken? WithKeyword,
        StorageParameterList? Options,
        SyntaxToken? TablespaceKeyword,
        SyntaxToken? Tablespace);

    // `CREATE MATERIALIZED VIEW [IF NOT EXISTS] name [(cols)] [USING method] [WITH (...)]
    //  [TABLESPACE ts] AS query [WITH [NO] DATA]`.
    internal CreateMaterializedViewStatement ParseCreateMaterializedView()
    {
        var createKeyword = Advance();
        var materializedKeyword = Advance();
        var viewKeyword = Advance();
        var ifNotExists = TryParsePgExistenceCheck();
        var name = ParseQualifiedName();
        SyntaxToken? columnsOpen = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        SyntaxToken? columnsClose = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            columnsOpen = Advance();
            columns = ParseNameList();
            columnsClose = Expect(SyntaxKind.CloseParen);
        }

        var storage = ParsePgMaterializedViewStorage();
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var query = ParseQuery();
        var data = ParsePgWithData();
        return new CreateMaterializedViewStatement
        {
            Span = SourceSpan.From(createKeyword, data.DataKeyword?.Span ?? query.Span),
            CreateKeyword = createKeyword,
            MaterializedKeyword = materializedKeyword,
            ViewKeyword = viewKeyword,
            IfNotExists = ifNotExists,
            Name = name,
            ColumnsOpen = columnsOpen,
            Columns = columns,
            ColumnsClose = columnsClose,
            UsingKeyword = storage.UsingKeyword,
            Method = storage.Method,
            OptionsWithKeyword = storage.WithKeyword,
            Options = storage.Options,
            TablespaceKeyword = storage.TablespaceKeyword,
            Tablespace = storage.Tablespace,
            AsKeyword = asKeyword,
            Query = query,
            WithKeyword = data.WithKeyword,
            NoKeyword = data.NoKeyword,
            DataKeyword = data.DataKeyword,
        };
    }

    private PgMaterializedViewStorage ParsePgMaterializedViewStorage()
    {
        var (usingKeyword, method) = TryParsePgUsingMethod();
        var (withKeyword, options) = TryParsePgWithParameters();
        var (tablespaceKeyword, tablespace) = TryParsePgTablespaceClause();
        return new PgMaterializedViewStorage(usingKeyword, method, withKeyword, options, tablespaceKeyword, tablespace);
    }

    internal readonly record struct PgWithData(SyntaxToken? WithKeyword, SyntaxToken? NoKeyword, SyntaxToken? DataKeyword);

    // Optional `WITH [NO] DATA`.
    internal PgWithData ParsePgWithData()
    {
        if (_current.Kind != SyntaxKind.WithKeyword)
        {
            return default;
        }

        ParseWithData(out var withKeyword, out var noKeyword, out var dataKeyword);
        return new PgWithData(withKeyword, noKeyword, dataKeyword);
    }

    // `REFRESH MATERIALIZED VIEW [CONCURRENTLY] name [WITH [NO] DATA]`.
    internal RefreshMaterializedViewStatement ParseRefreshMaterializedView()
    {
        var refreshKeyword = Advance();
        var materializedKeyword = Advance();
        var viewKeyword = ExpectIdent(Keyword.View);
        var concurrently = IdentifierEquals(Keyword.Concurrently) ? Advance() : (SyntaxToken?)null;
        var name = ParseQualifiedName();
        var data = ParsePgWithData();
        return new RefreshMaterializedViewStatement
        {
            Span = SourceSpan.From(refreshKeyword, data.DataKeyword ?? name[^1]),
            RefreshKeyword = refreshKeyword,
            MaterializedKeyword = materializedKeyword,
            ViewKeyword = viewKeyword,
            ConcurrentlyKeyword = concurrently,
            Name = name,
            WithKeyword = data.WithKeyword,
            NoKeyword = data.NoKeyword,
            DataKeyword = data.DataKeyword,
        };
    }

    private readonly record struct PgRuleActions(
        SyntaxToken? Kind,
        SyntaxToken? NothingKeyword,
        SyntaxToken? OpenParen,
        IReadOnlyList<Query> Commands,
        IReadOnlyList<SyntaxToken> Semicolons,
        SyntaxToken? CloseParen,
        SourceSpan End);

    // `CREATE [OR REPLACE] RULE name AS ON event TO table [WHERE condition]
    //  DO [ALSO | INSTEAD] {NOTHING | command | (command , command ...)}`.
    internal CreateRuleStatement ParseCreateRule()
    {
        var createKeyword = Advance();
        var orKeyword = _current.Kind == SyntaxKind.OrKeyword ? Advance() : (SyntaxToken?)null;
        var replaceKeyword = orKeyword is null ? (SyntaxToken?)null : ExpectIdent(Keyword.DdlReplace);
        var ruleKeyword = ExpectIdent(Keyword.DdlRule);
        var name = Expect(SyntaxKind.Identifier);
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        if (_current.Kind is not (SyntaxKind.SelectKeyword or SyntaxKind.UpdateKeyword)
            && !IdentifierEquals(Keyword.Insert)
            && !IdentifierEquals(Keyword.Delete))
        {
            throw new SqlParseException($"Expected SELECT, INSERT, UPDATE, or DELETE, found {_current.Kind}", _current.Position);
        }

        var ruleEvent = Advance();
        var toKeyword = ExpectIdent(Keyword.To);
        var table = ParseQualifiedName();
        var whereKeyword = _current.Kind == SyntaxKind.WhereKeyword ? Advance() : (SyntaxToken?)null;
        var where = whereKeyword is null ? null : ParseExpression();
        var doKeyword = ExpectIdent(Keyword.Do);
        var actions = ParsePgRuleActions();
        return new CreateRuleStatement
        {
            Span = SourceSpan.From(createKeyword, actions.End),
            CreateKeyword = createKeyword,
            OrKeyword = orKeyword,
            ReplaceKeyword = replaceKeyword,
            RuleKeyword = ruleKeyword,
            Name = name,
            AsKeyword = asKeyword,
            OnKeyword = onKeyword,
            Event = ruleEvent,
            ToKeyword = toKeyword,
            Table = table,
            WhereKeyword = whereKeyword,
            Where = where,
            DoKeyword = doKeyword,
            Kind = actions.Kind,
            NothingKeyword = actions.NothingKeyword,
            OpenParen = actions.OpenParen,
            Commands = actions.Commands,
            Semicolons = actions.Semicolons,
            CloseParen = actions.CloseParen,
        };
    }

    private PgRuleActions ParsePgRuleActions()
    {
        var kind = IdentifierEquals(Keyword.DdlAlso) || IdentifierEquals(Keyword.Instead) ? Advance() : (SyntaxToken?)null;
        if (IdentifierEquals(Keyword.DdlNothing))
        {
            var nothing = Advance();
            return new PgRuleActions(kind, nothing, null, [], [], null, nothing.Span);
        }

        if (_current.Kind != SyntaxKind.OpenParen)
        {
            var command = ParseStatement();
            return new PgRuleActions(kind, null, null, [command], [], null, command.Span);
        }

        var openParen = Advance();
        var commands = new List<Query>();
        var semicolons = new List<SyntaxToken>();
        while (_current.Kind != SyntaxKind.CloseParen)
        {
            if (_current.Kind == SyntaxKind.Semicolon)
            {
                semicolons.Add(Advance());
                continue;
            }

            commands.Add(ParseStatement());
            if (_current.Kind != SyntaxKind.Semicolon && _current.Kind != SyntaxKind.CloseParen)
            {
                throw new SqlParseException($"Expected ; or ), found {_current.Kind}", _current.Position);
            }
        }

        var closeParen = Advance();
        return new PgRuleActions(kind, null, openParen, commands, semicolons, closeParen, closeParen.Span);
    }

    // `DROP RULE [IF EXISTS] name ON table [CASCADE | RESTRICT]`.
    internal DropRuleStatement ParseDropRule()
    {
        var dropKeyword = Advance();
        var ruleKeyword = Advance();
        var ifExists = TryParsePgExistenceCheck();
        var name = Expect(SyntaxKind.Identifier);
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var table = ParseQualifiedName();
        var behavior = TryParseDropBehavior();
        return new DropRuleStatement
        {
            Span = SourceSpan.From(dropKeyword, behavior ?? table[^1]),
            DropKeyword = dropKeyword,
            RuleKeyword = ruleKeyword,
            IfExists = ifExists,
            Name = name,
            OnKeyword = onKeyword,
            Table = table,
            Behavior = behavior,
        };
    }
}
