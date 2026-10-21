namespace QlParse;

// Postgres DDL gaps: ALTER CONSTRAINT, SET ACCESS METHOD, OF type / NOT OF, SET WITHOUT OIDS,
// column STORAGE / COMPRESSION, `WITH OPERATOR(schema.op)` exclusion elements,
// ALTER INDEX / MATERIALIZED VIEW ALL IN TABLESPACE, and the Postgres schema statement forms.
internal sealed partial class Parser
{
    // ALTER CONSTRAINT name [[NOT] DEFERRABLE] [INITIALLY {DEFERRED | IMMEDIATE}]
    internal AlterConstraintAction ParsePgAlterConstraint()
    {
        var alterKeyword = Advance();
        var constraintKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        ParseConstraintCharacteristics(out var deferrableNot, out var deferrable, out var initially, out var initiallyWhen);
        return new AlterConstraintAction
        {
            Span = SourceSpan.From(alterKeyword, initiallyWhen ?? deferrable ?? name),
            AlterKeyword = alterKeyword,
            ConstraintKeyword = constraintKeyword,
            Name = name,
            DeferrableNotKeyword = deferrableNot,
            DeferrableKeyword = deferrable,
            InitiallyKeyword = initially,
            InitiallyWhen = initiallyWhen,
        };
    }

    // `OF type_name` | `NOT OF`
    internal TypedTableAction? TryParsePgTypedTableAction()
    {
        if (_current.Kind == SyntaxKind.OfKeyword)
        {
            var ofKeyword = Advance();
            var typeName = ParseQualifiedName();
            return new TypedTableAction
            {
                Span = SourceSpan.From(ofKeyword, typeName[^1]),
                OfKeyword = ofKeyword,
                TypeName = typeName,
            };
        }

        if (_current.Kind != SyntaxKind.NotKeyword || NextKind != SyntaxKind.OfKeyword)
        {
            return null;
        }

        var notKeyword = Advance();
        var notOfKeyword = Advance();
        return new TypedTableAction
        {
            Span = SourceSpan.From(notKeyword, notOfKeyword),
            NotKeyword = notKeyword,
            OfKeyword = notOfKeyword,
        };
    }

    // `SET ACCESS METHOD method` | `SET WITHOUT OIDS`
    internal AlterTableAction? TryParsePgSetAccessMethodOrOids()
    {
        if (NextEquals(Keyword.PgAccess) && IsKeywordAt(_index + 1, Keyword.Method))
        {
            var setKeyword = Advance();
            var accessKeyword = Advance();
            var methodKeyword = Advance();
            var method = Expect(SyntaxKind.Identifier);
            return new SetAccessMethodAction
            {
                Span = SourceSpan.From(setKeyword, method),
                SetKeyword = setKeyword,
                AccessKeyword = accessKeyword,
                MethodKeyword = methodKeyword,
                Method = method,
            };
        }

        if (!NextEquals(Keyword.Without) || !IsKeywordAt(_index + 1, Keyword.Oids))
        {
            return null;
        }

        var set = Advance();
        var withoutKeyword = Advance();
        var oidsKeyword = Advance();
        return new SetWithoutOidsAction
        {
            Span = SourceSpan.From(set, oidsKeyword),
            SetKeyword = set,
            WithoutKeyword = withoutKeyword,
            OidsKeyword = oidsKeyword,
        };
    }

    internal readonly record struct PgColumnStorage(
        SyntaxToken? StorageKeyword,
        SyntaxToken? StorageMode,
        SyntaxToken? CompressionKeyword,
        SyntaxToken? CompressionMethod,
        SourceSpan? End)
    {
        public PgColumnExtras? Extras => StorageKeyword is null && CompressionKeyword is null
            ? null
            : new PgColumnExtras
            {
                StorageKeyword = StorageKeyword,
                StorageMode = StorageMode,
                CompressionKeyword = CompressionKeyword,
                CompressionMethod = CompressionMethod,
            };
    }

