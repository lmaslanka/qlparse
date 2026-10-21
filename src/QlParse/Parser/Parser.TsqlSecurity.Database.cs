namespace QlParse;

// SQL Server databases and schemas (workstream F): CREATE / ALTER DATABASE with files and filegroups,
// ALTER DATABASE SCOPED CONFIGURATION, CREATE SCHEMA with schema elements, ALTER SCHEMA ... TRANSFER and
// ALTER AUTHORIZATION.
internal sealed partial class Parser
{
    private readonly record struct TsqlDatabaseFiles(
        SyntaxToken? OnKeyword,
        List<TsqlDatabaseFilegroup>? Filegroups,
        SyntaxToken? LogKeyword,
        SyntaxToken? LogOnKeyword,
        List<TsqlObjectOptionList>? LogFiles);

    private readonly record struct TsqlDatabaseTail(
        SyntaxToken? ForKeyword,
        SyntaxToken? AttachKeyword,
        SyntaxToken? WithKeyword,
        List<TsqlObjectOption>? Options,
        SyntaxToken? AsKeyword,
        SyntaxToken? SnapshotKeyword,
        SyntaxToken? OfKeyword,
        SyntaxToken? Source);

    // CREATE DATABASE name [CONTAINMENT = {NONE | PARTIAL}] [ON [PRIMARY] file, ... [, FILEGROUP ...]] [LOG ON file, ...]
    // [COLLATE c] [FOR {ATTACH | ATTACH_REBUILD_LOG}] [WITH option, ...] [AS SNAPSHOT OF source]
    private TsqlCreateDatabaseStatement ParseTsqlCreateDatabase()
    {
        var create = Advance();
        var database = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var containment = IdentifierEquals(Keyword.SecContainment) ? ParseTsqlObjectOption() : null;
        var files = ParseTsqlDatabaseFiles();
        var collate = _current.Kind == SyntaxKind.CollateKeyword ? Advance() : (SyntaxToken?)null;
        var collation = collate is null ? (SyntaxToken?)null : Expect(SyntaxKind.Identifier);
        var tail = ParseTsqlDatabaseTail();
        var end = TsqlCreateDatabaseEnd(tail) ?? collation?.Span ?? TsqlDatabaseFilesEnd(files) ?? containment?.Span ?? name.Span;
        return new TsqlCreateDatabaseStatement
        {
            Span = SourceSpan.From(create, end),
            CreateKeyword = create,
            DatabaseKeyword = database,
            Name = name,
            Containment = containment,
            OnKeyword = files.OnKeyword,
            Filegroups = files.Filegroups ?? (IReadOnlyList<TsqlDatabaseFilegroup>)[],
            LogKeyword = files.LogKeyword,
            LogOnKeyword = files.LogOnKeyword,
            LogFiles = files.LogFiles ?? (IReadOnlyList<TsqlObjectOptionList>)[],
            CollateKeyword = collate,
            Collation = collation,
            ForKeyword = tail.ForKeyword,
            AttachKeyword = tail.AttachKeyword,
            WithKeyword = tail.WithKeyword,
            Options = tail.Options ?? (IReadOnlyList<TsqlObjectOption>)[],
            AsKeyword = tail.AsKeyword,
            SnapshotKeyword = tail.SnapshotKeyword,
            OfKeyword = tail.OfKeyword,
            Source = tail.Source,
        };
    }

    private static SourceSpan? TsqlCreateDatabaseEnd(TsqlDatabaseTail tail) =>
        tail.Source?.Span ?? (tail.Options is not null ? tail.Options[^1].Span : tail.AttachKeyword?.Span);

    private static SourceSpan? TsqlDatabaseFilesEnd(TsqlDatabaseFiles files)
    {
        if (files.LogFiles is not null)
        {
            return files.LogFiles[^1].Span;
        }

        return files.Filegroups is not null ? files.Filegroups[^1].Span : null;
    }

