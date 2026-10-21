namespace QlParse;

// SQL Server SET items, MERGE WHEN clauses, BULK INSERT and TRUNCATE TABLE ... WITH (PARTITIONS ...)
// (workstream C).
internal sealed partial class Parser
{
    // ---- SET items ----

    private List<TsqlSetClause> ParseTsqlSetClauses()
    {
        var clauses = new List<TsqlSetClause> { ParseTsqlSetClause() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            clauses.Add(ParseTsqlSetClause());
        }

        return clauses;
    }

    // column {= | += | ...} value | column = DEFAULT | @v {= | += ...} value | @v = column = value
    // | column.WRITE(expr, offset, length) | udt_column.Method(...)
    private TsqlSetClause ParseTsqlSetClause()
    {
        if (_current.Kind == SyntaxKind.EmbeddedHost && NextKind is SyntaxKind.EqualsToken or SyntaxKind.CompoundAssignmentToken)
        {
            var variable = Advance();
            var variableOperator = Advance();
            if (variableOperator.Kind == SyntaxKind.EqualsToken && IsTsqlColumnAssignmentAhead())
            {
                return ParseTsqlColumnAssignment(variable, variableOperator);
            }

            var value = ParseExpression();
            return new TsqlSetClause
            {
                Span = SourceSpan.From(variable, value.Span),
                Variable = variable,
                VariableOperator = variableOperator,
                Value = value,
            };
        }

        return ParseTsqlColumnAssignment(null, null);
    }

    // `column [. column ...] =` ahead: the column half of `@v = column = value`.
    private bool IsTsqlColumnAssignmentAhead()
    {
        var index = _index - 1;
        while (index + 1 < _tokens.Count && _tokens[index].Kind == SyntaxKind.Identifier && _tokens[index + 1].Kind == SyntaxKind.Dot)
        {
            index += 2;
        }

        return index + 1 < _tokens.Count && _tokens[index].Kind == SyntaxKind.Identifier && _tokens[index + 1].Kind == SyntaxKind.EqualsToken;
    }

    private TsqlSetClause ParseTsqlColumnAssignment(SyntaxToken? variable, SyntaxToken? variableOperator)
    {
        var column = ParseExpression(ComparisonBindingPower + 1);
        var start = variable?.Span ?? column.Span;
        if (_current.Kind is not (SyntaxKind.EqualsToken or SyntaxKind.CompoundAssignmentToken))
        {
            if (variable is null && column is MethodInvocationExpression)
            {
                return new TsqlSetClause { Span = column.Span, Column = column };
            }

            throw new SqlParseException($"Expected = or a compound assignment, found {_current.Kind}", _current.Position);
        }

        var operatorToken = Advance();
        if (IdentifierEquals(Keyword.Default) && NextKind != SyntaxKind.OpenParen)
        {
            var defaultKeyword = Advance();
            return new TsqlSetClause
            {
                Span = SourceSpan.From(start, defaultKeyword),
                Variable = variable,
                VariableOperator = variableOperator,
                Column = column,
                Operator = operatorToken,
                DefaultKeyword = defaultKeyword,
            };
        }

        var value = ParseExpression();
        return new TsqlSetClause
        {
            Span = SourceSpan.From(start, value.Span),
            Variable = variable,
            VariableOperator = variableOperator,
            Column = column,
            Operator = operatorToken,
            Value = value,
        };
    }

    // ---- MERGE WHEN clauses ----

    // WHEN MATCHED [AND c] THEN {UPDATE SET ... | DELETE}
    // | WHEN NOT MATCHED [BY TARGET] [AND c] THEN INSERT [(cols)] {VALUES (...) | DEFAULT VALUES}
    // | WHEN NOT MATCHED BY SOURCE [AND c] THEN {UPDATE SET ... | DELETE}
    private MergeWhenClause ParseTsqlMergeWhen()
    {
        var whenKeyword = Advance();
        var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
        var matchedKeyword = ExpectIdent(Keyword.Matched);
        var byClause = ParseMergeByClause(notKeyword);
        SyntaxToken? andKeyword = null;
        Expression? condition = null;
        if (_current.Kind == SyntaxKind.AndKeyword)
        {
            andKeyword = Advance();
            condition = ParseExpression();
        }

        var thenKeyword = Expect(SyntaxKind.ThenKeyword);
        var bySource = byClause.ByKind is { } kind && TokenEquals(kind, Keyword.Source);
        var action = notKeyword is not null && !bySource ? ParseTsqlMergeInsert() : ParseTsqlMergeUpdateOrDelete();
        return new MergeWhenClause
        {
            Span = SourceSpan.From(whenKeyword, action.Span),
            WhenKeyword = whenKeyword,
            NotKeyword = notKeyword,
            MatchedKeyword = matchedKeyword,
            ByKeyword = byClause.ByKeyword,
            ByKind = byClause.ByKind,
            AndKeyword = andKeyword,
            Condition = condition,
            ThenKeyword = thenKeyword,
            Action = action,
        };
    }

    private MergeAction ParseTsqlMergeInsert()
    {
        if (!IdentifierEquals(Keyword.Insert) || !NextEquals(Keyword.Default))
        {
            return ParseMergeInsert();
        }

        var insertKeyword = Advance();
        var defaultKeyword = Advance();
        var valuesKeyword = Expect(SyntaxKind.ValuesKeyword);
        return new TsqlMergeInsertDefaultAction
        {
            Span = SourceSpan.From(insertKeyword, valuesKeyword),
            InsertKeyword = insertKeyword,
            DefaultKeyword = defaultKeyword,
            ValuesKeyword = valuesKeyword,
        };
    }

