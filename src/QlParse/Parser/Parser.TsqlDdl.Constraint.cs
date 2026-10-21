namespace QlParse;

// SQL Server table and column constraints (workstream D): PRIMARY KEY / UNIQUE with CLUSTERED|NONCLUSTERED
// [HASH], per-column ASC/DESC, index options and storage; FOREIGN KEY / REFERENCES and CHECK with NOT FOR
// REPLICATION; graph edge CONNECTION constraints; and `DEFAULT expr FOR column` (ALTER TABLE ... ADD).
internal sealed partial class Parser
{
    private readonly record struct TsqlConstraintHead(SyntaxToken Start, SyntaxToken? ConstraintKeyword, SyntaxToken? ConstraintName);

    private readonly record struct TsqlKeyColumns(
        SyntaxToken OpenParen,
        IReadOnlyList<SyntaxToken> Names,
        IReadOnlyList<SyntaxToken?>? Directions,
        SyntaxToken CloseParen);

    private TableConstraint ParseTsqlTableConstraint() => ParseTsqlConstraint(column: false);

    private TableConstraint ParseTsqlConstraint(bool column)
    {
        var start = _current;
        SyntaxToken? constraintKeyword = null;
        SyntaxToken? constraintName = null;
        if (IdentifierEquals(Keyword.Constraint))
        {
            constraintKeyword = Advance();
            constraintName = Expect(SyntaxKind.Identifier);
        }

        var head = new TsqlConstraintHead(start, constraintKeyword, constraintName);
        if (IdentifierEquals(Keyword.Primary) || _current.Kind == SyntaxKind.UniqueKeyword)
        {
            return ParseTsqlKeyConstraint(head);
        }

        if (IdentifierEquals(Keyword.Foreign) || IdentifierEquals(Keyword.References))
        {
            return ParseTsqlForeignKeyConstraint(head);
        }

        if (IdentifierEquals(Keyword.Check))
        {
            return ParseTsqlCheckConstraint(head);
        }

        return ParseTsqlOtherConstraint(head, column);
    }

    private TableConstraint ParseTsqlOtherConstraint(TsqlConstraintHead head, bool column)
    {
        if (IdentifierEquals(Keyword.Connection) && !column)
        {
            return ParseTsqlEdgeConstraint(head);
        }

        if (IdentifierEquals(Keyword.Default) && !column)
        {
            return ParseTsqlDefaultConstraint(head);
        }

        throw new SqlParseException($"Expected constraint, found {_current.Kind}", _current.Position);
    }

    private TableConstraint ParseTsqlKeyConstraint(TsqlConstraintHead head)
    {
        SyntaxToken? primaryKeyword = null;
        SyntaxToken? keyKeyword = null;
        SyntaxToken? uniqueKeyword = null;
        if (IdentifierEquals(Keyword.Primary))
        {
            primaryKeyword = Advance();
            keyKeyword = ExpectIdent(Keyword.Key);
        }
        else
        {
            uniqueKeyword = Advance();
        }

        var clustered = IdentifierEquals(Keyword.Clustered) || IdentifierEquals(Keyword.Nonclustered) ? Advance() : (SyntaxToken?)null;
        var hash = IdentifierEquals(Keyword.TsqlDdlHash) ? Advance() : (SyntaxToken?)null;
        TsqlKeyColumns? columns = _current.Kind == SyntaxKind.OpenParen ? ParseTsqlKeyColumns() : null;
        var (withKeyword, options) = TryParseTsqlWithOptionsClause();
        var extras = BuildTsqlKeyConstraintExtras(clustered, hash, columns?.Directions, withKeyword, options, TryParseTsqlOnStorage());
        return new TableConstraint
        {
            Span = SourceSpan.From(head.Start, TsqlLastConsumed()),
            ConstraintKeyword = head.ConstraintKeyword,
            ConstraintName = head.ConstraintName,
            PrimaryKeyword = primaryKeyword,
            KeyKeyword = keyKeyword,
            UniqueKeyword = uniqueKeyword,
            ColumnsOpen = columns?.OpenParen,
            Columns = columns?.Names,
            ColumnsClose = columns?.CloseParen,
            SqlServer = extras,
        };
    }

    private static TsqlTableConstraintExtras? BuildTsqlKeyConstraintExtras(
        SyntaxToken? clustered,
        SyntaxToken? hash,
        IReadOnlyList<SyntaxToken?>? directions,
        SyntaxToken? withKeyword,
        TsqlRelationalOptionList? options,
        TsqlStorageClause? storage)
    {
        if (clustered is null && hash is null && directions is null)
        {
            if (options is null && storage is null)
            {
                return null;
            }
        }

        return new TsqlTableConstraintExtras
        {
            ClusteredKeyword = clustered,
            HashKeyword = hash,
            ColumnDirections = directions,
            WithKeyword = withKeyword,
            Options = options,
            Storage = storage,
        };
    }

    // `(column [ASC|DESC], ...)`; Directions is allocated only when some column has an explicit order.
    private TsqlKeyColumns ParseTsqlKeyColumns()
    {
        var openParen = Advance();
        var names = new List<SyntaxToken>();
        List<SyntaxToken?>? directions = null;
        while (true)
        {
            names.Add(Expect(SyntaxKind.Identifier));
            if (_current.Kind is SyntaxKind.AscKeyword or SyntaxKind.DescKeyword)
            {
                directions ??= [.. Enumerable.Repeat<SyntaxToken?>(null, names.Count - 1)];
                directions.Add(Advance());
            }
            else
            {
                directions?.Add(null);
            }

            if (_current.Kind != SyntaxKind.Comma)
            {
                break;
            }

            Advance();
        }

        return new TsqlKeyColumns(openParen, names, directions, Expect(SyntaxKind.CloseParen));
    }