    // ON [PRIMARY] file, ... [, FILEGROUP name ... file, ...] ... [LOG ON file, ...]
    private TsqlDatabaseFiles ParseTsqlDatabaseFiles()
    {
        SyntaxToken? onKeyword = null;
        List<TsqlDatabaseFilegroup>? filegroups = null;
        if (_current.Kind == SyntaxKind.OnKeyword)
        {
            onKeyword = Advance();
            filegroups = [ParseTsqlPrimaryFilegroup()];
            while (_current.Kind == SyntaxKind.Comma && NextEquals(Keyword.SecFilegroup))
            {
                Advance();
                filegroups.Add(ParseTsqlFilegroup());
            }
        }

        if (!IdentifierEquals(Keyword.SecLog) || NextKind != SyntaxKind.OnKeyword)
        {
            return new TsqlDatabaseFiles(onKeyword, filegroups, null, null, null);
        }

        var log = Advance();
        var logOn = Advance();
        return new TsqlDatabaseFiles(onKeyword, filegroups, log, logOn, ParseTsqlDatabaseFileList());
    }

    private TsqlDatabaseFilegroup ParseTsqlPrimaryFilegroup()
    {
        var primary = IdentifierEquals(Keyword.Primary) ? Advance() : (SyntaxToken?)null;
        var files = ParseTsqlDatabaseFileList();
        return new TsqlDatabaseFilegroup
        {
            Span = SourceSpan.From(primary?.Span ?? files[0].Span, files[^1].Span),
            PrimaryKeyword = primary,
            Files = files,
        };
    }

    // FILEGROUP name [CONTAINS {FILESTREAM | MEMORY_OPTIMIZED_DATA}] [DEFAULT] file, ...
    private TsqlDatabaseFilegroup ParseTsqlFilegroup()
    {
        var filegroup = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var contains = IdentifierEquals(Keyword.Contains) ? Advance() : (SyntaxToken?)null;
        var containsKind = contains is null ? (SyntaxToken?)null : Expect(SyntaxKind.Identifier);
        var defaultKeyword = IdentifierEquals(Keyword.Default) ? Advance() : (SyntaxToken?)null;
        var files = ParseTsqlDatabaseFileList();
        return new TsqlDatabaseFilegroup
        {
            Span = SourceSpan.From(filegroup, files[^1].Span),
            FilegroupKeyword = filegroup,
            Name = name,
            ContainsKeyword = contains,
            ContainsKind = containsKind,
            DefaultKeyword = defaultKeyword,
            Files = files,
        };
    }

    // (NAME = n, FILENAME = 'f', SIZE = 10 MB, ...), ... — stops before `, FILEGROUP`.
    private List<TsqlObjectOptionList> ParseTsqlDatabaseFileList()
    {
        var files = new List<TsqlObjectOptionList> { ParseTsqlObjectOptionList() };
        while (_current.Kind == SyntaxKind.Comma && NextKind == SyntaxKind.OpenParen)
        {
            Advance();
            files.Add(ParseTsqlObjectOptionList());
        }

        return files;
    }

    // [FOR {ATTACH | ATTACH_REBUILD_LOG}] [WITH option, ...] [AS SNAPSHOT OF source]
    private TsqlDatabaseTail ParseTsqlDatabaseTail()
    {
        var forKeyword = _current.Kind == SyntaxKind.ForKeyword ? Advance() : (SyntaxToken?)null;
        var attach = forKeyword is null ? (SyntaxToken?)null : Expect(SyntaxKind.Identifier);
        var withKeyword = _current.Kind == SyntaxKind.WithKeyword ? Advance() : (SyntaxToken?)null;
        var options = withKeyword is null ? null : ParseTsqlObjectOptionItems();
        if (_current.Kind != SyntaxKind.AsKeyword)
        {
            return new TsqlDatabaseTail(forKeyword, attach, withKeyword, options, null, null, null, null);
        }

        var asKeyword = Advance();
        var snapshot = ExpectIdent(Keyword.GapSnapshot);
        var ofKeyword = Expect(SyntaxKind.OfKeyword);
        var source = Expect(SyntaxKind.Identifier);
        return new TsqlDatabaseTail(forKeyword, attach, withKeyword, options, asKeyword, snapshot, ofKeyword, source);
    }

