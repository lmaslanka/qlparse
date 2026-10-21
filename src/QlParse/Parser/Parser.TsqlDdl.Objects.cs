namespace QlParse;

// SQL Server views, synonyms, partition functions and schemes, CREATE TYPE, and the multi-name
// `DROP kind [IF EXISTS] a, b` statement (workstream D).
internal sealed partial class Parser
{
    // `CREATE VIEW name [(columns)] [WITH SCHEMABINDING|ENCRYPTION|VIEW_METADATA, ...] AS query [WITH CHECK OPTION]`;
    // CREATE OR ALTER VIEW calls this from its ALTER keyword on.
    private CreateViewStatement ParseTsqlCreateView()
    {
        var createKeyword = Advance();
        var viewKeyword = ExpectIdent(Keyword.View);
        var name = ParseQualifiedName();
        SyntaxToken? columnsOpen = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        SyntaxToken? columnsClose = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            ParseRequiredNameList(out var open, out var names, out var close);
            columnsOpen = open;
            columns = names;
            columnsClose = close;
        }

        var attributesWith = _current.Kind == SyntaxKind.WithKeyword ? Advance() : (SyntaxToken?)null;
        IReadOnlyList<SyntaxToken> attributes = attributesWith is null ? [] : ParseNameList();
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var query = ParseQuery();
        ParseCheckOption(out var withKeyword, out var levels, out var checkKeyword, out var optionKeyword);
        return new CreateViewStatement
        {
            Span = SourceSpan.From(createKeyword, TsqlLastConsumed()),
            CreateKeyword = createKeyword,
            ViewKeyword = viewKeyword,
            Name = name,
            ColumnsOpen = columnsOpen,
            Columns = columns,
            ColumnsClose = columnsClose,
            AsKeyword = asKeyword,
            Query = query,
            WithKeyword = withKeyword,
            Levels = levels,
            CheckKeyword = checkKeyword,
            OptionKeyword = optionKeyword,
            SqlServer = attributesWith is null ? null : new TsqlCreateViewExtras { AttributesWithKeyword = attributesWith.Value, Attributes = attributes },
        };
    }

    // `CREATE SYNONYM name FOR [server.][database.][schema.]object`.
    private TsqlCreateSynonymStatement ParseTsqlCreateSynonym()
    {
        var createKeyword = Advance();
        var synonymKeyword = Advance();
        var name = ParseQualifiedName();
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var target = ParseQualifiedName();
        return new TsqlCreateSynonymStatement
        {
            Span = SourceSpan.From(createKeyword, target[^1]),
            CreateKeyword = createKeyword,
            SynonymKeyword = synonymKeyword,
            Name = name,
            ForKeyword = forKeyword,
            Target = target,
        };
    }

    // `CREATE PARTITION FUNCTION name (type) AS RANGE [LEFT|RIGHT] FOR VALUES (boundary, ...)`.
    private TsqlCreatePartitionFunctionStatement ParseTsqlCreatePartitionFunction()
    {
        var createKeyword = Advance();
        var partitionKeyword = Advance();
        var functionKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var openParen = Expect(SyntaxKind.OpenParen);
        var parameterType = ParseDataType();
        var closeParen = Expect(SyntaxKind.CloseParen);
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var rangeKeyword = ExpectIdent(Keyword.Range);
        var boundary = _current.Kind is SyntaxKind.LeftKeyword or SyntaxKind.RightKeyword ? Advance() : (SyntaxToken?)null;
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var valuesKeyword = Expect(SyntaxKind.ValuesKeyword);
        var valuesOpen = Expect(SyntaxKind.OpenParen);
        IReadOnlyList<Expression> values = _current.Kind == SyntaxKind.CloseParen ? [] : ParseExpressionList();
        var valuesClose = Expect(SyntaxKind.CloseParen);
        return new TsqlCreatePartitionFunctionStatement
        {
            Span = SourceSpan.From(createKeyword, valuesClose),
            CreateKeyword = createKeyword,
            PartitionKeyword = partitionKeyword,
            FunctionKeyword = functionKeyword,
            Name = name,
            OpenParen = openParen,
            ParameterType = parameterType,
            CloseParen = closeParen,
            AsKeyword = asKeyword,
            RangeKeyword = rangeKeyword,
            Boundary = boundary,
            ForKeyword = forKeyword,
            ValuesKeyword = valuesKeyword,
            ValuesOpen = valuesOpen,
            Values = values,
            ValuesClose = valuesClose,
        };
    }

    // `CREATE PARTITION SCHEME name AS PARTITION function [ALL] TO (filegroup, ...)`.
    private TsqlCreatePartitionSchemeStatement ParseTsqlCreatePartitionScheme()
    {
        var createKeyword = Advance();
        var partitionKeyword = Advance();
        var schemeKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var asPartitionKeyword = ExpectIdent(Keyword.Partition);
        var functionName = Expect(SyntaxKind.Identifier);
        var allKeyword = _current.Kind == SyntaxKind.AllKeyword ? Advance() : (SyntaxToken?)null;
        var toKeyword = ExpectIdent(Keyword.To);
        ParseRequiredNameList(out var openParen, out var fileGroups, out var closeParen);
        return new TsqlCreatePartitionSchemeStatement
        {
            Span = SourceSpan.From(createKeyword, closeParen),
            CreateKeyword = createKeyword,
            PartitionKeyword = partitionKeyword,
            SchemeKeyword = schemeKeyword,
            Name = name,
            AsKeyword = asKeyword,
            AsPartitionKeyword = asPartitionKeyword,
            FunctionName = functionName,
            AllKeyword = allKeyword,
            ToKeyword = toKeyword,
            OpenParen = openParen,
            FileGroups = fileGroups,
            CloseParen = closeParen,
        };
    }

    // `ALTER PARTITION FUNCTION name () {SPLIT|MERGE} RANGE (boundary)`.
    private TsqlAlterPartitionFunctionStatement ParseTsqlAlterPartitionFunction()
    {
        var alterKeyword = Advance();
        var partitionKeyword = Advance();
        var functionKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var openParen = Expect(SyntaxKind.OpenParen);
        var closeParen = Expect(SyntaxKind.CloseParen);
        if (!IdentifierEquals(Keyword.TsqlDdlSplit) && !IdentifierEquals(Keyword.Merge))
        {
            throw new SqlParseException($"Expected SPLIT or MERGE, found {_current.Kind}", _current.Position);
        }

        var actionKeyword = Advance();
        var rangeKeyword = ExpectIdent(Keyword.Range);
        var valueOpen = Expect(SyntaxKind.OpenParen);
        var value = ParseExpression();
        var valueClose = Expect(SyntaxKind.CloseParen);
        return new TsqlAlterPartitionFunctionStatement
        {
            Span = SourceSpan.From(alterKeyword, valueClose),
            AlterKeyword = alterKeyword,
            PartitionKeyword = partitionKeyword,
            FunctionKeyword = functionKeyword,
            Name = name,
            OpenParen = openParen,
            CloseParen = closeParen,
            ActionKeyword = actionKeyword,
            RangeKeyword = rangeKeyword,
            ValueOpen = valueOpen,
            Value = value,
            ValueClose = valueClose,
        };
    }

    // `ALTER PARTITION SCHEME name NEXT USED [filegroup]`.
    private TsqlAlterPartitionSchemeStatement ParseTsqlAlterPartitionScheme()
    {
        var alterKeyword = Advance();
        var partitionKeyword = Advance();
        var schemeKeyword = Advance();
        var name = Expect(SyntaxKind.Identifier);
        var nextKeyword = ExpectIdent(Keyword.Next);
        var usedKeyword = ExpectIdent(Keyword.TsqlDdlUsed);
        var fileGroup = _current.Kind == SyntaxKind.Identifier && !IsTsqlReservedAliasWord() ? Advance() : (SyntaxToken?)null;
        return new TsqlAlterPartitionSchemeStatement
        {
            Span = SourceSpan.From(alterKeyword, fileGroup ?? usedKeyword),
            AlterKeyword = alterKeyword,
            PartitionKeyword = partitionKeyword,
            SchemeKeyword = schemeKeyword,
            Name = name,
            NextKeyword = nextKeyword,
            UsedKeyword = usedKeyword,
            FileGroup = fileGroup,
        };
    }

    // CREATE TYPE is claimed only for its T-SQL forms: `FROM base_type`, `AS TABLE (...)`, `EXTERNAL NAME`.
    private TsqlCreateTypeStatement? TryParseTsqlCreateType()
    {
        var index = _index + 1;
        while (index + Len2 < _tokens.Count && _tokens[index].Kind == SyntaxKind.Identifier && _tokens[index + 1].Kind == SyntaxKind.Dot)
        {
            index += Len2;
        }

        index++;
        if (index >= _tokens.Count)
        {
            return null;
        }

        var after = _tokens[index];
        var isTsqlForm = after.Kind == SyntaxKind.FromKeyword
            || (after.Kind == SyntaxKind.AsKeyword && IsKeywordAt(index + 1, Keyword.Table))
            || TokenEquals(after, Keyword.External);
        return isTsqlForm ? ParseTsqlCreateType() : null;
    }

    private TsqlCreateTypeStatement ParseTsqlCreateType()
    {
        var createKeyword = Advance();
        var typeKeyword = Advance();
        var name = ParseQualifiedName();
        if (_current.Kind == SyntaxKind.FromKeyword)
        {
            var fromKeyword = Advance();
            var baseType = ParseDataType();
            var (notKeyword, nullKeyword) = TryParseTsqlNullability();
            return new TsqlCreateTypeStatement
            {
                Span = SourceSpan.From(createKeyword, TsqlLastConsumed()),
                CreateKeyword = createKeyword,
                TypeKeyword = typeKeyword,
                Name = name,
                FromKeyword = fromKeyword,
                BaseType = baseType,
                NotKeyword = notKeyword,
                NullKeyword = nullKeyword,
            };
        }

        return _current.Kind == SyntaxKind.AsKeyword
            ? ParseTsqlCreateTableType(createKeyword, typeKeyword, name)
            : ParseTsqlCreateClrType(createKeyword, typeKeyword, name);
    }

    private TsqlCreateTypeStatement ParseTsqlCreateTableType(SyntaxToken createKeyword, SyntaxToken typeKeyword, IReadOnlyList<SyntaxToken> name)
    {
        var asKeyword = Advance();
        var tableKeyword = Advance();
        var body = ParseTsqlTableElements();
        var (withKeyword, options) = TryParseTsqlParenthesizedWith();

        return new TsqlCreateTypeStatement
        {
            Span = SourceSpan.From(createKeyword, TsqlLastConsumed()),
            CreateKeyword = createKeyword,
            TypeKeyword = typeKeyword,
            Name = name,
            AsKeyword = asKeyword,
            TableKeyword = tableKeyword,
            OpenParen = body.OpenParen,
            Elements = body.Elements,
            CloseParen = body.CloseParen,
            WithKeyword = withKeyword,
            Options = options,
        };
    }

    private TsqlCreateTypeStatement ParseTsqlCreateClrType(SyntaxToken createKeyword, SyntaxToken typeKeyword, IReadOnlyList<SyntaxToken> name)
    {
        var externalKeyword = Advance();
        var nameKeyword = ExpectIdent(Keyword.Name);
        var externalName = ParseQualifiedName();
        return new TsqlCreateTypeStatement
        {
            Span = SourceSpan.From(createKeyword, externalName[^1]),
            CreateKeyword = createKeyword,
            TypeKeyword = typeKeyword,
            Name = name,
            ExternalKeyword = externalKeyword,
            NameKeyword = nameKeyword,
            ExternalName = externalName,
        };
    }

    // `DROP kind [IF EXISTS] name, ... [ON {DATABASE|ALL SERVER}]`; kindLength is 1 or 2 (PARTITION FUNCTION).
    private TsqlDropObjectsStatement ParseTsqlDropObjects(int kindLength)
    {
        var dropKeyword = Advance();
        IReadOnlyList<SyntaxToken> objectKind = kindLength == 1 ? [Advance()] : [Advance(), Advance()];
        var ifExists = TryParseTsqlIfExists();
        var names = ParseSchemaNameList();
        SyntaxToken? onKeyword = null;
        IReadOnlyList<SyntaxToken> scope = [];
        if (_current.Kind == SyntaxKind.OnKeyword && TokenEquals(objectKind[0], Keyword.Trigger))
        {
            onKeyword = Advance();
            scope = _current.Kind == SyntaxKind.AllKeyword ? [Advance(), ExpectIdent(Keyword.TsqlDdlServer)] : [ExpectIdent(Keyword.TsqlDdlDatabase)];
        }

        return new TsqlDropObjectsStatement
        {
            Span = SourceSpan.From(dropKeyword, TsqlLastConsumed()),
            DropKeyword = dropKeyword,
            ObjectKind = objectKind,
            IfExists = ifExists,
            Names = names,
            OnKeyword = onKeyword,
            Scope = scope,
        };
    }
}
