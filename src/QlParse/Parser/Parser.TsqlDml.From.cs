namespace QlParse;

// SQL Server FROM items (workstream C), reached from the ISO table-source parser under SqlOptions.SqlServer:
// table hints, TABLESAMPLE, FOR SYSTEM_TIME CONTAINED IN, PIVOT / UNPIVOT, CROSS / OUTER APPLY, join hints,
// table-valued function calls and the rowset functions (OPENJSON, OPENROWSET, CONTAINSTABLE, ...).
internal sealed partial class Parser
{
    // After a FROM item and its alias: legacy (hints), WITH (hints), FOR SYSTEM_TIME CONTAINED IN (...), PIVOT and UNPIVOT.
    internal TableSource ParseTsqlTableSourceTail(TableSource table)
    {
        table = ConvertTsqlLegacyHintColumns(table);
        while (true)
        {
            if (_current.Kind == SyntaxKind.WithKeyword && NextKind == SyntaxKind.OpenParen)
            {
                table = ParseTsqlHintedTable(table);
                continue;
            }

            if (_current.Kind == SyntaxKind.ForKeyword && NextEquals(Keyword.SystemTime))
            {
                table = ParseTsqlTemporalTable(table);
                continue;
            }

            if (IdentifierEquals(Keyword.DmlPivot) && NextKind == SyntaxKind.OpenParen)
            {
                table = ParseTsqlPivot(table);
                continue;
            }

            if (IdentifierEquals(Keyword.DmlUnpivot) && NextKind == SyntaxKind.OpenParen)
            {
                table = ParseTsqlUnpivot(table);
                continue;
            }

            return table;
        }
    }

    // ParseSystemTime leaves FOR SYSTEM_TIME CONTAINED IN to ParseTsqlTableSourceTail.
    internal bool IsTsqlSystemTimeContainedIn() =>
        _index + 1 < _tokens.Count && TokenEquals(_tokens[_index + 1], Keyword.DmlContained);

    private TsqlTemporalTable ParseTsqlTemporalTable(TableSource table)
    {
        var forKeyword = Advance();
        var systemTimeKeyword = Advance();
        var containedKeyword = ExpectIdent(Keyword.DmlContained);
        var inKeyword = Expect(SyntaxKind.InKeyword);
        var openParen = Expect(SyntaxKind.OpenParen);
        var start = ParseExpression();
        var comma = Expect(SyntaxKind.Comma);
        var end = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        var alias = ParseTsqlAlias();
        return new TsqlTemporalTable
        {
            Span = SourceSpan.From(table.Span, alias.Alias ?? closeParen),
            Table = table,
            ForKeyword = forKeyword,
            SystemTimeKeyword = systemTimeKeyword,
            ContainedKeyword = containedKeyword,
            InKeyword = inKeyword,
            OpenParen = openParen,
            Start = start,
            Comma = comma,
            End = end,
            CloseParen = closeParen,
            AsKeyword = alias.AsKeyword,
            Alias = alias.Alias,
        };
    }

    // TABLESAMPLE [SYSTEM] (size [PERCENT | ROWS]) [REPEATABLE (seed)]
    internal TsqlSampledTable ParseTsqlSample(TableSource table)
    {
        var tablesampleKeyword = Advance();
        var systemKeyword = IdentifierEquals(Keyword.System) ? Advance() : (SyntaxToken?)null;
        var openParen = Expect(SyntaxKind.OpenParen);
        var size = ParseExpression();
        var unit = IdentifierEquals(Keyword.Percent) || IdentifierEquals(Keyword.Rows) ? Advance() : (SyntaxToken?)null;
        var closeParen = Expect(SyntaxKind.CloseParen);
        if (!IdentifierEquals(Keyword.Repeatable))
        {
            return new TsqlSampledTable
            {
                Span = SourceSpan.From(table.Span, closeParen),
                Table = table,
                TablesampleKeyword = tablesampleKeyword,
                SystemKeyword = systemKeyword,
                OpenParen = openParen,
                Size = size,
                Unit = unit,
                CloseParen = closeParen,
            };
        }

        var repeatableKeyword = Advance();
        var repeatOpen = Expect(SyntaxKind.OpenParen);
        var seed = ParseExpression();
        var repeatClose = Expect(SyntaxKind.CloseParen);
        return new TsqlSampledTable
        {
            Span = SourceSpan.From(table.Span, repeatClose),
            Table = table,
            TablesampleKeyword = tablesampleKeyword,
            SystemKeyword = systemKeyword,
            OpenParen = openParen,
            Size = size,
            Unit = unit,
            CloseParen = closeParen,
            RepeatableKeyword = repeatableKeyword,
            RepeatOpenParen = repeatOpen,
            Seed = seed,
            RepeatCloseParen = repeatClose,
        };
    }