    // ALTER DATABASE {name | CURRENT} {SET option, ... [WITH termination] | file / filegroup action | MODIFY NAME = n | COLLATE c}
    private TsqlAlterDatabaseStatement ParseTsqlAlterDatabase()
    {
        var alter = Advance();
        var database = Advance();
        var name = Expect(SyntaxKind.Identifier);
        if (_current.Kind == SyntaxKind.SetKeyword)
        {
            return ParseTsqlAlterDatabaseSet(alter, database, name);
        }

        if (_current.Kind == SyntaxKind.CollateKeyword)
        {
            var collate = Advance();
            var collation = Expect(SyntaxKind.Identifier);
            return new TsqlAlterDatabaseStatement
            {
                Span = SourceSpan.From(alter, collation),
                AlterKeyword = alter,
                DatabaseKeyword = database,
                Name = name,
                ActionKeywords = [collate],
                Target = collation,
            };
        }

        return ParseTsqlAlterDatabaseFiles(alter, database, name);
    }

    private TsqlAlterDatabaseStatement ParseTsqlAlterDatabaseSet(SyntaxToken alter, SyntaxToken database, SyntaxToken name)
    {
        var set = Advance();
        var options = ParseTsqlObjectOptionItems();
        var withKeyword = _current.Kind == SyntaxKind.WithKeyword ? Advance() : (SyntaxToken?)null;
        var termination = withKeyword is null ? null : ParseTsqlTermination();
        return new TsqlAlterDatabaseStatement
        {
            Span = SourceSpan.From(alter.Span, termination is not null ? termination[^1].Span : options[^1].Span),
            AlterKeyword = alter,
            DatabaseKeyword = database,
            Name = name,
            ActionKeywords = [set],
            Options = options,
            WithKeyword = withKeyword,
            Termination = termination ?? (IReadOnlyList<SyntaxToken>)[],
        };
    }

    // ROLLBACK AFTER n [SECONDS] | ROLLBACK IMMEDIATE | NO_WAIT
    private List<SyntaxToken> ParseTsqlTermination()
    {
        if (!IdentifierEquals(Keyword.Rollback))
        {
            return [ExpectIdent(Keyword.SecNoWait)];
        }

        var rollback = Advance();
        if (!IdentifierEquals(Keyword.After))
        {
            return [rollback, ExpectIdent(Keyword.Immediate)];
        }

        var after = Advance();
        var seconds = Expect(SyntaxKind.Number);
        return IdentifierEquals(Keyword.SecSeconds) ? [rollback, after, seconds, Advance()] : [rollback, after, seconds];
    }

    // {ADD | REMOVE | MODIFY} {FILE | LOG FILE | FILEGROUP | NAME} ...
    private TsqlAlterDatabaseStatement ParseTsqlAlterDatabaseFiles(SyntaxToken alter, SyntaxToken database, SyntaxToken name)
    {
        if (!IdentifierEquals(Keyword.Add) && !IdentifierEquals(Keyword.SecRemove) && !IdentifierEquals(Keyword.SecModify))
        {
            throw new SqlParseException($"Expected SET, ADD, REMOVE, MODIFY or COLLATE, found {_current.Kind}", _current.Position);
        }

        var verb = Advance();
        if (IdentifierEquals(Keyword.SecFilegroup))
        {
            return ParseTsqlAlterFilegroup(new TsqlAlterDatabaseHead(alter, database, name), verb);
        }

        if (IdentifierEquals(Keyword.Name))
        {
            var nameKeyword = Advance();
            var equalsToken = Expect(SyntaxKind.EqualsToken);
            var newName = Expect(SyntaxKind.Identifier);
            return new TsqlAlterDatabaseStatement
            {
                Span = SourceSpan.From(alter, newName),
                AlterKeyword = alter,
                DatabaseKeyword = database,
                Name = name,
                ActionKeywords = [verb, nameKeyword],
                EqualsToken = equalsToken,
                Target = newName,
            };
        }

        return ParseTsqlAlterDatabaseFile(new TsqlAlterDatabaseHead(alter, database, name), verb);
    }

    private readonly record struct TsqlAlterDatabaseHead(SyntaxToken Alter, SyntaxToken Database, SyntaxToken Name);

