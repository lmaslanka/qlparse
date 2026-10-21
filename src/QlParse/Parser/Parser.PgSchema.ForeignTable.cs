namespace QlParse;

// Postgres CREATE FOREIGN TABLE (column OPTIONS, INHERITS, PARTITION OF) and CREATE STATISTICS.
internal sealed partial class Parser
{
    private readonly record struct PgForeignTableBody(
        SyntaxToken? OpenParen,
        IReadOnlyList<PgForeignColumn> Columns,
        SyntaxToken? CloseParen,
        SyntaxToken? InheritsKeyword,
        SyntaxToken? InheritsOpenParen,
        IReadOnlyList<IReadOnlyList<SyntaxToken>> Inherits,
        SyntaxToken? InheritsCloseParen,
        PgForeignTablePartition? Partition,
        IReadOnlyList<TableConstraint>? Constraints = null);

    // CREATE FOREIGN TABLE [IF NOT EXISTS] name {(columns) [INHERITS (parents)] | PARTITION OF parent bound}
    // SERVER s [OPTIONS (...)]
    private PgCreateForeignTableStatement ParsePgCreateForeignTable()
    {
        var createKeyword = Advance();
        var foreignKeyword = Advance();
        var tableKeyword = Advance();
        var ifNotExists = ParsePgIfNotExists();
        var name = ParseQualifiedName();
        var body = IdentifierEquals(Keyword.Partition) ? ParsePgForeignPartitionBody() : ParsePgForeignColumnsBody();
        var serverKeyword = ExpectIdent(Keyword.PgServer);
        var server = Expect(SyntaxKind.Identifier);
        var options = ParsePgOptionalOptionList();
        return new PgCreateForeignTableStatement
        {
            Span = SourceSpan.From(createKeyword, options?.Span ?? server.Span),
            CreateKeyword = createKeyword,
            ForeignKeyword = foreignKeyword,
            TableKeyword = tableKeyword,
            IfKeyword = ifNotExists.IfKeyword,
            NotKeyword = ifNotExists.NotKeyword,
            ExistsKeyword = ifNotExists.ExistsKeyword,
            Name = name,
            OpenParen = body.OpenParen,
            Columns = body.Columns,
            Constraints = body.Constraints ?? [],
            CloseParen = body.CloseParen,
            InheritsKeyword = body.InheritsKeyword,
            InheritsOpenParen = body.InheritsOpenParen,
            Inherits = body.Inherits,
            InheritsCloseParen = body.InheritsCloseParen,
            Partition = body.Partition,
            ServerKeyword = serverKeyword,
            Server = server,
            Options = options,
        };
    }

    private PgForeignTableBody ParsePgForeignColumnsBody()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var elements = ParsePgForeignElements();
        var columns = elements.Columns;
        var closeParen = Expect(SyntaxKind.CloseParen);
        if (!IdentifierEquals(Keyword.PgInherits))
        {
            return new PgForeignTableBody(openParen, columns, closeParen, null, null, [], null, null, elements.Constraints);
        }