    // `[STORAGE mode] [COMPRESSION method]` right after a column's data type (either order).
    internal PgColumnStorage ParsePgColumnStorage()
    {
        var result = default(PgColumnStorage);
        while (true)
        {
            if (result.StorageKeyword is null && IdentifierEquals(Keyword.DdlStorage))
            {
                var storageKeyword = Advance();
                var mode = Expect(SyntaxKind.Identifier);
                result = result with { StorageKeyword = storageKeyword, StorageMode = mode, End = mode.Span };
                continue;
            }

            if (result.CompressionKeyword is not null || !IdentifierEquals(Keyword.Compression))
            {
                return result;
            }

            var compressionKeyword = Advance();
            var method = Expect(SyntaxKind.Identifier);
            result = result with { CompressionKeyword = compressionKeyword, CompressionMethod = method, End = method.Span };
        }
    }

    // `element WITH OPERATOR(schema.op)` in an exclusion constraint.
    internal ExclusionElement ParsePgQualifiedExclusionElement(IndexColumn column, SyntaxToken withKeyword)
    {
        var operatorName = ParsePgOperatorName();
        return new ExclusionElement
        {
            Span = SourceSpan.From(column.Span, operatorName.Span),
            Column = column,
            WithKeyword = withKeyword,
            Operator = operatorName.Operator,
            QualifiedOperator = operatorName,
        };
    }

    // ALTER INDEX ALL IN TABLESPACE name [OWNED BY role [, ...]] SET TABLESPACE new [NOWAIT]
    internal AlterIndexStatement ParsePgAlterIndexAllInTablespace(SyntaxToken alterKeyword, SyntaxToken indexKeyword)
    {
        var allInTablespace = ParsePgAllInTablespace();
        var action = TryParsePgDdlAlterTableAction()
            ?? throw new SqlParseException($"Expected ALTER INDEX action, found {_current.Kind}", _current.Position);
        return new AlterIndexStatement
        {
            Span = SourceSpan.From(alterKeyword, action.Span),
            AlterKeyword = alterKeyword,
            IndexKeyword = indexKeyword,
            Name = [],
            AllInTablespace = allInTablespace,
            RelationAction = action,
        };
    }

    // ALTER MATERIALIZED VIEW ALL IN TABLESPACE name [OWNED BY role [, ...]] SET TABLESPACE new [NOWAIT]
    internal AlterViewStatement ParsePgAlterMaterializedViewAllInTablespace(
        SyntaxToken alterKeyword,
        SyntaxToken materializedKeyword,
        SyntaxToken viewKeyword)
    {
        var allInTablespace = ParsePgAllInTablespace();
        var action = ParsePgAlterViewAction();
        return new AlterViewStatement
        {
            Span = SourceSpan.From(alterKeyword, action.Span),
            AlterKeyword = alterKeyword,
            MaterializedKeyword = materializedKeyword,
            ViewKeyword = viewKeyword,
            Name = [],
            AllInTablespace = allInTablespace,
            Action = action,
        };
    }

    // ALTER SCHEMA name OWNER TO role
    internal AlterSchemaStatement ParsePgAlterSchemaOwner(SyntaxToken alterKeyword, SyntaxToken schemaKeyword, IReadOnlyList<SyntaxToken> name)
    {
        var ownerKeyword = Advance();
        var toKeyword = ExpectIdent(Keyword.To);
        var owner = ParseGrantee();
        return new AlterSchemaStatement
        {
            Span = SourceSpan.From(alterKeyword, owner),
            AlterKeyword = alterKeyword,
            SchemaKeyword = schemaKeyword,
            Name = name,
            ToKeyword = toKeyword,
            NewName = [],
            OwnerKeyword = ownerKeyword,
            Owner = owner,
        };
    }

    // DROP SCHEMA [IF EXISTS] name [, ...] [CASCADE | RESTRICT]
    internal DropSchemaStatement ParsePgDropSchema(SyntaxToken dropKeyword, SyntaxToken schemaKeyword)
    {
        var targets = ParsePgDropTargets();
        return new DropSchemaStatement
        {
            Span = SourceSpan.From(dropKeyword, targets.End),
            DropKeyword = dropKeyword,
            SchemaKeyword = schemaKeyword,
            IfExists = targets.IfExists,
            Name = targets.Name,
            AdditionalNames = targets.AdditionalNames,
            Behavior = targets.Behavior,
        };
    }
}