    // `[FOREIGN KEY [(columns)]] REFERENCES table [(columns)] [ON DELETE ...] [ON UPDATE ...] [NOT FOR REPLICATION]`.
    private TableConstraint ParseTsqlForeignKeyConstraint(TsqlConstraintHead head)
    {
        SyntaxToken? foreignKeyword = null;
        SyntaxToken? keyKeyword = null;
        SyntaxToken? columnsOpen = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        SyntaxToken? columnsClose = null;
        if (IdentifierEquals(Keyword.Foreign))
        {
            foreignKeyword = Advance();
            keyKeyword = ExpectIdent(Keyword.Key);
            if (_current.Kind == SyntaxKind.OpenParen)
            {
                ParseRequiredNameList(out var open, out var names, out var close);
                columnsOpen = open;
                columns = names;
                columnsClose = close;
            }
        }

        ParseReferences(out var referencesKeyword, out var referenceTable, out var referenceOpen, out var referenceColumns, out var referenceClose);
        ParseMatchAndActions(out _, out _, out var actions);
        var notForReplication = TryParseTsqlNotForReplication();
        return new TableConstraint
        {
            Span = SourceSpan.From(head.Start, TsqlLastConsumed()),
            ConstraintKeyword = head.ConstraintKeyword,
            ConstraintName = head.ConstraintName,
            ForeignKeyword = foreignKeyword,
            KeyKeyword = keyKeyword,
            ColumnsOpen = columnsOpen,
            Columns = columns,
            ColumnsClose = columnsClose,
            ReferencesKeyword = referencesKeyword,
            ReferenceTable = referenceTable,
            ReferenceOpen = referenceOpen,
            ReferenceColumns = referenceColumns,
            ReferenceClose = referenceClose,
            Actions = actions,
            SqlServer = notForReplication is null ? null : new TsqlTableConstraintExtras { NotForReplication = notForReplication },
        };
    }

    // `CHECK [NOT FOR REPLICATION] (condition)`.
    private TableConstraint ParseTsqlCheckConstraint(TsqlConstraintHead head)
    {
        var checkKeyword = Advance();
        var notForReplication = TryParseTsqlNotForReplication();
        var checkOpen = Expect(SyntaxKind.OpenParen);
        var check = ParseExpression();
        var checkClose = Expect(SyntaxKind.CloseParen);
        return new TableConstraint
        {
            Span = SourceSpan.From(head.Start, checkClose),
            ConstraintKeyword = head.ConstraintKeyword,
            ConstraintName = head.ConstraintName,
            CheckKeyword = checkKeyword,
            CheckOpen = checkOpen,
            Check = check,
            CheckClose = checkClose,
            SqlServer = notForReplication is null ? null : new TsqlTableConstraintExtras { NotForReplication = notForReplication },
        };
    }

    // Graph edge constraint: `CONNECTION (node TO node, ...) [ON DELETE {NO ACTION|CASCADE}]`.
    private TableConstraint ParseTsqlEdgeConstraint(TsqlConstraintHead head)
    {
        var connectionKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var connections = new List<TsqlEdgeConnection> { ParseTsqlEdgeConnection() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            connections.Add(ParseTsqlEdgeConnection());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        ParseMatchAndActions(out _, out _, out var actions);
        return new TableConstraint
        {
            Span = SourceSpan.From(head.Start, TsqlLastConsumed()),
            ConstraintKeyword = head.ConstraintKeyword,
            ConstraintName = head.ConstraintName,
            Actions = actions,
            SqlServer = new TsqlTableConstraintExtras
            {
                Connection = new TsqlEdgeConnectionClause
                {
                    Span = SourceSpan.From(connectionKeyword, closeParen),
                    ConnectionKeyword = connectionKeyword,
                    OpenParen = openParen,
                    Connections = connections,
                    CloseParen = closeParen,
                },
            },
        };
    }

    private TsqlEdgeConnection ParseTsqlEdgeConnection()
    {
        var fromNode = ParseQualifiedName();
        var toKeyword = ExpectIdent(Keyword.To);
        var toNode = ParseQualifiedName();
        return new TsqlEdgeConnection
        {
            Span = SourceSpan.From(fromNode[0], toNode[^1]),
            FromNode = fromNode,
            ToKeyword = toKeyword,
            ToNode = toNode,
        };
    }

    // `[CONSTRAINT name] DEFAULT expr FOR column [WITH VALUES]` (ALTER TABLE ... ADD).
    private TableConstraint ParseTsqlDefaultConstraint(TsqlConstraintHead head)
    {
        var defaultClause = ParseDefaultClause();
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var forColumn = Expect(SyntaxKind.Identifier);
        SyntaxToken? withKeyword = null;
        SyntaxToken? valuesKeyword = null;
        if (_current.Kind == SyntaxKind.WithKeyword && NextKind == SyntaxKind.ValuesKeyword)
        {
            withKeyword = Advance();
            valuesKeyword = Advance();
        }

        return new TableConstraint
        {
            Span = SourceSpan.From(head.Start, TsqlLastConsumed()),
            ConstraintKeyword = head.ConstraintKeyword,
            ConstraintName = head.ConstraintName,
            SqlServer = new TsqlTableConstraintExtras
            {
                Default = defaultClause,
                ForKeyword = forKeyword,
                ForColumn = forColumn,
                ValuesWithKeyword = withKeyword,
                ValuesKeyword = valuesKeyword,
            },
        };
    }
}