        var inheritsKeyword = Advance();
        var inheritsOpen = Expect(SyntaxKind.OpenParen);
        var parents = ParseSchemaNameList();
        var inheritsClose = Expect(SyntaxKind.CloseParen);
        return new PgForeignTableBody(openParen, columns, closeParen, inheritsKeyword, inheritsOpen, parents, inheritsClose, null, elements.Constraints);
    }

    private readonly record struct PgColumnConstraints(
        SyntaxToken? CollateKeyword,
        IReadOnlyList<SyntaxToken>? Collation,
        SyntaxToken? ConstraintKeyword,
        SyntaxToken? ConstraintName,
        SyntaxToken? NotKeyword,
        SyntaxToken? NullKeyword,
        DefaultClause? Default,
        SourceSpan? End,
        IReadOnlyList<TableConstraint>? Checks = null);

    // name type [OPTIONS (...)] [COLLATE c] [[CONSTRAINT n] {NOT NULL | NULL | DEFAULT e}]...
    private PgForeignColumn ParsePgForeignColumn()
    {
        var name = Expect(SyntaxKind.Identifier);
        var type = ParseDataType();
        var options = ParsePgOptionalOptionList();
        var constraints = ParsePgColumnConstraints();
        return new PgForeignColumn
        {
            Span = SourceSpan.From(name, constraints.End ?? options?.Span ?? type.Span),
            Name = name,
            Type = type,
            Options = options,
            CollateKeyword = constraints.CollateKeyword,
            Collation = constraints.Collation,
            ConstraintKeyword = constraints.ConstraintKeyword,
            ConstraintName = constraints.ConstraintName,
            NotKeyword = constraints.NotKeyword,
            NullKeyword = constraints.NullKeyword,
            Default = constraints.Default,
            Checks = constraints.Checks ?? [],
        };
    }

    private PgColumnConstraints ParsePgColumnConstraints()
    {
        var result = new PgColumnConstraints(null, null, null, null, null, null, null, null);
        while (true)
        {
            if (_current.Kind == SyntaxKind.CollateKeyword)
            {
                var collateKeyword = Advance();
                var collation = ParseQualifiedName();
                result = result with { CollateKeyword = collateKeyword, Collation = collation, End = collation[^1].Span };
                continue;
            }

            if (IdentifierEquals(Keyword.Constraint))
            {
                var constraintKeyword = Advance();
                var constraintName = Expect(SyntaxKind.Identifier);
                result = result with { ConstraintKeyword = constraintKeyword, ConstraintName = constraintName, End = constraintName.Span };
                continue;
            }

            if (_current.Kind is SyntaxKind.NotKeyword or SyntaxKind.NullKeyword)
            {
                SyntaxToken? notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : null;
                var nullKeyword = Expect(SyntaxKind.NullKeyword);
                result = result with { NotKeyword = notKeyword, NullKeyword = nullKeyword, End = nullKeyword.Span };
                continue;
            }

            if (IdentifierEquals(Keyword.Default))
            {
                var defaultClause = ParseDefaultClause();
                result = result with { Default = defaultClause, End = defaultClause.Span };
                continue;
            }

            if (!IdentifierEquals(Keyword.Check))
            {
                return result;
            }

            result = WithPgForeignColumnCheck(result);
        }
    }

    // CHECK (expression) [NO INHERIT] after a foreign table column.
    private PgColumnConstraints WithPgForeignColumnCheck(PgColumnConstraints result)
    {
        var check = ParseCheckConstraint(_current, null, null);
        List<TableConstraint> checks = result.Checks is null ? [check] : [.. result.Checks, check];
        return result with { Checks = checks, End = check.Span };
    }

    // PARTITION OF parent {FOR VALUES {IN (...) | FROM (...) TO (...) | WITH (MODULUS m, REMAINDER r)} | DEFAULT}
    private PgForeignTableBody ParsePgForeignPartitionBody()
    {
        var partitionKeyword = Advance();
        var ofKeyword = Expect(SyntaxKind.OfKeyword);
        var parent = ParseQualifiedName();
        if (IdentifierEquals(Keyword.Default))
        {
            var defaultKeyword = Advance();
            return PgForeignPartitionOnly(new PgForeignTablePartition
            {
                Span = SourceSpan.From(partitionKeyword, defaultKeyword),
                PartitionKeyword = partitionKeyword,
                OfKeyword = ofKeyword,
                Parent = parent,
                DefaultKeyword = defaultKeyword,
            });
        }

        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var valuesKeyword = Expect(SyntaxKind.ValuesKeyword);
        if (_current.Kind == SyntaxKind.WithKeyword)
        {
            var withKeyword = Advance();
            var hashBound = ParsePgDefinitionList();
            return PgForeignPartitionOnly(new PgForeignTablePartition
            {
                Span = SourceSpan.From(partitionKeyword, hashBound.Span),
                PartitionKeyword = partitionKeyword,
                OfKeyword = ofKeyword,
                Parent = parent,
                ForKeyword = forKeyword,
                ValuesKeyword = valuesKeyword,
                BoundKind = withKeyword,
                HashBound = hashBound,
            });
        }

        return ParsePgForeignListOrRangeBound(partitionKeyword, ofKeyword, parent, forKeyword, valuesKeyword);
    }

    private PgForeignTableBody ParsePgForeignListOrRangeBound(
        SyntaxToken partitionKeyword,
        SyntaxToken ofKeyword,
        IReadOnlyList<SyntaxToken> parent,
        SyntaxToken forKeyword,
        SyntaxToken valuesKeyword)
    {
        if (_current.Kind is not SyntaxKind.InKeyword and not SyntaxKind.FromKeyword)
        {
            throw new SqlParseException($"Expected IN, FROM, or WITH, found {_current.Kind}", _current.Position);
        }

        var boundKind = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var values = ParseExpressionList();
        var closeParen = Expect(SyntaxKind.CloseParen);
        SyntaxToken? toKeyword = null;
        SyntaxToken? toOpen = null;
        IReadOnlyList<Expression> toValues = [];
        SyntaxToken? toClose = null;
        if (boundKind.Kind == SyntaxKind.FromKeyword)
        {
            toKeyword = ExpectIdent(Keyword.To);
            toOpen = Expect(SyntaxKind.OpenParen);
            toValues = ParseExpressionList();
            toClose = Expect(SyntaxKind.CloseParen);
        }

        return PgForeignPartitionOnly(new PgForeignTablePartition
        {
            Span = SourceSpan.From(partitionKeyword, toClose ?? closeParen),
            PartitionKeyword = partitionKeyword,
            OfKeyword = ofKeyword,
            Parent = parent,
            ForKeyword = forKeyword,
            ValuesKeyword = valuesKeyword,
            BoundKind = boundKind,
            OpenParen = openParen,
            Values = values,
            CloseParen = closeParen,
            ToKeyword = toKeyword,
            ToOpenParen = toOpen,
            ToValues = toValues,
            ToCloseParen = toClose,
        });
    }

    private static PgForeignTableBody PgForeignPartitionOnly(PgForeignTablePartition partition) =>
        new(null, [], null, null, null, [], null, partition);

    // CREATE STATISTICS [[IF NOT EXISTS] name] [(kind, ...)] ON column_or_expression, ... FROM table
    private PgCreateStatisticsStatement ParsePgCreateStatistics()
    {
        var createKeyword = Advance();
        var statisticsKeyword = Advance();
        var ifNotExists = ParsePgIfNotExists();
        IReadOnlyList<SyntaxToken>? name = _current.Kind == SyntaxKind.Identifier ? ParseQualifiedName() : null;
        SyntaxToken? openParen = null;
        IReadOnlyList<SyntaxToken> kinds = [];
        SyntaxToken? closeParen = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            openParen = Advance();
            kinds = ParseNameList();
            closeParen = Expect(SyntaxKind.CloseParen);
        }

        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var expressions = ParseExpressionList();
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var table = ParseQualifiedName();
        return new PgCreateStatisticsStatement
        {
            Span = SourceSpan.From(createKeyword, table[^1]),
            CreateKeyword = createKeyword,
            StatisticsKeyword = statisticsKeyword,
            IfKeyword = ifNotExists.IfKeyword,
            NotKeyword = ifNotExists.NotKeyword,
            ExistsKeyword = ifNotExists.ExistsKeyword,
            Name = name,
            OpenParen = openParen,
            Kinds = kinds,
            CloseParen = closeParen,
            OnKeyword = onKeyword,
            Expressions = expressions,
            FromKeyword = fromKeyword,
            Table = table,
        };
    }
}
