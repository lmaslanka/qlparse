namespace QlParse;

internal sealed partial class Parser
{
    // `IF EXISTS` / `IF NOT EXISTS`, returns null (without consuming) when the current token is not IF.
    internal ExistenceCheck? TryParsePgExistenceCheck()
    {
        if (!IdentifierEquals(Keyword.If))
        {
            return null;
        }

        var hasNot = NextKind == SyntaxKind.NotKeyword;
        var existsIndex = hasNot ? _index + 1 : _index;
        if (existsIndex >= _tokens.Count || _tokens[existsIndex].Kind != SyntaxKind.ExistsKeyword)
        {
            return null;
        }

        var ifKeyword = Advance();
        var notKeyword = hasNot ? Advance() : (SyntaxToken?)null;
        var existsKeyword = Advance();
        return new ExistenceCheck
        {
            Span = SourceSpan.From(ifKeyword, existsKeyword),
            IfKeyword = ifKeyword,
            NotKeyword = notKeyword,
            ExistsKeyword = existsKeyword,
        };
    }

    internal ExistenceCheck? TryParsePgIfExists() =>
        IsPostgres ? TryParsePgExistenceCheck() : null;

    // ISO requires CASCADE or RESTRICT, Postgres makes it optional.
    internal SyntaxToken? ParseOptionalDropBehavior() =>
        IsPostgres ? TryParseDropBehavior() : ParseDropBehavior();

    internal SyntaxToken? TryParseDropBehavior() =>
        IdentifierEquals(Keyword.Cascade) || IdentifierEquals(Keyword.Restrict) ? Advance() : null;

    // `( name [= value] [, ...] )`: storage parameters, view options, opclass parameters,
    // hash partition bounds and utility options all share this shape.
    internal StorageParameterList ParseStorageParameterList()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var parameters = new List<StorageParameter> { ParseStorageParameter() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            parameters.Add(ParseStorageParameter());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new StorageParameterList
        {
            Span = SourceSpan.From(openParen, closeParen),
            OpenParen = openParen,
            Parameters = parameters,
            CloseParen = closeParen,
        };
    }

    private StorageParameter ParseStorageParameter()
    {
        var name = new List<SyntaxToken> { ExpectWord() };
        while (_current.Kind == SyntaxKind.Dot)
        {
            Advance();
            name.Add(ExpectWord());
        }

        var equalsToken = _current.Kind == SyntaxKind.EqualsToken ? Advance() : (SyntaxToken?)null;
        var sign = _current.Kind is SyntaxKind.MinusToken or SyntaxKind.PlusToken ? Advance() : (SyntaxToken?)null;
        SyntaxToken? value = null;
        if (sign is not null || equalsToken is not null || IsParameterValueKind(_current.Kind))
        {
            value = ExpectParameterValue();
        }

        var end = value ?? name[^1];
        return new StorageParameter
        {
            Span = SourceSpan.From(name[0], end),
            Name = name,
            EqualsToken = equalsToken,
            Sign = sign,
            Value = value,
        };
    }