    private readonly record struct TsqlAlterDatabaseAction(
        List<SyntaxToken> Keywords,
        SourceSpan End,
        SyntaxToken? Target = null,
        List<TsqlObjectOptionList>? Files = null,
        SyntaxToken? ToKeyword = null,
        SyntaxToken? FilegroupKeyword = null,
        SyntaxToken? Filegroup = null,
        TsqlObjectOption? Property = null);

    // ADD [LOG] FILE file, ... [TO FILEGROUP fg] | REMOVE FILE name | MODIFY FILE file
    private TsqlAlterDatabaseStatement ParseTsqlAlterDatabaseFile(TsqlAlterDatabaseHead head, SyntaxToken verb)
    {
        List<SyntaxToken> keywords = IdentifierEquals(Keyword.SecLog) && TokenEquals(verb, Keyword.Add)
            ? [verb, Advance(), ExpectIdent(Keyword.SecFile)]
            : [verb, ExpectIdent(Keyword.SecFile)];
        if (TokenEquals(verb, Keyword.SecRemove))
        {
            var file = Expect(SyntaxKind.Identifier);
            return BuildTsqlAlterDatabase(head, new TsqlAlterDatabaseAction(keywords, file.Span, Target: file));
        }

        var files = TokenEquals(verb, Keyword.Add) ? ParseTsqlDatabaseFileList() : [ParseTsqlObjectOptionList()];
        if (!IdentifierEquals(Keyword.To) || !NextEquals(Keyword.SecFilegroup))
        {
            return BuildTsqlAlterDatabase(head, new TsqlAlterDatabaseAction(keywords, files[^1].Span, Files: files));
        }

        var to = Advance();
        var filegroupKeyword = Advance();
        var filegroup = Expect(SyntaxKind.Identifier);
        return BuildTsqlAlterDatabase(
            head,
            new TsqlAlterDatabaseAction(keywords, filegroup.Span, null, files, to, filegroupKeyword, filegroup));
    }

    // ADD FILEGROUP fg [CONTAINS {FILESTREAM | MEMORY_OPTIMIZED_DATA}] | REMOVE FILEGROUP fg |
    // MODIFY FILEGROUP fg {READ_ONLY | READ_WRITE | DEFAULT | NAME = new | AUTOGROW_SINGLE_FILE | AUTOGROW_ALL_FILES}
    private TsqlAlterDatabaseStatement ParseTsqlAlterFilegroup(TsqlAlterDatabaseHead head, SyntaxToken verb)
    {
        var filegroupKeyword = Advance();
        var filegroup = Expect(SyntaxKind.Identifier);
        var hasProperty = TokenEquals(verb, Keyword.Add) ? IdentifierEquals(Keyword.Contains) : TokenEquals(verb, Keyword.SecModify);
        var property = hasProperty ? ParseTsqlObjectOption() : null;
        var end = property?.Span ?? filegroup.Span;
        return BuildTsqlAlterDatabase(head, new TsqlAlterDatabaseAction([verb, filegroupKeyword], end, Target: filegroup, Property: property));
    }

    private static TsqlAlterDatabaseStatement BuildTsqlAlterDatabase(TsqlAlterDatabaseHead head, TsqlAlterDatabaseAction action) => new()
    {
        Span = SourceSpan.From(head.Alter, action.End),
        AlterKeyword = head.Alter,
        DatabaseKeyword = head.Database,
        Name = head.Name,
        ActionKeywords = action.Keywords,
        Target = action.Target,
        Files = action.Files ?? (IReadOnlyList<TsqlObjectOptionList>)[],
        ToKeyword = action.ToKeyword,
        FilegroupKeyword = action.FilegroupKeyword,
        Filegroup = action.Filegroup,
        Options = action.Property is null ? [] : [action.Property],
    };

