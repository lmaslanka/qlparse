namespace QlParse;

internal sealed partial class Parser
{
    // `ALTER TABLE [IF EXISTS] [ONLY] name [*] action [, ...]`.
    internal AlterTableStatement ParsePgAlterTable(SyntaxToken alterKeyword, SyntaxToken tableKeyword)
    {
        var ifExists = TryParsePgExistenceCheck();
        var target = ParsePgAlterTableTarget();
        var action = ParseAlterTableAction();
        IReadOnlyList<AlterTableAction> additionalActions = [];
        if (_current.Kind == SyntaxKind.Comma)
        {
            var actions = new List<AlterTableAction>();
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                actions.Add(ParseAlterTableAction());
            }

            additionalActions = actions;
        }

        var end = additionalActions.Count > 0 ? additionalActions[^1].Span : action.Span;
        return new AlterTableStatement
        {
            Span = SourceSpan.From(alterKeyword, end),
            AlterKeyword = alterKeyword,
            TableKeyword = tableKeyword,
            Name = target.Name,
            Action = action,
            Postgres = BuildPgAlterTableExtras(ifExists, target, additionalActions),
        };
    }

    private static PgAlterTableExtras? BuildPgAlterTableExtras(
        ExistenceCheck? ifExists,
        PgAlterTableTarget target,
        IReadOnlyList<AlterTableAction> additionalActions)
    {
        if (ifExists is null && additionalActions.Count == 0 && !HasPgAlterTableTarget(target))
        {
            return null;
        }

        return new PgAlterTableExtras
        {
            IfExists = ifExists,
            AllInTablespace = target.AllInTablespace,
            OnlyKeyword = target.Only,
            Star = target.Star,
            AdditionalActions = additionalActions,
        };
    }

    private static bool HasPgAlterTableTarget(PgAlterTableTarget target) =>
        target.AllInTablespace is not null || target.Only is not null || target.Star is not null;

    private readonly record struct PgAlterTableTarget(
        AllInTablespaceClause? AllInTablespace,
        SyntaxToken? Only,
        IReadOnlyList<SyntaxToken> Name,
        SyntaxToken? Star);

    // `[ONLY] name [*]`, or `ALL IN TABLESPACE name [OWNED BY role [, ...]]` (whose name list stays empty).
    private PgAlterTableTarget ParsePgAlterTableTarget()
    {
        if (_current.Kind == SyntaxKind.AllKeyword && NextKind == SyntaxKind.InKeyword)
        {
            return new PgAlterTableTarget(ParsePgAllInTablespace(), null, [], null);
        }

        var only = _current.Kind == SyntaxKind.OnlyKeyword ? Advance() : (SyntaxToken?)null;
        var name = ParseQualifiedName();
        var star = _current.Kind == SyntaxKind.Star ? Advance() : (SyntaxToken?)null;
        return new PgAlterTableTarget(null, only, name, star);
    }

    private AllInTablespaceClause ParsePgAllInTablespace()
    {
        var allKeyword = Advance();
        var inKeyword = Advance();
        var tablespaceKeyword = ExpectIdent(Keyword.DdlTablespace);
        var tablespace = Expect(SyntaxKind.Identifier);
        SyntaxToken? ownedKeyword = null;
        SyntaxToken? byKeyword = null;
        IReadOnlyList<SyntaxToken> roles = [];
        if (IdentifierEquals(Keyword.DdlOwned))
        {
            ownedKeyword = Advance();
            byKeyword = Expect(SyntaxKind.ByKeyword);
            roles = ParseNameList();
        }

        var end = roles.Count > 0 ? roles[^1] : tablespace;
        return new AllInTablespaceClause
        {
            Span = SourceSpan.From(allKeyword, end),
            AllKeyword = allKeyword,
            InKeyword = inKeyword,
            TablespaceKeyword = tablespaceKeyword,
            Tablespace = tablespace,
            OwnedKeyword = ownedKeyword,
            ByKeyword = byKeyword,
            Roles = roles,
        };
    }

    // Postgres-only ALTER TABLE actions (also reused by ALTER INDEX / VIEW / SEQUENCE / TABLESPACE).
    // Returns null without consuming anything when the current token starts none of them.
    internal AlterTableAction? TryParsePgDdlAlterTableAction()
    {
        if (_current.Kind == SyntaxKind.SetKeyword)
        {
            return TryParsePgSetAction();
        }

        if (_current.Kind != SyntaxKind.Identifier)
        {
            return TryParsePgTypedTableAction();
        }

        var text = _current.TextOf(_source);
        return text.Length switch
        {
            Len2 => TryParsePgNoInherit(text),
            Len5 => TryParsePgAlterLen5(text),
            Len6 => TryParsePgAlterLen6(text),
            Len7 => TryParsePgAlterLen7(text),
            Len8 => TextEquals(text, Keyword.DdlValidate) ? ParsePgValidateConstraint() : null,
            _ => null,
        };
    }

    private AlterTableAction? TryParsePgNoInherit(ReadOnlySpan<char> text) =>
        TextEquals(text, Keyword.No) && NextEquals(Keyword.DdlInherit) ? ParsePgInheritAction() : null;

    private AlterTableAction? TryParsePgAlterLen5(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.DdlOwner))
        {
            return ParsePgOwnerAction();
        }

        if (TextEquals(text, Keyword.DdlReset) && NextKind == SyntaxKind.OpenParen)
        {
            var resetKeyword = Advance();
            var parameters = ParseStorageParameterList();
            return new RelationOptionsAction
            {
                Span = SourceSpan.From(resetKeyword, parameters.Span),
                VerbKeyword = resetKeyword,
                Parameters = parameters,
            };
        }

        return null;
    }

    private AlterTableAction? TryParsePgAlterLen6(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Rename))
        {
            return ParsePgRenameAction();
        }

        if (TextEquals(text, Keyword.DdlEnable))
        {
            return TryParsePgToggleTrigger();
        }

        if (TextEquals(text, Keyword.Attach) && NextEquals(Keyword.Partition))
        {
            return ParsePgAttachPartition();
        }

        if (TextEquals(text, Keyword.Detach) && NextEquals(Keyword.Partition))
        {
            return ParsePgDetachPartition();
        }

        return null;
    }

    private AlterTableAction? TryParsePgAlterLen7(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Disable))
        {
            return TryParsePgToggleTrigger();
        }

        if (TextEquals(text, Keyword.DdlInherit))
        {
            return ParsePgInheritAction();
        }

        if (TextEquals(text, Keyword.DdlCluster) && NextKind == SyntaxKind.OnKeyword)
        {
            var clusterKeyword = Advance();
            var onKeyword = Advance();
            var index = Expect(SyntaxKind.Identifier);
            return new ClusterAction
            {
                Span = SourceSpan.From(clusterKeyword, index),
                ClusterKeyword = clusterKeyword,
                OnKeyword = onKeyword,
                Index = index,
            };
        }

        if (TextEquals(text, Keyword.DdlReplica) && NextEquals(Keyword.Identity))
        {
            return ParsePgReplicaIdentity();
        }

        return null;
    }

    private AlterTableAction? TryParsePgSetAction()
    {
        if (NextKind == SyntaxKind.OpenParen)
        {
            var setKeyword = Advance();
            var parameters = ParseStorageParameterList();
            return new RelationOptionsAction
            {
                Span = SourceSpan.From(setKeyword, parameters.Span),
                VerbKeyword = setKeyword,
                Parameters = parameters,
            };
        }

        if (NextEquals(Keyword.Schema))
        {
            var setKeyword = Advance();
            var schemaKeyword = Advance();
            var schema = Expect(SyntaxKind.Identifier);
            return new SetRelationSchemaAction
            {
                Span = SourceSpan.From(setKeyword, schema),
                SetKeyword = setKeyword,
                SchemaKeyword = schemaKeyword,
                Schema = schema,
            };
        }

        if (NextEquals(Keyword.DdlTablespace))
        {
            return ParsePgSetTablespace();
        }

        return TryParsePgSetPersistenceOrCluster();
    }

    private AlterTableAction? TryParsePgSetPersistenceOrCluster()
    {
        if (NextEquals(Keyword.Logged) || NextEquals(Keyword.Unlogged))
        {
            var setKeyword = Advance();
            var persistence = Advance();
            return new SetPersistenceAction
            {
                Span = SourceSpan.From(setKeyword, persistence),
                SetKeyword = setKeyword,
                Persistence = persistence,
            };
        }

        if (NextEquals(Keyword.Without) && IsKeywordAt(_index + 1, Keyword.DdlCluster))
        {
            var setKeyword = Advance();
            var withoutKeyword = Advance();
            var clusterKeyword = Advance();
            return new ClusterAction
            {
                Span = SourceSpan.From(setKeyword, clusterKeyword),
                SetKeyword = setKeyword,
                WithoutKeyword = withoutKeyword,
                ClusterKeyword = clusterKeyword,
            };
        }

        return TryParsePgSetAccessMethodOrOids();
    }

    private SetTablespaceAction ParsePgSetTablespace()
    {
        var setKeyword = Advance();
        var tablespaceKeyword = Advance();
        var tablespace = Expect(SyntaxKind.Identifier);
        var nowait = IdentifierEquals(Keyword.DdlNowait) ? Advance() : (SyntaxToken?)null;
        return new SetTablespaceAction
        {
            Span = SourceSpan.From(setKeyword, nowait ?? tablespace),
            SetKeyword = setKeyword,
            TablespaceKeyword = tablespaceKeyword,
            Tablespace = tablespace,
            NowaitKeyword = nowait,
        };
    }

    // `RENAME TO new` | `RENAME [COLUMN] old TO new` | `RENAME CONSTRAINT old TO new`.
    private RenameRelationAction ParsePgRenameAction()
    {
        var renameKeyword = Advance();
        SyntaxToken? targetKeyword = null;
        SyntaxToken? target = null;
        if (!IdentifierEquals(Keyword.To))
        {
            if (IdentifierEquals(Keyword.Column) || IdentifierEquals(Keyword.Constraint))
            {
                targetKeyword = Advance();
            }

            target = Expect(SyntaxKind.Identifier);
        }

        var toKeyword = ExpectIdent(Keyword.To);
        var newName = Expect(SyntaxKind.Identifier);
        return new RenameRelationAction
        {
            Span = SourceSpan.From(renameKeyword, newName),
            RenameKeyword = renameKeyword,
            TargetKeyword = targetKeyword,
            Target = target,
            ToKeyword = toKeyword,
            NewName = newName,
        };
    }

    private RelationOwnerAction ParsePgOwnerAction()
    {
        var ownerKeyword = Advance();
        var toKeyword = ExpectIdent(Keyword.To);
        if (_current.Kind is not (SyntaxKind.Identifier or SyntaxKind.CurrentUserKeyword or SyntaxKind.SessionUserKeyword or SyntaxKind.CurrentRoleKeyword))
        {
            throw new SqlParseException($"Expected role name, found {_current.Kind}", _current.Position);
        }

        var owner = Advance();
        return new RelationOwnerAction
        {
            Span = SourceSpan.From(ownerKeyword, owner),
            OwnerKeyword = ownerKeyword,
            ToKeyword = toKeyword,
            Owner = owner,
        };
    }

    private InheritAction ParsePgInheritAction()
    {
        var start = _current;
        var noKeyword = IdentifierEquals(Keyword.No) ? Advance() : (SyntaxToken?)null;
        var inheritKeyword = Advance();
        var parent = ParseQualifiedName();
        return new InheritAction
        {
            Span = SourceSpan.From(start, parent[^1]),
            NoKeyword = noKeyword,
            InheritKeyword = inheritKeyword,
            Parent = parent,
        };
    }

    // `{ENABLE [REPLICA | ALWAYS] | DISABLE} {TRIGGER | RULE} {name | ALL | USER}`, anything else
    // (e.g. ROW LEVEL SECURITY) is left for other ALTER TABLE actions.
    private ToggleTriggerAction? TryParsePgToggleTrigger()
    {
        var hasMode = NextEquals(Keyword.DdlReplica) || NextEquals(Keyword.Always);
        var objectIndex = hasMode ? _index + 1 : _index;
        if (!IsKeywordAt(objectIndex, Keyword.Trigger) && !IsKeywordAt(objectIndex, Keyword.DdlRule))
        {
            return null;
        }

        var verbKeyword = Advance();
        var modeKeyword = hasMode ? Advance() : (SyntaxToken?)null;
        var objectKeyword = Advance();
        if (_current.Kind is not (SyntaxKind.Identifier or SyntaxKind.AllKeyword or SyntaxKind.UserKeyword))
        {
            throw new SqlParseException($"Expected trigger name, ALL, or USER, found {_current.Kind}", _current.Position);
        }

        var target = Advance();
        return new ToggleTriggerAction
        {
            Span = SourceSpan.From(verbKeyword, target),
            VerbKeyword = verbKeyword,
            ModeKeyword = modeKeyword,
            ObjectKeyword = objectKeyword,
            Target = target,
        };
    }

    private ReplicaIdentityAction ParsePgReplicaIdentity()
    {
        var replicaKeyword = Advance();
        var identityKeyword = Advance();
        if (_current.Kind is SyntaxKind.UsingKeyword)
        {
            var usingKeyword = Advance();
            var indexKeyword = ExpectIdent(Keyword.Index);
            var index = Expect(SyntaxKind.Identifier);
            return new ReplicaIdentityAction
            {
                Span = SourceSpan.From(replicaKeyword, index),
                ReplicaKeyword = replicaKeyword,
                IdentityKeyword = identityKeyword,
                Mode = usingKeyword,
                IndexKeyword = indexKeyword,
                Index = index,
            };
        }

        if (_current.Kind != SyntaxKind.FullKeyword && !IdentifierEquals(Keyword.Default) && !IdentifierEquals(Keyword.DdlNothing))
        {
            throw new SqlParseException($"Expected DEFAULT, FULL, NOTHING, or USING, found {_current.Kind}", _current.Position);
        }

        var mode = Advance();
        return new ReplicaIdentityAction
        {
            Span = SourceSpan.From(replicaKeyword, mode),
            ReplicaKeyword = replicaKeyword,
            IdentityKeyword = identityKeyword,
            Mode = mode,
        };
    }

    // `ATTACH PARTITION name {FOR VALUES ... | DEFAULT}`, ALTER INDEX attaches without a bound.
    private AttachPartitionAction ParsePgAttachPartition()
    {
        var attachKeyword = Advance();
        var partitionKeyword = Advance();
        var name = ParseQualifiedName();
        var bound = IsPartitionBoundStart() ? ParsePartitionBound() : null;
        return new AttachPartitionAction
        {
            Span = SourceSpan.From(attachKeyword, bound?.Span ?? name[^1].Span),
            AttachKeyword = attachKeyword,
            PartitionKeyword = partitionKeyword,
            Name = name,
            Bound = bound,
        };
    }

    private DetachPartitionAction ParsePgDetachPartition()
    {
        var detachKeyword = Advance();
        var partitionKeyword = Advance();
        var name = ParseQualifiedName();
        var mode = IdentifierEquals(Keyword.Concurrently) || IdentifierEquals(Keyword.Finalize) ? Advance() : (SyntaxToken?)null;
        return new DetachPartitionAction
        {
            Span = SourceSpan.From(detachKeyword, mode ?? name[^1]),
            DetachKeyword = detachKeyword,
            PartitionKeyword = partitionKeyword,
            Name = name,
            Mode = mode,
        };
    }

    private ValidateTableConstraintAction? ParsePgValidateConstraint()
    {
        if (!NextEquals(Keyword.Constraint))
        {
            return null;
        }

        var validateKeyword = Advance();
        var constraintKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        return new ValidateTableConstraintAction
        {
            Span = SourceSpan.From(validateKeyword, name),
            ValidateKeyword = validateKeyword,
            ConstraintKeyword = constraintKeyword,
            Name = name,
        };
    }
}
