namespace QlParse;

internal sealed partial class Parser
{
    internal readonly record struct PgIndexHead(SyntaxToken? Concurrently, ExistenceCheck? IfNotExists, bool OmitsName);

    internal readonly record struct PgOperatorClass(
        IReadOnlyList<SyntaxToken>? Name,
        StorageParameterList? Parameters,
        SourceSpan? End);

    // `CREATE INDEX [CONCURRENTLY] [[IF NOT EXISTS] name] ON ...`: the name is optional in Postgres.
    internal PgIndexHead ParsePgIndexHead()
    {
        if (!IsPostgres)
        {
            return default;
        }

        var concurrently = IdentifierEquals(Keyword.Concurrently) ? Advance() : (SyntaxToken?)null;
        var ifNotExists = TryParsePgExistenceCheck();
        return new PgIndexHead(concurrently, ifNotExists, ifNotExists is null && _current.Kind == SyntaxKind.OnKeyword);
    }

    // Null unless at least one Postgres-only CREATE INDEX clause is present.
    internal static PgCreateIndexExtras? BuildPgCreateIndexExtras(
        PgIndexHead head,
        SyntaxToken? only,
        SyntaxToken? usingKeyword,
        SyntaxToken? method,
        IndexParameters? options)
    {
        if (head.Concurrently is null && head.IfNotExists is null && !HasPgIndexClauses(only, usingKeyword, options))
        {
            return null;
        }

        return new PgCreateIndexExtras
        {
            ConcurrentlyKeyword = head.Concurrently,
            IfNotExists = head.IfNotExists,
            OnlyKeyword = only,
            UsingKeyword = usingKeyword,
            Method = method,
            Options = options,
        };
    }

    private static bool HasPgIndexClauses(SyntaxToken? only, SyntaxToken? usingKeyword, IndexParameters? options) =>
        only is not null || usingKeyword is not null || options is not null;

    internal SyntaxToken? TryParsePgOnly() =>
        IsPostgres && _current.Kind == SyntaxKind.OnlyKeyword ? Advance() : null;

    internal (SyntaxToken? Keyword, SyntaxToken? Method) TryParsePgIndexMethod() =>
        IsPostgres ? TryParsePgUsingMethod() : default;

    internal IndexParameters? TryParsePgIndexOptions() =>
        IsPostgres ? TryParsePgIndexParameters() : null;

    // `column opclass [(opclass_parameter = value, ...)]`, NULLS stays the ISO null-ordering keyword.
    internal PgOperatorClass ParsePgOperatorClass()
    {
        if (_current.Kind != SyntaxKind.Identifier || IdentifierEquals(Keyword.Nulls))
        {
            return default;
        }

        var name = ParseQualifiedName();
        var parameters = _current.Kind == SyntaxKind.OpenParen ? ParseStorageParameterList() : null;
        return new PgOperatorClass(name, parameters, parameters?.Span ?? name[^1].Span);
    }

    // ALTER INDEX actions other than ISO RENAME / ON: `SET TABLESPACE`, `SET (...)`, `RESET (...)`,
    // `ATTACH PARTITION`, `ALTER [COLUMN] n SET STATISTICS`, `SET SCHEMA`, `OWNER TO`, `RENAME TO`.
    internal AlterIndexStatement ParsePgAlterIndexAction(
        SyntaxToken alterKeyword,
        SyntaxToken indexKeyword,
        ExistenceCheck? ifExists,
        IReadOnlyList<SyntaxToken> name)
    {
        var action = IdentifierEquals(Keyword.Alter) ? ParseAlterColumn() : TryParsePgDdlAlterTableAction();
        if (action is null)
        {
            throw new SqlParseException($"Expected ALTER INDEX action, found {_current.Kind}", _current.Position);
        }

        return new AlterIndexStatement
        {
            Span = SourceSpan.From(alterKeyword, action.Span),
            AlterKeyword = alterKeyword,
            IndexKeyword = indexKeyword,
            Name = name,
            Postgres = new PgAlterIndexExtras { IfExists = ifExists, RelationAction = action },
        };
    }
}
