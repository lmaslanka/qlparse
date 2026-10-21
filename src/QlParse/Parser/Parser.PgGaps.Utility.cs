namespace QlParse;

// Postgres utility/security gaps: multiple role-grant options, ALTER GROUP ADD/DROP USER,
// SET TRANSACTION SNAPSHOT, SET ... FROM CURRENT, the old CLUSTER index ON table form, and
// ALTER DEFAULT PRIVILEGES with its FOR / IN clauses in either order.
internal sealed partial class Parser
{
    // `, {ADMIN | INHERIT | SET} {OPTION | TRUE | FALSE} ...` after a role grant's first WITH option.
    internal IReadOnlyList<PgRoleGrantOption> ParsePgRoleGrantOptions()
    {
        var options = new List<PgRoleGrantOption>();
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            var (kind, value) = ParsePgRoleGrantKindAndValue();
            options.Add(new PgRoleGrantOption { Span = SourceSpan.From(kind, value), Kind = kind, Value = value });
        }

        return options;
    }

    internal (SyntaxToken Kind, SyntaxToken Value) ParsePgRoleGrantKindAndValue()
    {
        if (!IdentifierEquals(Keyword.Admin) && !IdentifierEquals(Keyword.UtilInherit) && _current.Kind != SyntaxKind.SetKeyword)
        {
            throw new SqlParseException($"Expected ADMIN, INHERIT, or SET, found {_current.Kind}", _current.Position);
        }

        var kind = Advance();
        if (!IdentifierEquals(Keyword.Option) && _current.Kind is not (SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword))
        {
            throw new SqlParseException($"Expected OPTION, TRUE, or FALSE, found {_current.Kind}", _current.Position);
        }

        return (kind, Advance());
    }

    // ALTER GROUP role {ADD | DROP} USER ...: the verb follows the single-token role name.
    internal bool IsPgAlterGroupMembers() =>
        IsKeywordAt(_index + Len2, Keyword.Add) || IsKeywordAt(_index + Len2, Keyword.Drop);

    // ALTER GROUP role {ADD | DROP} USER name [, ...], kept as an ALTER ROLE whose single option
    // has the keywords {ADD | DROP} USER and the user names as its roles.
    internal AlterRoleStatement ParsePgAlterGroupMembers()
    {
        var alterKeyword = Advance();
        var groupKeyword = Advance();
        var name = ParseGrantee();
        var verb = Advance();
        var userKeyword = Expect(SyntaxKind.UserKeyword);
        var users = ParseGrantees();
        var option = new KeywordOption
        {
            Span = SourceSpan.From(verb, users[^1]),
            Keywords = [verb, userKeyword],
            Roles = users,
        };
        return new AlterRoleStatement
        {
            Span = SourceSpan.From(alterKeyword, users[^1]),
            AlterKeyword = alterKeyword,
            RoleKeyword = groupKeyword,
            Name = name,
            Options = [option],
        };
    }

    // SET TRANSACTION SNAPSHOT 'snapshot_id'
    internal SetTransactionStatement ParsePgSetTransactionSnapshot(
        SyntaxToken setKeyword,
        SyntaxToken? localKeyword,
        SyntaxToken transactionKeyword)
    {
        var snapshotKeyword = Advance();
        var snapshotId = Expect(SyntaxKind.String);
        return new SetTransactionStatement
        {
            Span = SourceSpan.From(setKeyword, snapshotId),
            SetKeyword = setKeyword,
            LocalKeyword = localKeyword,
            TransactionKeyword = transactionKeyword,
            Modes = [],
            Postgres = new PgSetTransactionExtras { SnapshotKeyword = snapshotKeyword, SnapshotId = snapshotId },
        };
    }

    // SET [SESSION | LOCAL] name FROM CURRENT, also the SET action of ALTER ROLE / ALTER DATABASE.
    internal SetConfigurationStatement ParsePgSetFromCurrent(SyntaxToken setKeyword, SyntaxToken? scope, IReadOnlyList<SyntaxToken> name)
    {
        var fromKeyword = Advance();
        var currentKeyword = ExpectIdent(Keyword.Current);
        return new SetConfigurationStatement
        {
            Span = SourceSpan.From(setKeyword, currentKeyword),
            SetKeyword = setKeyword,
            ScopeKeyword = scope,
            Name = name,
            AssignmentToken = fromKeyword,
            CurrentKeyword = currentKeyword,
        };
    }

    internal readonly record struct ClusterTarget(
        IReadOnlyList<SyntaxToken>? Table,
        SyntaxToken? UsingKeyword,
        SyntaxToken? Index,
        SyntaxToken? OnKeyword,
        SourceSpan? End);

    // `table [USING index]`, or the pre-8.3 `index ON table`.
    internal ClusterTarget ParseClusterTarget()
    {
        var name = ParseQualifiedName();
        if (_current.Kind == SyntaxKind.UsingKeyword)
        {
            var usingKeyword = Advance();
            var index = Expect(SyntaxKind.Identifier);
            return new ClusterTarget(name, usingKeyword, index, null, index.Span);
        }

        if (_current.Kind != SyntaxKind.OnKeyword || name.Count != 1)
        {
            return new ClusterTarget(name, null, null, null, name[^1].Span);
        }

        var onKeyword = Advance();
        var table = ParseQualifiedName();
        return new ClusterTarget(table, null, name[0], onKeyword, table[^1].Span);
    }

    internal readonly record struct DefaultPrivilegesRoles(
        SyntaxToken? ForKeyword,
        SyntaxToken? RoleKeyword,
        IReadOnlyList<SyntaxToken> TargetRoles);

    // [FOR {ROLE | USER} role [, ...]]
    internal DefaultPrivilegesRoles ParseDefaultPrivilegesRoles()
    {
        if (_current.Kind != SyntaxKind.ForKeyword)
        {
            return new DefaultPrivilegesRoles(null, null, []);
        }

        var forKeyword = Advance();
        var roleKeyword = _current.Kind == SyntaxKind.UserKeyword ? Advance() : ExpectIdent(Keyword.Role);
        return new DefaultPrivilegesRoles(forKeyword, roleKeyword, ParseGrantees());
    }

    internal readonly record struct DefaultPrivilegesSchemas(
        SyntaxToken? InKeyword,
        SyntaxToken? SchemaKeyword,
        IReadOnlyList<IReadOnlyList<SyntaxToken>> Schemas);

    // [IN SCHEMA schema [, ...]]
    internal DefaultPrivilegesSchemas ParseDefaultPrivilegesSchemas()
    {
        if (_current.Kind != SyntaxKind.InKeyword)
        {
            return new DefaultPrivilegesSchemas(null, null, []);
        }

        var inKeyword = Advance();
        var schemaKeyword = ExpectIdent(Keyword.Schema);
        return new DefaultPrivilegesSchemas(inKeyword, schemaKeyword, ParseSchemaNameList());
    }
}
