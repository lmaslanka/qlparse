namespace QlParse;

// SQL Server ALTER TABLE (workstream D): `[WITH {CHECK|NOCHECK}]` prefix, comma-separated ADD / DROP lists,
// ALTER COLUMN, {CHECK|NOCHECK} CONSTRAINT, {ENABLE|DISABLE} TRIGGER, SWITCH, SET (...) and REBUILD.
internal sealed partial class Parser
{
    private AlterTableStatement ParseTsqlAlterTable()
    {
        var alterKeyword = Advance();
        var tableKeyword = Advance();
        var name = ParseQualifiedName();
        SyntaxToken? withKeyword = null;
        SyntaxToken? checkMode = null;
        if (_current.Kind == SyntaxKind.WithKeyword && (NextEquals(Keyword.Check) || NextEquals(Keyword.TsqlDdlNocheck)))
        {
            withKeyword = Advance();
            checkMode = Advance();
        }

        var action = ParseTsqlAlterTableAction();
        return new AlterTableStatement
        {
            Span = SourceSpan.From(alterKeyword, action.Span),
            AlterKeyword = alterKeyword,
            TableKeyword = tableKeyword,
            Name = name,
            Action = action,
            SqlServer = withKeyword is null ? null : new TsqlAlterTableExtras { WithKeyword = withKeyword, CheckMode = checkMode },
        };
    }

    private AlterTableAction ParseTsqlAlterTableAction()
    {
        if (IdentifierEquals(Keyword.Add))
        {
            return ParseTsqlAddAction();
        }

        if (IdentifierEquals(Keyword.Drop))
        {
            return ParseTsqlDropAction();
        }

        if (IdentifierEquals(Keyword.Alter))
        {
            return ParseTsqlAlterColumnAction();
        }

        return TryParseTsqlAlterTableToggle() ?? ParseTsqlAlterTableMaintenance();
    }

    // ---- ADD ----

    private AlterTableAction ParseTsqlAddAction()
    {
        var addKeyword = Advance();
        var elements = new List<TableElement> { ParseTsqlAddElement() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            elements.Add(ParseTsqlAddElement());
        }

        var span = SourceSpan.From(addKeyword, elements[^1].Span);
        if (elements.Count > 1)
        {
            return new TsqlAddElementsAction { Span = span, AddKeyword = addKeyword, Elements = elements };
        }

        return elements[0] switch
        {
            ColumnDefinition column => new AddColumnAction { Span = span, AddKeyword = addKeyword, Column = column },
            TableConstraint constraint => new AddConstraintAction { Span = span, AddKeyword = addKeyword, Constraint = constraint },
            PeriodDefinition period => new AddPeriodAction { Span = span, AddKeyword = addKeyword, Period = period },
            _ => new TsqlAddElementsAction { Span = span, AddKeyword = addKeyword, Elements = elements },
        };
    }

    // An ADD element: a column, a constraint (including `DEFAULT expr FOR column`), an index or a period.
    private TableElement ParseTsqlAddElement() =>
        IdentifierEquals(Keyword.Default) ? ParseTsqlTableConstraint() : ParseTsqlTableElement();

    // ---- DROP ----

    private AlterTableAction ParseTsqlDropAction()
    {
        var dropKeyword = Advance();
        var elements = new List<TsqlDropElement> { ParseTsqlDropElement() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            elements.Add(ParseTsqlDropElement());
        }

        var span = SourceSpan.From(dropKeyword, elements[^1].Span);
        if (elements is [{ WithKeyword: null, ForKeyword: null, KindKeyword: { } kind } single])
        {
            if (TokenEquals(kind, Keyword.Column))
            {
                return new DropColumnAction { Span = span, DropKeyword = dropKeyword, ColumnKeyword = kind, IfExists = single.IfExists, Name = single.Name };
            }

            return new DropConstraintAction { Span = span, DropKeyword = dropKeyword, ConstraintKeyword = kind, IfExists = single.IfExists, Name = single.Name };
        }

        return new TsqlDropElementsAction { Span = span, DropKeyword = dropKeyword, Elements = elements };
    }