    private SyntaxToken ExpectWord()
    {
        if (!IsWordKind(_current.Kind))
        {
            throw new SqlParseException($"Expected name, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    private SyntaxToken ExpectParameterValue()
    {
        if (!IsParameterValueKind(_current.Kind))
        {
            throw new SqlParseException($"Expected value, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    internal static bool IsWordKind(SyntaxKind kind) =>
        kind == SyntaxKind.Identifier || kind is >= SyntaxKind.SelectKeyword and <= SyntaxKind.WidthBucketKeyword;

    private static bool IsParameterValueKind(SyntaxKind kind) =>
        IsWordKind(kind) || kind is SyntaxKind.Number or SyntaxKind.String;

    // `WITH ( storage_parameter [= value] [, ...] )`, WITH must be followed by an open paren.
    internal bool IsPgWithParameters() =>
        _current.Kind == SyntaxKind.WithKeyword && NextKind == SyntaxKind.OpenParen;

    internal (SyntaxToken? WithKeyword, StorageParameterList? Parameters) TryParsePgWithParameters()
    {
        if (!IsPgWithParameters())
        {
            return (null, null);
        }

        var withKeyword = Advance();
        return (withKeyword, ParseStorageParameterList());
    }

    internal (SyntaxToken? Keyword, SyntaxToken? Name) TryParsePgTablespaceClause()
    {
        if (!IdentifierEquals(Keyword.DdlTablespace))
        {
            return (null, null);
        }

        var tablespaceKeyword = Advance();
        return (tablespaceKeyword, Expect(SyntaxKind.Identifier));
    }

    internal (SyntaxToken? Keyword, SyntaxToken? Method) TryParsePgUsingMethod()
    {
        if (_current.Kind != SyntaxKind.UsingKeyword)
        {
            return (null, null);
        }

        var usingKeyword = Advance();
        return (usingKeyword, Expect(SyntaxKind.Identifier));
    }

    internal readonly record struct PgDropTargets(
        ExistenceCheck? IfExists,
        IReadOnlyList<SyntaxToken> Name,
        IReadOnlyList<IReadOnlyList<SyntaxToken>> AdditionalNames,
        SyntaxToken? Behavior,
        SourceSpan End);

    // `[IF EXISTS] name [, ...] [CASCADE | RESTRICT]`: the shared tail of every Postgres DROP.
    internal PgDropTargets ParsePgDropTargets()
    {
        var ifExists = TryParsePgExistenceCheck();
        var name = ParseQualifiedName();
        IReadOnlyList<IReadOnlyList<SyntaxToken>> additionalNames = [];
        if (_current.Kind == SyntaxKind.Comma)
        {
            var names = new List<IReadOnlyList<SyntaxToken>>();
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                names.Add(ParseQualifiedName());
            }

            additionalNames = names;
        }

        var behavior = TryParseDropBehavior();
        var lastName = additionalNames.Count > 0 ? additionalNames[^1] : name;
        var end = behavior?.Span ?? lastName[^1].Span;
        return new PgDropTargets(ifExists, name, additionalNames, behavior, end);
    }

    // IF EXISTS or a second name; null otherwise.
    internal static PgDropExtras? BuildPgDropExtras(PgDropTargets targets) =>
        HasPgDropExtras(targets) ? new PgDropExtras { IfExists = targets.IfExists, AdditionalNames = targets.AdditionalNames } : null;

    private static bool HasPgDropExtras(PgDropTargets targets) =>
        targets.IfExists is not null || targets.AdditionalNames.Count > 0;

    internal DropTableStatement ParsePgDropTable(SyntaxToken dropKeyword, SyntaxToken tableKeyword)
    {
        var targets = ParsePgDropTargets();
        return new DropTableStatement
        {
            Span = SourceSpan.From(dropKeyword, targets.End),
            DropKeyword = dropKeyword,
            TableKeyword = tableKeyword,
            Name = targets.Name,
            Behavior = targets.Behavior,
            Postgres = BuildPgDropExtras(targets),
        };
    }

    internal DropViewStatement ParsePgDropView(SyntaxToken dropKeyword, SyntaxToken? materializedKeyword, SyntaxToken viewKeyword)
    {
        var targets = ParsePgDropTargets();
        return new DropViewStatement
        {
            Span = SourceSpan.From(dropKeyword, targets.End),
            DropKeyword = dropKeyword,
            ViewKeyword = viewKeyword,
            Name = targets.Name,
            Behavior = targets.Behavior,
            Postgres = materializedKeyword is null && !HasPgDropExtras(targets)
                ? null
                : new PgDropViewExtras
                {
                    MaterializedKeyword = materializedKeyword,
                    IfExists = targets.IfExists,
                    AdditionalNames = targets.AdditionalNames,
                },
        };
    }

    internal DropSequenceStatement ParsePgDropSequence(SyntaxToken dropKeyword, SyntaxToken sequenceKeyword)
    {
        var targets = ParsePgDropTargets();
        return new DropSequenceStatement
        {
            Span = SourceSpan.From(dropKeyword, targets.End),
            DropKeyword = dropKeyword,
            SequenceKeyword = sequenceKeyword,
            Name = targets.Name,
            Behavior = targets.Behavior,
            Postgres = BuildPgDropExtras(targets),
        };
    }

    internal DropIndexStatement ParsePgDropIndex(SyntaxToken dropKeyword, SyntaxToken indexKeyword)
    {
        var concurrently = IdentifierEquals(Keyword.Concurrently) ? Advance() : (SyntaxToken?)null;
        var targets = ParsePgDropTargets();
        var behavior = targets.Behavior;
        var end = targets.End;
        SyntaxToken? onKeyword = null;
        IReadOnlyList<SyntaxToken>? tableName = null;
        if (behavior is null && _current.Kind == SyntaxKind.OnKeyword)
        {
            // The ISO grammar's `DROP INDEX name ON table` form keeps parsing under Postgres.
            onKeyword = Advance();
            var table = ParseQualifiedName();
            tableName = table;
            behavior = TryParseDropBehavior();
            end = behavior?.Span ?? table[^1].Span;
        }

        return new DropIndexStatement
        {
            Span = SourceSpan.From(dropKeyword, end),
            DropKeyword = dropKeyword,
            IndexKeyword = indexKeyword,
            Name = targets.Name,
            OnKeyword = onKeyword,
            TableName = tableName,
            Behavior = behavior,
            Postgres = concurrently is null && !HasPgDropExtras(targets)
                ? null
                : new PgDropIndexExtras
                {
                    ConcurrentlyKeyword = concurrently,
                    IfExists = targets.IfExists,
                    AdditionalNames = targets.AdditionalNames,
                },
        };
    }

    // Postgres object-kind persistence word before TABLE / SEQUENCE / VIEW.
    internal SyntaxToken? TryParsePgPersistence() =>
        IdentifierEquals(Keyword.DdlTemp) || IdentifierEquals(Keyword.Temporary) || IdentifierEquals(Keyword.Unlogged)
            ? Advance()
            : null;

    internal SyntaxToken? PgTemporaryOf(SyntaxToken? persistence) =>
        persistence is { } token && !TokenEquals(token, Keyword.Unlogged) ? token : null;

    internal SyntaxToken? PgUnloggedOf(SyntaxToken? persistence) =>
        persistence is { } token && TokenEquals(token, Keyword.Unlogged) ? token : null;
}