    // PIVOT (aggregate FOR column IN ([a], [b], ...)) [AS] alias
    private TsqlPivotTable ParseTsqlPivot(TableSource table)
    {
        var pivotKeyword = Advance();
        var openParen = Advance();
        var aggregate = ParseExpression();
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var column = ParseExpression(ComparisonBindingPower + 1);
        var inKeyword = Expect(SyntaxKind.InKeyword);
        var valuesOpen = Expect(SyntaxKind.OpenParen);
        var values = ParseNameList();
        var valuesClose = Expect(SyntaxKind.CloseParen);
        var closeParen = Expect(SyntaxKind.CloseParen);
        var alias = ParseTsqlRequiredAlias();
        return new TsqlPivotTable
        {
            Span = SourceSpan.From(table.Span, alias.Alias),
            Table = table,
            PivotKeyword = pivotKeyword,
            OpenParen = openParen,
            Aggregate = aggregate,
            ForKeyword = forKeyword,
            Column = column,
            InKeyword = inKeyword,
            ValuesOpenParen = valuesOpen,
            Values = values,
            ValuesCloseParen = valuesClose,
            CloseParen = closeParen,
            AsKeyword = alias.AsKeyword,
            Alias = alias.Alias,
        };
    }

    // UNPIVOT (value_column FOR name_column IN ([a], [b], ...)) [AS] alias
    private TsqlUnpivotTable ParseTsqlUnpivot(TableSource table)
    {
        var unpivotKeyword = Advance();
        var openParen = Advance();
        var valueColumn = Expect(SyntaxKind.Identifier);
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var nameColumn = Expect(SyntaxKind.Identifier);
        var inKeyword = Expect(SyntaxKind.InKeyword);
        var columnsOpen = Expect(SyntaxKind.OpenParen);
        var columns = ParseNameList();
        var columnsClose = Expect(SyntaxKind.CloseParen);
        var closeParen = Expect(SyntaxKind.CloseParen);
        var alias = ParseTsqlRequiredAlias();
        return new TsqlUnpivotTable
        {
            Span = SourceSpan.From(table.Span, alias.Alias),
            Table = table,
            UnpivotKeyword = unpivotKeyword,
            OpenParen = openParen,
            ValueColumn = valueColumn,
            ForKeyword = forKeyword,
            NameColumn = nameColumn,
            InKeyword = inKeyword,
            ColumnsOpenParen = columnsOpen,
            Columns = columns,
            ColumnsCloseParen = columnsClose,
            CloseParen = closeParen,
            AsKeyword = alias.AsKeyword,
            Alias = alias.Alias,
        };
    }

    private readonly record struct TsqlRequiredAlias(SyntaxToken? AsKeyword, SyntaxToken Alias);

    private TsqlRequiredAlias ParseTsqlRequiredAlias()
    {
        var alias = ParseTsqlAlias();
        return alias.Alias is { } name
            ? new TsqlRequiredAlias(alias.AsKeyword, name)
            : throw new SqlParseException("Expected alias", _current.Position);
    }

    // ---- Joins ----

    // [INNER | {LEFT | RIGHT | FULL} [OUTER] | CROSS] [HASH | LOOP | MERGE | REMOTE] JOIN, CROSS APPLY, OUTER APPLY.
    // NATURAL, UNION and USING joins do not exist in SQL Server.
    internal JoinClause ParseTsqlJoin()
    {
        if (_current.Kind == SyntaxKind.OuterKeyword)
        {
            return ParseTsqlApply(Advance());
        }

        if (_current.Kind == SyntaxKind.CrossKeyword)
        {
            var cross = Advance();
            return IdentifierEquals(Keyword.DmlApply) ? ParseTsqlApply(cross) : ParseTsqlJoinRest(cross, null, isCross: true);
        }

        if (_current.Kind is SyntaxKind.LeftKeyword or SyntaxKind.RightKeyword or SyntaxKind.FullKeyword)
        {
            var side = Advance();
            return ParseTsqlJoinRest(side, _current.Kind == SyntaxKind.OuterKeyword ? Advance() : null, isCross: false);
        }

        var inner = _current.Kind == SyntaxKind.InnerKeyword ? Advance() : (SyntaxToken?)null;
        return ParseTsqlJoinRest(inner, null, isCross: false);
    }

