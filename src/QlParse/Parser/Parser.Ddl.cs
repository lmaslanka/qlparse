namespace QlParse;

internal sealed partial class Parser
{
    private bool IsCreateTable()
    {
        if (!IdentifierEquals(Keyword.Create))
        {
            return false;
        }

        if (NextEquals(Keyword.Table))
        {
            return true;
        }

        if (!NextEquals(Keyword.Global) && !NextEquals(Keyword.Local))
        {
            return false;
        }

        return IsTemporaryTableAfterScope();
    }

    private bool NextEquals(string keyword) =>
        _index < _tokens.Count && TokenEquals(_tokens[_index], keyword);

    internal SchemaDefinition ParseCreateSchema()
    {
        var createKeyword = Advance();
        var schemaKeyword = ExpectIdent(Keyword.Schema);
        IReadOnlyList<SyntaxToken>? name = null;
        SyntaxToken? authorizationKeyword = null;
        SyntaxToken? authorization = null;
        if (!IdentifierEquals(Keyword.Authorization))
        {
            name = ParseQualifiedName();
        }

        if (IdentifierEquals(Keyword.Authorization))
        {
            authorizationKeyword = Advance();
            authorization = Expect(SyntaxKind.Identifier);
        }

        var initialEnd = authorization?.Span ?? (name is not null ? name[^1].Span : schemaKeyword.Span);
        var tail = ParseSchemaDefaultAndPath(initialEnd);
        var defaultKeyword = tail.DefaultKeyword;
        var characterKeyword = tail.CharacterKeyword;
        var setKeyword = tail.SetKeyword;
        var characterSet = tail.CharacterSet;
        var pathKeyword = tail.PathKeyword;
        var path = tail.Path;
        var end = tail.End;
        var elements = new List<CreateTableStatement>();
        while (IsCreateTable())
        {
            elements.Add(ParseCreateTable());
            end = elements[^1].Span;
        }

        return new SchemaDefinition
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            SchemaKeyword = schemaKeyword,
            Name = name,
            AuthorizationKeyword = authorizationKeyword,
            Authorization = authorization,
            DefaultKeyword = defaultKeyword,
            CharacterKeyword = characterKeyword,
            SetKeyword = setKeyword,
            CharacterSet = characterSet,
            PathKeyword = pathKeyword,
            Path = path,
            Elements = elements,
        };
    }

    private readonly record struct SchemaTail(
        SyntaxToken? DefaultKeyword,
        SyntaxToken? CharacterKeyword,
        SyntaxToken? SetKeyword,
        IReadOnlyList<SyntaxToken>? CharacterSet,
        SyntaxToken? PathKeyword,
        IReadOnlyList<IReadOnlyList<SyntaxToken>>? Path,
        SourceSpan End);

    private SchemaTail ParseSchemaDefaultAndPath(SourceSpan initialEnd)
    {
        SyntaxToken? defaultKeyword = null;
        SyntaxToken? characterKeyword = null;
        SyntaxToken? setKeyword = null;
        IReadOnlyList<SyntaxToken>? characterSet = null;
        SyntaxToken? pathKeyword = null;
        IReadOnlyList<IReadOnlyList<SyntaxToken>>? path = null;
        var end = initialEnd;
        while (IdentifierEquals(Keyword.Default) || IdentifierEquals(Keyword.Path))
        {
            if (IdentifierEquals(Keyword.Default))
            {
                if (defaultKeyword is not null)
                {
                    throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
                }

                defaultKeyword = Advance();
                characterKeyword = ExpectIdent(Keyword.Character);
                setKeyword = Expect(SyntaxKind.SetKeyword);
                characterSet = ParseQualifiedName();
                end = characterSet[^1].Span;
                continue;
            }

            if (pathKeyword is not null)
            {
                throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
            }

            pathKeyword = Advance();
            path = ParseSchemaNameList();
            end = path[^1][^1].Span;
        }

        return new SchemaTail(defaultKeyword, characterKeyword, setKeyword, characterSet, pathKeyword, path, end);
    }

    internal AlterSchemaStatement ParseAlterSchema()
    {
        var alterKeyword = Advance();
        var schemaKeyword = ExpectIdent(Keyword.Schema);
        var name = ParseQualifiedName();
        var renameKeyword = ExpectIdent(Keyword.Rename);
        var toKeyword = ExpectIdent(Keyword.To);
        var newName = ParseQualifiedName();
        return new AlterSchemaStatement
        {
            Span = SourceSpan.From(alterKeyword, newName[^1]),
            AlterKeyword = alterKeyword,
            SchemaKeyword = schemaKeyword,
            Name = name,
            RenameKeyword = renameKeyword,
            ToKeyword = toKeyword,
            NewName = newName,
        };
    }

    internal DropSchemaStatement ParseDropSchema()
    {
        var dropKeyword = Advance();
        var schemaKeyword = ExpectIdent(Keyword.Schema);
        var name = ParseQualifiedName();
        var behavior = ParseDropBehavior();
        return new DropSchemaStatement
        {
            Span = SourceSpan.From(dropKeyword, behavior),
            DropKeyword = dropKeyword,
            SchemaKeyword = schemaKeyword,
            Name = name,
            Behavior = behavior,
        };
    }

    private CreateTableStatement ParseCreateTable()
    {
        var createKeyword = Advance();
        SyntaxToken? scope = null;
        SyntaxToken? temporary = null;
        if (IdentifierEquals(Keyword.Global) || IdentifierEquals(Keyword.Local))
        {
            scope = Advance();
            temporary = ExpectIdent(Keyword.Temporary);
        }

        var tableKeyword = ExpectIdent(Keyword.Table);
        var name = ParseQualifiedName();
        var body = ParseTableBody();
        ParseOnCommit(out var onKeyword, out var commitKeyword, out var commitAction, out var rowsKeyword);
        ParseSystemVersioning(out var systemWith, out var systemKeyword, out var versioningKeyword);
        var withKeyword = systemWith ?? body.WithKeyword;
        var end = ParseCreateTableEnd(name, body, versioningKeyword, rowsKeyword);
        return new CreateTableStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            Scope = scope,
            TemporaryKeyword = temporary,
            TableKeyword = tableKeyword,
            Name = name,
            OfKeyword = body.OfKeyword,
            TypeName = body.TypeName,
            UnderKeyword = body.UnderKeyword,
            Supertable = body.Supertable,
            OpenParen = body.OpenParen,
            Elements = body.Elements,
            CloseParen = body.CloseParen,
            AsColumnsOpen = body.AsColumnsOpen,
            AsColumns = body.AsColumns,
            AsColumnsClose = body.AsColumnsClose,
            AsKeyword = body.AsKeyword,
            Query = body.Query,
            WithKeyword = withKeyword,
            NoKeyword = body.NoKeyword,
            DataKeyword = body.DataKeyword,
            OnKeyword = onKeyword,
            CommitKeyword = commitKeyword,
            CommitAction = commitAction,
            RowsKeyword = rowsKeyword,
            SystemKeyword = systemKeyword,
            VersioningKeyword = versioningKeyword,
        };
    }

    private static SourceSpan ParseCreateTableEnd(
        IReadOnlyList<SyntaxToken> name,
        TableBody body,
        SyntaxToken? versioningKeyword,
        SyntaxToken? rowsKeyword) =>
        versioningKeyword?.Span
        ?? rowsKeyword?.Span
        ?? body.DataKeyword?.Span
        ?? body.Query?.Span
        ?? body.CloseParen?.Span
        ?? body.Supertable?[^1].Span
        ?? body.TypeName?[^1].Span
        ?? name[^1].Span;

    private readonly record struct TableBody(
        SyntaxToken? OfKeyword,
        IReadOnlyList<SyntaxToken>? TypeName,
        SyntaxToken? UnderKeyword,
        IReadOnlyList<SyntaxToken>? Supertable,
        SyntaxToken? OpenParen,
        IReadOnlyList<TableElement>? Elements,
        SyntaxToken? CloseParen,
        SyntaxToken? AsColumnsOpen,
        IReadOnlyList<SyntaxToken>? AsColumns,
        SyntaxToken? AsColumnsClose,
        SyntaxToken? AsKeyword,
        Query? Query,
        SyntaxToken? WithKeyword,
        SyntaxToken? NoKeyword,
        SyntaxToken? DataKeyword);

    private TableBody ParseTableBody()
    {
        if (_current.Kind == SyntaxKind.OfKeyword)
        {
            return ParseTypedTableBody();
        }

        return ParsePlainTableBody();
    }

    private TableBody ParseTypedTableBody()
    {
        var ofKeyword = Advance();
        var typeName = ParseQualifiedName();
        SyntaxToken? underKeyword = null;
        IReadOnlyList<SyntaxToken>? supertable = null;
        if (IdentifierEquals(Keyword.Under))
        {
            underKeyword = Advance();
            supertable = ParseQualifiedName();
        }

        SyntaxToken? openParen = null;
        IReadOnlyList<TableElement>? elements = null;
        SyntaxToken? closeParen = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            ParseTableElements(out var elementOpen, out var elementList, out var elementClose);
            openParen = elementOpen;
            elements = elementList;
            closeParen = elementClose;
        }

        return new TableBody(ofKeyword, typeName, underKeyword, supertable, openParen, elements, closeParen, null, null, null, null, null, null, null, null);
    }

    private TableBody ParsePlainTableBody()
    {
        if (_current.Kind == SyntaxKind.AsKeyword || IsAsColumnList())
        {
            return ParseAsQueryTableBody();
        }

        ParseTableElements(out var elementOpen, out var elementList, out var elementClose);
        return new TableBody(null, null, null, null, elementOpen, elementList, elementClose, null, null, null, null, null, null, null, null);
    }

    private TableBody ParseAsQueryTableBody()
    {
        SyntaxToken? asColumnsOpen = null;
        IReadOnlyList<SyntaxToken>? asColumns = null;
        SyntaxToken? asColumnsClose = null;
        if (_current.Kind is SyntaxKind.OpenParen)
        {
            asColumnsOpen = Advance();
            asColumns = ParseNameList();
            asColumnsClose = Expect(SyntaxKind.CloseParen);
        }

        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var query = ParseQuery();
        ParseWithData(out var withKeyword, out var noKeyword, out var dataKeyword);
        return new TableBody(null, null, null, null, null, null, null, asColumnsOpen, asColumns, asColumnsClose, asKeyword, query, withKeyword, noKeyword, dataKeyword);
    }

    internal AlterTableStatement ParseAlterTable()
    {
        var alterKeyword = Advance();
        var tableKeyword = ExpectIdent(Keyword.Table);
        var name = ParseQualifiedName();
        var action = ParseAlterTableAction();
        return new AlterTableStatement
        {
            Span = SourceSpan.From(alterKeyword, action.Span),
            AlterKeyword = alterKeyword,
            TableKeyword = tableKeyword,
            Name = name,
            Action = action,
        };
    }

    internal DropTableStatement ParseDropTable()
    {
        var dropKeyword = Advance();
        var tableKeyword = ExpectIdent(Keyword.Table);
        var name = ParseQualifiedName();
        var behavior = ParseDropBehavior();
        return new DropTableStatement
        {
            Span = SourceSpan.From(dropKeyword, behavior),
            DropKeyword = dropKeyword,
            TableKeyword = tableKeyword,
            Name = name,
            Behavior = behavior,
        };
    }

    private AlterTableAction ParseAlterTableAction()
    {
        if (IdentifierEquals(Keyword.Add))
        {
            return ParseAddTableAction();
        }

        if (IdentifierEquals(Keyword.Drop))
        {
            return ParseDropTableAction();
        }

        if (!IdentifierEquals(Keyword.Alter))
        {
            throw new SqlParseException($"Expected ADD, DROP, or ALTER, found {_current.Kind}", _current.Position);
        }

        return ParseAlterColumn();
    }

    private AlterTableAction ParseAddTableAction()
    {
        var addKeyword = Advance();
        if (IsPeriodDefinition())
        {
            var period = ParsePeriodDefinition();
            return new AddPeriodAction
            {
                Span = SourceSpan.From(addKeyword, period.Span),
                AddKeyword = addKeyword,
                Period = period,
            };
        }

        if (IsSystemVersioning())
        {
            var systemKeyword = Advance();
            var versioningKeyword = Advance();
            return new SystemVersioningAction
            {
                Span = SourceSpan.From(addKeyword, versioningKeyword),
                VerbKeyword = addKeyword,
                SystemKeyword = systemKeyword,
                VersioningKeyword = versioningKeyword,
            };
        }

        if (IsTableConstraintStart())
        {
            var constraint = ParseTableConstraint();
            return new AddConstraintAction
            {
                Span = SourceSpan.From(addKeyword, constraint.Span),
                AddKeyword = addKeyword,
                Constraint = constraint,
            };
        }

        var columnKeyword = IdentifierEquals(Keyword.Column) ? Advance() : (SyntaxToken?)null;
        var column = ParseColumn();
        return new AddColumnAction
        {
            Span = SourceSpan.From(addKeyword, column.Span),
            AddKeyword = addKeyword,
            ColumnKeyword = columnKeyword,
            Column = column,
        };
    }

    private AlterTableAction ParseDropTableAction()
    {
        var dropKeyword = Advance();
        if (IsPeriodDefinition())
        {
            var periodKeyword = Advance();
            var forKeyword = Advance();
            var periodName = Expect(SyntaxKind.Identifier);
            var behavior = ParseDropBehavior();
            return new DropPeriodAction
            {
                Span = SourceSpan.From(dropKeyword, behavior),
                DropKeyword = dropKeyword,
                PeriodKeyword = periodKeyword,
                ForKeyword = forKeyword,
                Name = periodName,
                Behavior = behavior,
            };
        }

        if (IsSystemVersioning())
        {
            var systemKeyword = Advance();
            var versioningKeyword = Advance();
            return new SystemVersioningAction
            {
                Span = SourceSpan.From(dropKeyword, versioningKeyword),
                VerbKeyword = dropKeyword,
                SystemKeyword = systemKeyword,
                VersioningKeyword = versioningKeyword,
            };
        }

        if (IdentifierEquals(Keyword.Constraint))
        {
            var constraintKeyword = Advance();
            var constraintName = Expect(SyntaxKind.Identifier);
            var behavior = ParseDropBehavior();
            return new DropConstraintAction
            {
                Span = SourceSpan.From(dropKeyword, behavior),
                DropKeyword = dropKeyword,
                ConstraintKeyword = constraintKeyword,
                Name = constraintName,
                Behavior = behavior,
            };
        }

        var columnKeyword = IdentifierEquals(Keyword.Column) ? Advance() : (SyntaxToken?)null;
        var name = Expect(SyntaxKind.Identifier);
        var dropBehavior = ParseDropBehavior();
        return new DropColumnAction
        {
            Span = SourceSpan.From(dropKeyword, dropBehavior),
            DropKeyword = dropKeyword,
            ColumnKeyword = columnKeyword,
            Name = name,
            Behavior = dropBehavior,
        };
    }

}