    private MergeAction ParseTsqlMergeUpdateOrDelete()
    {
        if (_current.Kind == SyntaxKind.UpdateKeyword)
        {
            var updateKeyword = Advance();
            var setKeyword = Expect(SyntaxKind.SetKeyword);
            var assignments = ParseTsqlSetClauses();
            return new TsqlMergeUpdateAction
            {
                Span = SourceSpan.From(updateKeyword, assignments[^1].Span),
                UpdateKeyword = updateKeyword,
                SetKeyword = setKeyword,
                Assignments = assignments,
            };
        }

        if (!IdentifierEquals(Keyword.Delete))
        {
            throw new SqlParseException($"Expected UPDATE or DELETE, found {_current.Kind}", _current.Position);
        }

        var deleteKeyword = Advance();
        return new MergeDeleteAction { Span = deleteKeyword.Span, DeleteKeyword = deleteKeyword };
    }

    // ---- BULK INSERT ----

    // BULK INSERT table FROM 'file' [WITH (option [= value] | ORDER (col [ASC | DESC], ...), ...)]
    private TsqlBulkInsertStatement ParseTsqlBulkInsert()
    {
        var bulkKeyword = Advance();
        var insertKeyword = Advance();
        var tableName = ParseTsqlMultiPartName();
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var source = Expect(SyntaxKind.String);
        if (_current.Kind != SyntaxKind.WithKeyword || NextKind != SyntaxKind.OpenParen)
        {
            return new TsqlBulkInsertStatement
            {
                Span = SourceSpan.From(bulkKeyword, source),
                BulkKeyword = bulkKeyword,
                InsertKeyword = insertKeyword,
                TableName = tableName,
                FromKeyword = fromKeyword,
                Source = source,
            };
        }

        var withKeyword = Advance();
        var openParen = Advance();
        var options = new List<TsqlBulkInsertOption> { ParseTsqlBulkInsertOption() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            options.Add(ParseTsqlBulkInsertOption());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlBulkInsertStatement
        {
            Span = SourceSpan.From(bulkKeyword, closeParen),
            BulkKeyword = bulkKeyword,
            InsertKeyword = insertKeyword,
            TableName = tableName,
            FromKeyword = fromKeyword,
            Source = source,
            WithKeyword = withKeyword,
            OpenParen = openParen,
            Options = options,
            CloseParen = closeParen,
        };
    }

    private TsqlBulkInsertOption ParseTsqlBulkInsertOption()
    {
        var name = ExpectTsqlDmlWord();
        if (_current.Kind == SyntaxKind.EqualsToken)
        {
            var equalsToken = Advance();
            var value = ParseExpression();
            return new TsqlBulkInsertOption { Span = SourceSpan.From(name, value.Span), Name = name, EqualsToken = equalsToken, Value = value };
        }

        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return new TsqlBulkInsertOption { Span = name.Span, Name = name };
        }

        var openParen = Advance();
        var order = new List<OrderByItem> { ParseOrderByItem() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            order.Add(ParseOrderByItem());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlBulkInsertOption
        {
            Span = SourceSpan.From(name, closeParen),
            Name = name,
            OpenParen = openParen,
            Order = order,
            CloseParen = closeParen,
        };
    }

    // ---- TRUNCATE TABLE name [WITH (PARTITIONS (n | n TO m, ...))] ----

    private TruncateStatement ParseTsqlTruncate()
    {
        var truncateKeyword = Advance();
        var tableKeyword = Advance();
        var tableName = ParseTsqlMultiPartName();
        var extras = _current.Kind == SyntaxKind.WithKeyword && NextKind == SyntaxKind.OpenParen ? ParseTsqlTruncatePartitions() : null;
        return new TruncateStatement
        {
            Span = SourceSpan.From(truncateKeyword, extras?.CloseParen ?? tableName[^1]),
            TruncateKeyword = truncateKeyword,
            TableKeyword = tableKeyword,
            TableName = tableName,
            SqlServer = extras,
        };
    }

    private TsqlTruncateExtras ParseTsqlTruncatePartitions()
    {
        var withKeyword = Advance();
        var openParen = Advance();
        var partitionsKeyword = ExpectIdent(Keyword.DmlPartitions);
        var partitionsOpen = Expect(SyntaxKind.OpenParen);
        var partitions = new List<TsqlTruncatePartitionRange> { ParseTsqlTruncatePartitionRange() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            partitions.Add(ParseTsqlTruncatePartitionRange());
        }

        var partitionsClose = Expect(SyntaxKind.CloseParen);
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlTruncateExtras
        {
            WithKeyword = withKeyword,
            OpenParen = openParen,
            PartitionsKeyword = partitionsKeyword,
            PartitionsOpenParen = partitionsOpen,
            Partitions = partitions,
            PartitionsCloseParen = partitionsClose,
            CloseParen = closeParen,
        };
    }

    private TsqlTruncatePartitionRange ParseTsqlTruncatePartitionRange()
    {
        var start = ParseExpression();
        if (!IdentifierEquals(Keyword.To))
        {
            return new TsqlTruncatePartitionRange { Span = start.Span, Start = start };
        }

        var toKeyword = Advance();
        var end = ParseExpression();
        return new TsqlTruncatePartitionRange { Span = SourceSpan.From(start.Span, end.Span), Start = start, ToKeyword = toKeyword, End = end };
    }
}