    // ALTER DATABASE SCOPED CONFIGURATION [FOR SECONDARY] {SET name = value | CLEAR PROCEDURE_CACHE [plan_handle]}
    private TsqlAlterDatabaseScopedConfigurationStatement ParseTsqlAlterScopedConfiguration()
    {
        var keywords = TakeTsqlWords(Len4);
        var forKeyword = _current.Kind == SyntaxKind.ForKeyword ? Advance() : (SyntaxToken?)null;
        var secondary = forKeyword is null ? (SyntaxToken?)null : ExpectIdent(Keyword.SecSecondary);
        var action = _current.Kind == SyntaxKind.SetKeyword ? Advance() : ExpectIdent(Keyword.SecClear);
        var option = ParseTsqlObjectOption();
        return new TsqlAlterDatabaseScopedConfigurationStatement
        {
            Span = SourceSpan.From(keywords[0], option.Span),
            AlterKeyword = keywords[0],
            DatabaseKeyword = keywords[1],
            ScopedKeyword = keywords[2],
            ConfigurationKeyword = keywords[Len3],
            ForKeyword = forKeyword,
            SecondaryKeyword = secondary,
            ActionKeyword = action,
            Option = option,
        };
    }

    // CREATE SCHEMA {name [AUTHORIZATION owner] | AUTHORIZATION owner} [{CREATE TABLE | CREATE VIEW | GRANT | DENY | REVOKE} ...]
    private TsqlCreateSchemaStatement ParseTsqlCreateSchema()
    {
        var create = Advance();
        var schema = Advance();
        var name = IdentifierEquals(Keyword.Authorization) ? (SyntaxToken?)null : Expect(SyntaxKind.Identifier);
        var authorization = IdentifierEquals(Keyword.Authorization) ? Advance() : (SyntaxToken?)null;
        var owner = authorization is null ? (SyntaxToken?)null : ParseGrantee();
        List<Query>? elements = null;
        while (IsTsqlSchemaElementStart())
        {
            (elements ??= []).Add(ParseStatement());
        }

        var end = elements is not null ? elements[^1].Span : owner?.Span ?? name!.Value.Span;
        return new TsqlCreateSchemaStatement
        {
            Span = SourceSpan.From(create, end),
            CreateKeyword = create,
            SchemaKeyword = schema,
            Name = name,
            AuthorizationKeyword = authorization,
            Owner = owner,
            Elements = elements ?? (IReadOnlyList<Query>)[],
        };
    }

    private bool IsTsqlSchemaElementStart()
    {
        if (IdentifierEquals(Keyword.Create))
        {
            return NextEquals(Keyword.Table) || NextEquals(Keyword.View);
        }

        return IdentifierEquals(Keyword.Grant) || IdentifierEquals(Keyword.SecDeny) || IdentifierEquals(Keyword.Revoke);
    }

    // ALTER SCHEMA target TRANSFER [class ::] securable
    private TsqlAlterSchemaTransferStatement ParseTsqlAlterSchemaTransfer()
    {
        var alter = Advance();
        var schema = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var transfer = Advance();
        var securable = ParseTsqlSecurable();
        return new TsqlAlterSchemaTransferStatement
        {
            Span = SourceSpan.From(alter, securable.Span),
            AlterKeyword = alter,
            SchemaKeyword = schema,
            Name = name,
            TransferKeyword = transfer,
            Securable = securable,
        };
    }

    // ALTER AUTHORIZATION ON [class ::] entity TO {principal | SCHEMA OWNER}
    private TsqlAlterAuthorizationStatement ParseTsqlAlterAuthorization()
    {
        var alter = Advance();
        var authorization = Advance();
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var securable = ParseTsqlSecurable();
        var to = ExpectIdent(Keyword.To);
        if (IdentifierEquals(Keyword.Schema) && NextEquals(Keyword.SecOwner))
        {
            var schema = Advance();
            var owner = Advance();
            return new TsqlAlterAuthorizationStatement
            {
                Span = SourceSpan.From(alter, owner),
                AlterKeyword = alter,
                AuthorizationKeyword = authorization,
                OnKeyword = onKeyword,
                Securable = securable,
                ToKeyword = to,
                SchemaKeyword = schema,
                OwnerKeyword = owner,
            };
        }

        var principal = ParseGrantee();
        return new TsqlAlterAuthorizationStatement
        {
            Span = SourceSpan.From(alter, principal),
            AlterKeyword = alter,
            AuthorizationKeyword = authorization,
            OnKeyword = onKeyword,
            Securable = securable,
            ToKeyword = to,
            Principal = principal,
        };
    }
}