    private JoinClause ParseTsqlApply(SyntaxToken joinType)
    {
        var applyKeyword = ExpectIdent(Keyword.DmlApply);
        var table = ParseTableSource();
        return new JoinClause
        {
            Span = SourceSpan.From(joinType.Span, table.Span),
            JoinType = joinType,
            JoinKeyword = applyKeyword,
            Table = table,
        };
    }

    private JoinClause ParseTsqlJoinRest(SyntaxToken? joinType, SyntaxToken? outer, bool isCross)
    {
        var hint = joinType is not null && !isCross && IsTsqlJoinHint() ? Advance() : (SyntaxToken?)null;
        var joinKeyword = Expect(SyntaxKind.JoinKeyword);
        var table = ParseTableSource();
        var constraint = isCross ? null : TryParseOnConstraint(null, isCross: false, isUnion: false)
            ?? throw new SqlParseException("Expected ON", _current.Position);
        return new JoinClause
        {
            Span = SourceSpan.From((joinType ?? joinKeyword).Span, constraint?.Span ?? table.Span),
            JoinType = joinType,
            OuterKeyword = outer,
            JoinKeyword = joinKeyword,
            Table = table,
            Constraint = constraint,
            SqlServer = hint is { } token ? new TsqlJoinExtras { Hint = token, Constraint = constraint } : null,
        };
    }

    private bool IsTsqlJoinHint() =>
        IdentifierEquals(Keyword.DmlHash) || IdentifierEquals(Keyword.DmlLoop) || IdentifierEquals(Keyword.Merge) || IdentifierEquals(Keyword.DmlRemote);

    // ---- Table-valued and rowset functions ----

    // `name(...)` in FROM: a rowset function, or an ordinary inline / multi-statement table-valued function call.
    internal TableSource ParseTsqlRoutineTable(List<SyntaxToken> nameParts)
    {
        if (IsTsqlLegacyTableHints())
        {
            return ParseTsqlLegacyHintedReference(nameParts);
        }

        return nameParts.Count == 1 && Keyword.IsTsqlRowsetFunction(nameParts[0].TextOf(_source))
            ? ParseTsqlRowsetFunction(nameParts)
            : ParseTsqlTableFunction(nameParts);
    }

    private TableFunction ParseTsqlTableFunction(List<SyntaxToken> nameParts)
    {
        var openParen = Advance();
        var arguments = _current.Kind == SyntaxKind.CloseParen ? [] : ParseExpressionList();
        var closeParen = Expect(SyntaxKind.CloseParen);
        var alias = ParseTsqlAlias();
        SyntaxToken? columnOpen = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        SyntaxToken? columnClose = null;
        if (alias.Alias is not null)
        {
            ParseOptionalColumnList(out var open, out var names, out var close);
            columnOpen = open;
            columns = names;
            columnClose = close;
        }

        return new TableFunction
        {
            Span = SourceSpan.From(nameParts[0], columnClose ?? alias.Alias ?? closeParen),
            NameParts = nameParts,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
            AsKeyword = alias.AsKeyword,
            Alias = alias.Alias,
            ColumnOpenParen = columnOpen,
            Columns = columns,
            ColumnCloseParen = columnClose,
        };
    }