    // `[CONSTRAINT|COLUMN] [IF EXISTS] name [WITH (...)]` or `PERIOD FOR SYSTEM_TIME`.
    private TsqlDropElement ParseTsqlDropElement()
    {
        var start = _current;
        if (IsPeriodDefinition())
        {
            var periodKeyword = Advance();
            var forKeyword = Advance();
            var periodName = Expect(SyntaxKind.Identifier);
            return new TsqlDropElement { Span = SourceSpan.From(periodKeyword, periodName), KindKeyword = periodKeyword, ForKeyword = forKeyword, Name = periodName };
        }

        var kindKeyword = IdentifierEquals(Keyword.Constraint) || IdentifierEquals(Keyword.Column) ? Advance() : (SyntaxToken?)null;
        var ifExists = TryParseTsqlIfExists();
        var name = Expect(SyntaxKind.Identifier);
        var (withKeyword, options) = _current.Kind == SyntaxKind.WithKeyword && NextKind == SyntaxKind.OpenParen
            ? TryParseTsqlWithOptionsClause()
            : default;
        return new TsqlDropElement
        {
            Span = SourceSpan.From(start, TsqlLastConsumed()),
            KindKeyword = kindKeyword,
            IfExists = ifExists,
            Name = name,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    // ---- ALTER COLUMN ----

    private TsqlAlterColumnAction ParseTsqlAlterColumnAction()
    {
        var alterKeyword = Advance();
        var columnKeyword = ExpectIdent(Keyword.Column);
        var name = Expect(SyntaxKind.Identifier);
        if (IdentifierEquals(Keyword.Add) || IdentifierEquals(Keyword.Drop))
        {
            return ParseTsqlAlterColumnProperty(alterKeyword, columnKeyword, name);
        }

        var dataType = ParseDataType();
        var collateKeyword = _current.Kind == SyntaxKind.CollateKeyword ? Advance() : (SyntaxToken?)null;
        var collation = collateKeyword is null ? null : ParseQualifiedName();
        var sparseBefore = TryParseTsqlSparse();
        var (notKeyword, nullKeyword) = TryParseTsqlNullability();
        var sparse = sparseBefore ?? TryParseTsqlSparse();
        var encryption = IdentifierEquals(Keyword.TsqlDdlEncrypted) ? ParseTsqlEncryption() : null;
        var (withKeyword, options) = TryParseTsqlWithOptionsClause();
        return new TsqlAlterColumnAction
        {
            Span = SourceSpan.From(alterKeyword, TsqlLastConsumed()),
            AlterKeyword = alterKeyword,
            ColumnKeyword = columnKeyword,
            Name = name,
            DataType = dataType,
            CollateKeyword = collateKeyword,
            Collation = collation,
            NotKeyword = notKeyword,
            NullKeyword = nullKeyword,
            SparseKeyword = sparse,
            Encryption = encryption,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    private SyntaxToken? TryParseTsqlSparse() => IdentifierEquals(Keyword.TsqlDdlSparse) ? Advance() : null;

    // `[NOT] NULL`, both absent when neither keyword follows.
    private (SyntaxToken? NotKeyword, SyntaxToken? NullKeyword) TryParseTsqlNullability()
    {
        if (_current.Kind == SyntaxKind.NullKeyword)
        {
            return (null, Advance());
        }

        if (_current.Kind != SyntaxKind.NotKeyword)
        {
            return (null, null);
        }

        var notKeyword = Advance();
        return (notKeyword, Expect(SyntaxKind.NullKeyword));
    }

    // `{ADD|DROP} {ROWGUIDCOL|PERSISTED|NOT FOR REPLICATION|SPARSE|HIDDEN|MASKED [WITH (FUNCTION = '...')]}`.
    private TsqlAlterColumnAction ParseTsqlAlterColumnProperty(SyntaxToken alterKeyword, SyntaxToken columnKeyword, SyntaxToken name)
    {
        var verbKeyword = Advance();
        TsqlMaskingClause? masking = null;
        IReadOnlyList<SyntaxToken> property = [];
        if (IdentifierEquals(Keyword.TsqlDdlMasked))
        {
            masking = ParseTsqlMasking();
        }
        else
        {
            property = TryParseTsqlNotForReplication() is { } replication
                ? [replication.NotKeyword, replication.ForKeyword, replication.ReplicationKeyword]
                : [ExpectTsqlColumnProperty()];
        }

        var (withKeyword, options) = TryParseTsqlWithOptionsClause();
        return new TsqlAlterColumnAction
        {
            Span = SourceSpan.From(alterKeyword, TsqlLastConsumed()),
            AlterKeyword = alterKeyword,
            ColumnKeyword = columnKeyword,
            Name = name,
            VerbKeyword = verbKeyword,
            Property = property,
            Masking = masking,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    private SyntaxToken ExpectTsqlColumnProperty()
    {
        if (IdentifierEquals(Keyword.TsqlDdlRowguidcol) || IdentifierEquals(Keyword.TsqlDdlPersisted)
            || IdentifierEquals(Keyword.TsqlDdlSparse) || IdentifierEquals(Keyword.TsqlDdlHidden))
        {
            return Advance();
        }

        throw new SqlParseException($"Expected column property, found {_current.Kind}", _current.Position);
    }

    // ---- CHECK / NOCHECK CONSTRAINT, ENABLE / DISABLE TRIGGER ----

    private AlterTableAction? TryParseTsqlAlterTableToggle()
    {
        if ((IdentifierEquals(Keyword.Check) || IdentifierEquals(Keyword.TsqlDdlNocheck)) && NextEquals(Keyword.Constraint))
        {
            var checkKeyword = Advance();
            var constraintKeyword = Advance();
            var (allKeyword, names) = ParseTsqlAllOrNames();
            return new TsqlCheckConstraintAction
            {
                Span = SourceSpan.From(checkKeyword, TsqlLastConsumed()),
                CheckKeyword = checkKeyword,
                ConstraintKeyword = constraintKeyword,
                AllKeyword = allKeyword,
                Names = names,
            };
        }

        if ((IdentifierEquals(Keyword.DdlEnable) || IdentifierEquals(Keyword.Disable)) && NextEquals(Keyword.Trigger))
        {
            var verbKeyword = Advance();
            var triggerKeyword = Advance();
            var (allKeyword, names) = ParseTsqlAllOrNames();
            return new TsqlToggleTriggersAction
            {
                Span = SourceSpan.From(verbKeyword, TsqlLastConsumed()),
                VerbKeyword = verbKeyword,
                TriggerKeyword = triggerKeyword,
                AllKeyword = allKeyword,
                Names = names,
            };
        }

        return null;
    }

    private (SyntaxToken? AllKeyword, IReadOnlyList<SyntaxToken> Names) ParseTsqlAllOrNames() =>
        _current.Kind == SyntaxKind.AllKeyword ? (Advance(), []) : (null, ParseNameList());

    // ---- SWITCH / SET / REBUILD ----

    private AlterTableAction ParseTsqlAlterTableMaintenance()
    {
        if (IdentifierEquals(Keyword.TsqlDdlSwitch))
        {
            return ParseTsqlSwitchPartition();
        }

        if (_current.Kind == SyntaxKind.SetKeyword && NextKind == SyntaxKind.OpenParen)
        {
            var setKeyword = Advance();
            var options = ParseTsqlRelationalOptions();
            return new TsqlSetTableOptionsAction { Span = SourceSpan.From(setKeyword, options.Span), SetKeyword = setKeyword, Options = options };
        }

        if (IdentifierEquals(Keyword.Rebuild))
        {
            return ParseTsqlRebuildAction();
        }

        throw new SqlParseException($"Expected ALTER TABLE action, found {_current.Kind}", _current.Position);
    }

    // `SWITCH [PARTITION n] TO target [PARTITION m] [WITH (...)]`.
    private TsqlSwitchPartitionAction ParseTsqlSwitchPartition()
    {
        var switchKeyword = Advance();
        var (partitionKeyword, sourcePartition) = TryParseTsqlPartitionNumber();
        var toKeyword = ExpectIdent(Keyword.To);
        var target = ParseQualifiedName();
        var (targetPartitionKeyword, targetPartition) = TryParseTsqlPartitionNumber();
        var (withKeyword, options) = TryParseTsqlWithOptionsClause();
        return new TsqlSwitchPartitionAction
        {
            Span = SourceSpan.From(switchKeyword, TsqlLastConsumed()),
            SwitchKeyword = switchKeyword,
            PartitionKeyword = partitionKeyword,
            SourcePartition = sourcePartition,
            ToKeyword = toKeyword,
            Target = target,
            TargetPartitionKeyword = targetPartitionKeyword,
            TargetPartition = targetPartition,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    private (SyntaxToken? PartitionKeyword, Expression? Number) TryParseTsqlPartitionNumber()
    {
        if (!IdentifierEquals(Keyword.Partition))
        {
            return (null, null);
        }

        var partitionKeyword = Advance();
        return (partitionKeyword, ParseExpression());
    }

    // `REBUILD [PARTITION = {ALL|n}] [WITH (...)]`, shared by ALTER TABLE and ALTER INDEX.
    private TsqlRebuildAction ParseTsqlRebuildAction()
    {
        var rebuildKeyword = Advance();
        var (partitionKeyword, equalsToken, partition) = TryParseTsqlPartitionAssignment();
        var (withKeyword, options) = TryParseTsqlWithOptionsClause();
        return new TsqlRebuildAction
        {
            Span = SourceSpan.From(rebuildKeyword, TsqlLastConsumed()),
            RebuildKeyword = rebuildKeyword,
            PartitionKeyword = partitionKeyword,
            EqualsToken = equalsToken,
            Partition = partition,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    private (SyntaxToken? PartitionKeyword, SyntaxToken? EqualsToken, SyntaxToken? Partition) TryParseTsqlPartitionAssignment()
    {
        if (!IdentifierEquals(Keyword.Partition))
        {
            return (null, null, null);
        }

        var partitionKeyword = Advance();
        var equalsToken = Expect(SyntaxKind.EqualsToken);
        var partition = _current.Kind == SyntaxKind.AllKeyword ? Advance() : Expect(SyntaxKind.Number);
        return (partitionKeyword, equalsToken, partition);
    }
}