    internal TsqlRowsetFunctionTable ParseTsqlRowsetFunction(List<SyntaxToken> nameParts)
    {
        var openParen = Advance();
        var arguments = new List<TsqlRowsetArgument>();
        var separators = new List<SyntaxToken>();
        if (_current.Kind != SyntaxKind.CloseParen)
        {
            ParseTsqlRowsetArguments(arguments, separators);
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        IReadOnlyList<SyntaxToken> objectName = [];
        if (_current.Kind == SyntaxKind.Dot)
        {
            Advance();
            objectName = ParseTsqlMultiPartName();
        }

        var schema = _current.Kind == SyntaxKind.WithKeyword && NextKind is SyntaxKind.OpenParen or SyntaxKind.Identifier
            ? ParseTsqlRowsetSchema()
            : null;
        var alias = ParseTsqlAlias();
        SyntaxToken? columnOpen = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        SyntaxToken? columnClose = null;
        if (alias.Alias is not null)
        {
            ParseOptionalColumnList(out var open, out var names, out var close);
            columnOpen = open;
            columns = names;
            columnClose = close;
        }

        var end = columnClose?.Span ?? alias.Alias?.Span ?? schema?.Span ?? (objectName.Count > 0 ? objectName[^1].Span : closeParen.Span);
        return new TsqlRowsetFunctionTable
        {
            Span = SourceSpan.From(nameParts[0].Span, end),
            NameParts = nameParts,
            OpenParen = openParen,
            Arguments = arguments,
            Separators = separators,
            CloseParen = closeParen,
            ObjectName = objectName,
            Schema = schema,
            AsKeyword = alias.AsKeyword,
            Alias = alias.Alias,
            ColumnOpenParen = columnOpen,
            Columns = columns,
            ColumnCloseParen = columnClose,
        };
    }

    // Arguments are separated by `,`, or by `;` inside an OPENROWSET connection string list.
    private void ParseTsqlRowsetArguments(List<TsqlRowsetArgument> arguments, List<SyntaxToken> separators)
    {
        arguments.Add(ParseTsqlRowsetArgument());
        while (_current.Kind is SyntaxKind.Comma or SyntaxKind.Semicolon)
        {
            separators.Add(Advance());
            arguments.Add(ParseTsqlRowsetArgument());
        }
    }

    // expr | * | name = expr [AS alias] | BULK 'file' | LANGUAGE term | {CHANGES | VERSION} table
    private TsqlRowsetArgument ParseTsqlRowsetArgument()
    {
        SyntaxToken? name = null;
        SyntaxToken? equalsToken = null;
        if (NextKind == SyntaxKind.EqualsToken && IsTsqlDmlWord(_current))
        {
            name = Advance();
            equalsToken = Advance();
        }
        else
        {
            if (IsTsqlRowsetArgumentWord() && NextKind is not (SyntaxKind.Comma or SyntaxKind.CloseParen))
            {
                name = Advance();
            }
        }

        var value = _current.Kind == SyntaxKind.Star ? ParseStar() : ParseExpression();
        var start = name?.Span ?? value.Span;
        if (_current.Kind != SyntaxKind.AsKeyword)
        {
            return new TsqlRowsetArgument { Span = SourceSpan.From(start, value.Span), Name = name, EqualsToken = equalsToken, Value = value };
        }

        var asKeyword = Advance();
        var alias = Expect(SyntaxKind.Identifier);
        return new TsqlRowsetArgument
        {
            Span = SourceSpan.From(start, alias),
            Name = name,
            EqualsToken = equalsToken,
            Value = value,
            AsKeyword = asKeyword,
            Alias = alias,
        };
    }

    private bool IsTsqlRowsetArgumentWord() =>
        IdentifierEquals(Keyword.DmlBulk) || IdentifierEquals(Keyword.Language)
        || IdentifierEquals(Keyword.GapsChanges) || IdentifierEquals(Keyword.GapsVersion);

    // WITH (column type ['path' | ordinal] [AS JSON], ...) | WITH table_name
    private TsqlRowsetSchema ParseTsqlRowsetSchema()
    {
        var withKeyword = Advance();
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            var tableName = ParseTsqlMultiPartName();
            return new TsqlRowsetSchema { Span = SourceSpan.From(withKeyword, tableName[^1]), WithKeyword = withKeyword, TableName = tableName };
        }

        var openParen = Advance();
        var columns = new List<TsqlRowsetColumn> { ParseTsqlRowsetColumn() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            columns.Add(ParseTsqlRowsetColumn());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlRowsetSchema
        {
            Span = SourceSpan.From(withKeyword, closeParen),
            WithKeyword = withKeyword,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = closeParen,
        };
    }

    private TsqlRowsetColumn ParseTsqlRowsetColumn()
    {
        var name = ExpectTsqlDmlWord();
        var type = ParseDataType();
        var path = _current.Kind is SyntaxKind.String or SyntaxKind.Number ? Literal() : null;
        var end = path?.Span ?? type.Span;
        if (_current.Kind != SyntaxKind.AsKeyword || !NextEquals(Keyword.Json))
        {
            return new TsqlRowsetColumn { Span = SourceSpan.From(name.Span, end), Name = name, Type = type, Path = path };
        }

        var asKeyword = Advance();
        var jsonKeyword = Advance();
        return new TsqlRowsetColumn
        {
            Span = SourceSpan.From(name, jsonKeyword),
            Name = name,
            Type = type,
            Path = path,
            AsKeyword = asKeyword,
            JsonKeyword = jsonKeyword,
        };
    }
}
