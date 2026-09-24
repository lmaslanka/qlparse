namespace QlParse;

internal sealed partial class Parser
{
    private bool IsCreateTable() =>
        IdentifierEquals(Keyword.Create)
        && (NextEquals(Keyword.Table) || NextEquals(Keyword.Global) || NextEquals(Keyword.Local));

    private bool NextEquals(string keyword) =>
        _index < _tokens.Count && TokenEquals(_tokens[_index], keyword);

    private SchemaDefinition ParseCreateSchema()
    {
        var createKeyword = Advance();
        var schemaKeyword = ExpectIdent(Keyword.Schema);
        IReadOnlyList<SyntaxToken>? name = null;
        SyntaxToken? authorizationKeyword = null;
        SyntaxToken? authorization = null;
        if (IdentifierEquals(Keyword.Authorization))
        {
            authorizationKeyword = Advance();
            authorization = Expect(SyntaxKind.Identifier);
        }
        else
        {
            name = ParseQualifiedName();
            if (IdentifierEquals(Keyword.Authorization))
            {
                authorizationKeyword = Advance();
                authorization = Expect(SyntaxKind.Identifier);
            }
        }

        SyntaxToken? defaultKeyword = null;
        SyntaxToken? characterKeyword = null;
        SyntaxToken? setKeyword = null;
        IReadOnlyList<SyntaxToken>? characterSet = null;
        SyntaxToken? pathKeyword = null;
        IReadOnlyList<IReadOnlyList<SyntaxToken>>? path = null;
        var end = authorization?.Span ?? (name is not null ? name[^1].Span : schemaKeyword.Span);
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

    private AlterSchemaStatement ParseAlterSchema()
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

    private DropSchemaStatement ParseDropSchema()
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
        SyntaxToken? ofKeyword = null;
        IReadOnlyList<SyntaxToken>? typeName = null;
        SyntaxToken? underKeyword = null;
        IReadOnlyList<SyntaxToken>? supertable = null;
        SyntaxToken? openParen = null;
        IReadOnlyList<TableElement>? elements = null;
        SyntaxToken? closeParen = null;
        SyntaxToken? asColumnsOpen = null;
        IReadOnlyList<SyntaxToken>? asColumns = null;
        SyntaxToken? asColumnsClose = null;
        SyntaxToken? asKeyword = null;
        Query? query = null;
        SyntaxToken? withKeyword = null;
        SyntaxToken? noKeyword = null;
        SyntaxToken? dataKeyword = null;
        if (_current.Kind == SyntaxKind.OfKeyword)
        {
            ofKeyword = Advance();
            typeName = ParseQualifiedName();
            if (IdentifierEquals(Keyword.Under))
            {
                underKeyword = Advance();
                supertable = ParseQualifiedName();
            }

            if (_current.Kind == SyntaxKind.OpenParen)
            {
                ParseTableElements(out var elementOpen, out var elementList, out var elementClose);
                openParen = elementOpen;
                elements = elementList;
                closeParen = elementClose;
            }
        }
        else if (_current.Kind == SyntaxKind.AsKeyword || IsAsColumnList())
        {
            if (_current.Kind == SyntaxKind.OpenParen)
            {
                asColumnsOpen = Advance();
                asColumns = ParseNameList();
                asColumnsClose = Expect(SyntaxKind.CloseParen);
            }

            asKeyword = Expect(SyntaxKind.AsKeyword);
            query = ParseQuery();
            ParseWithData(out var withToken, out noKeyword, out var dataToken);
            withKeyword = withToken;
            dataKeyword = dataToken;
        }
        else
        {
            ParseTableElements(out var elementOpen, out var elementList, out var elementClose);
            openParen = elementOpen;
            elements = elementList;
            closeParen = elementClose;
        }

        ParseOnCommit(out var onKeyword, out var commitKeyword, out var commitAction, out var rowsKeyword);
        ParseSystemVersioning(out var systemWith, out var systemKeyword, out var versioningKeyword);
        if (systemWith is not null)
        {
            withKeyword = systemWith;
        }

        var end = versioningKeyword?.Span
            ?? rowsKeyword?.Span
            ?? dataKeyword?.Span
            ?? query?.Span
            ?? closeParen?.Span
            ?? supertable?[^1].Span
            ?? typeName?[^1].Span
            ?? name[^1].Span;
        return new CreateTableStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            Scope = scope,
            TemporaryKeyword = temporary,
            TableKeyword = tableKeyword,
            Name = name,
            OfKeyword = ofKeyword,
            TypeName = typeName,
            UnderKeyword = underKeyword,
            Supertable = supertable,
            OpenParen = openParen,
            Elements = elements,
            CloseParen = closeParen,
            AsColumnsOpen = asColumnsOpen,
            AsColumns = asColumns,
            AsColumnsClose = asColumnsClose,
            AsKeyword = asKeyword,
            Query = query,
            WithKeyword = withKeyword,
            NoKeyword = noKeyword,
            DataKeyword = dataKeyword,
            OnKeyword = onKeyword,
            CommitKeyword = commitKeyword,
            CommitAction = commitAction,
            RowsKeyword = rowsKeyword,
            SystemKeyword = systemKeyword,
            VersioningKeyword = versioningKeyword,
        };
    }

    private AlterTableStatement ParseAlterTable()
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

    private DropTableStatement ParseDropTable()
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

        if (IdentifierEquals(Keyword.Drop))
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

        if (!IdentifierEquals(Keyword.Alter))
        {
            throw new SqlParseException($"Expected ADD, DROP, or ALTER, found {_current.Kind}", _current.Position);
        }

        return ParseAlterColumn();
    }

    private AlterColumnAction ParseAlterColumn()
    {
        var alterKeyword = Advance();
        var columnKeyword = IdentifierEquals(Keyword.Column) ? Advance() : (SyntaxToken?)null;
        var name = Expect(SyntaxKind.Identifier);
        SyntaxToken? setKeyword = null;
        SyntaxToken? dropKeyword = null;
        SyntaxToken? addKeyword = null;
        SyntaxToken? dataKeyword = null;
        SyntaxToken? typeKeyword = null;
        DataType? dataType = null;
        DefaultClause? defaultClause = null;
        SyntaxToken? notKeyword = null;
        SyntaxToken? nullKeyword = null;
        SyntaxToken? scopeKeyword = null;
        IReadOnlyList<SyntaxToken>? scopeName = null;
        SyntaxToken? behavior = null;
        SyntaxToken? restartKeyword = null;
        SyntaxToken? withKeyword = null;
        Expression? restartValue = null;
        SyntaxToken? generatedKeyword = null;
        SyntaxToken? alwaysKeyword = null;
        SyntaxToken? byKeyword = null;
        SyntaxToken? defaultKeyword = null;
        SyntaxToken? identityKeyword = null;
        SequenceOption? identityOption = null;
        SourceSpan end;
        if (_current.Kind == SyntaxKind.SetKeyword)
        {
            setKeyword = Advance();
            if (IdentifierEquals(Keyword.Data))
            {
                dataKeyword = Advance();
                typeKeyword = ExpectIdent(Keyword.Type);
                dataType = ParseDataType();
                end = dataType.Span;
            }
            else if (IdentifierEquals(Keyword.Default))
            {
                defaultClause = ParseDefaultClause();
                end = defaultClause.Span;
            }
            else if (_current.Kind == SyntaxKind.NotKeyword)
            {
                notKeyword = Advance();
                var nullToken = Expect(SyntaxKind.NullKeyword);
                nullKeyword = nullToken;
                end = nullToken.Span;
            }
            else if (IdentifierEquals(Keyword.Generated))
            {
                var generatedToken = Advance();
                generatedKeyword = generatedToken;
                ParseGenerationRule(out alwaysKeyword, out byKeyword, out defaultKeyword);
                end = (defaultKeyword ?? alwaysKeyword ?? generatedToken).Span;
            }
            else if (IsBasicSequenceOption())
            {
                var option = ParseSequenceOption();
                identityOption = option;
                end = option.Span;
            }
            else
            {
                throw new SqlParseException($"Expected column alter action, found {_current.Kind}", _current.Position);
            }
        }
        else if (IdentifierEquals(Keyword.Drop))
        {
            dropKeyword = Advance();
            if (IdentifierEquals(Keyword.Default))
            {
                var defaultToken = Advance();
                defaultKeyword = defaultToken;
                end = defaultToken.Span;
            }
            else if (_current.Kind == SyntaxKind.NotKeyword)
            {
                notKeyword = Advance();
                var nullToken = Expect(SyntaxKind.NullKeyword);
                nullKeyword = nullToken;
                end = nullToken.Span;
            }
            else if (IdentifierEquals(Keyword.Scope))
            {
                scopeKeyword = Advance();
                var dropBehavior = ParseDropBehavior();
                behavior = dropBehavior;
                end = dropBehavior.Span;
            }
            else if (IdentifierEquals(Keyword.Identity))
            {
                var identityToken = Advance();
                identityKeyword = identityToken;
                end = identityToken.Span;
            }
            else
            {
                throw new SqlParseException($"Expected DROP action, found {_current.Kind}", _current.Position);
            }
        }
        else if (IdentifierEquals(Keyword.Add))
        {
            addKeyword = Advance();
            scopeKeyword = ExpectIdent(Keyword.Scope);
            scopeName = ParseQualifiedName();
            end = scopeName[^1].Span;
        }
        else if (IdentifierEquals(Keyword.Restart))
        {
            var restartToken = Advance();
            restartKeyword = restartToken;
            end = restartToken.Span;
            if (_current.Kind == SyntaxKind.WithKeyword)
            {
                withKeyword = Advance();
                restartValue = ParseExpression();
                end = restartValue.Span;
            }
        }
        else
        {
            throw new SqlParseException($"Expected column alter action, found {_current.Kind}", _current.Position);
        }

        return new AlterColumnAction
        {
            Span = SourceSpan.From(alterKeyword, end),
            AlterKeyword = alterKeyword,
            ColumnKeyword = columnKeyword,
            Name = name,
            SetKeyword = setKeyword,
            DropKeyword = dropKeyword,
            AddKeyword = addKeyword,
            DataKeyword = dataKeyword,
            TypeKeyword = typeKeyword,
            DataType = dataType,
            Default = defaultClause,
            NotKeyword = notKeyword,
            NullKeyword = nullKeyword,
            ScopeKeyword = scopeKeyword,
            ScopeName = scopeName,
            Behavior = behavior,
            RestartKeyword = restartKeyword,
            WithKeyword = withKeyword,
            RestartValue = restartValue,
            GeneratedKeyword = generatedKeyword,
            AlwaysKeyword = alwaysKeyword,
            ByKeyword = byKeyword,
            DefaultKeyword = defaultKeyword,
            IdentityKeyword = identityKeyword,
            IdentityOption = identityOption,
        };
    }

    private void ParseTableElements(
        out SyntaxToken openParen,
        out IReadOnlyList<TableElement> elements,
        out SyntaxToken closeParen)
    {
        openParen = Expect(SyntaxKind.OpenParen);
        var items = new List<TableElement> { ParseTableElement() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParseTableElement());
        }

        elements = items;
        closeParen = Expect(SyntaxKind.CloseParen);
    }

    private TableElement ParseTableElement()
    {
        if (_current.Kind == SyntaxKind.LikeKeyword)
        {
            return ParseLikeClause();
        }

        if (IsTableConstraintStart())
        {
            return ParseTableConstraint();
        }

        if (IsPeriodDefinition())
        {
            return ParsePeriodDefinition();
        }

        if (IsRefIs())
        {
            return ParseRefIs();
        }

        if (IsWithOptions())
        {
            return ParseColumn(withOptions: true);
        }

        return ParseColumn();
    }

    private ColumnDefinition ParseColumn(bool withOptions = false)
    {
        var name = Expect(SyntaxKind.Identifier);
        SyntaxToken? withKeyword = null;
        SyntaxToken? optionsKeyword = null;
        DataType? type = null;
        if (withOptions)
        {
            withKeyword = Expect(SyntaxKind.WithKeyword);
            optionsKeyword = ExpectIdent(Keyword.Options);
        }
        else
        {
            type = ParseDataType();
        }

        ParseColumnTail(
            out var defaultClause,
            out var identity,
            out var generated,
            out var collateKeyword,
            out var collation,
            out var scopeKeyword,
            out var scopeName,
            out var constraints);
        var end = name.Span;
        if (type is not null)
        {
            end = type.Span;
        }

        if (optionsKeyword is SyntaxToken optionsToken)
        {
            end = optionsToken.Span;
        }

        if (defaultClause is not null)
        {
            end = defaultClause.Span;
        }

        if (identity is not null)
        {
            end = identity.Span;
        }

        if (generated is not null)
        {
            end = generated.Span;
        }

        if (collation is not null)
        {
            end = collation[^1].Span;
        }

        if (scopeName is not null)
        {
            end = scopeName[^1].Span;
        }

        if (constraints.Count > 0)
        {
            end = constraints[^1].Span;
        }

        return new ColumnDefinition
        {
            Span = SourceSpan.From(name, end),
            Name = name,
            Type = type,
            WithKeyword = withKeyword,
            OptionsKeyword = optionsKeyword,
            Default = defaultClause,
            Identity = identity,
            Generated = generated,
            CollateKeyword = collateKeyword,
            Collation = collation,
            ScopeKeyword = scopeKeyword,
            ScopeName = scopeName,
            Constraints = constraints,
        };
    }

    private void ParseColumnTail(
        out DefaultClause? defaultClause,
        out IdentityColumn? identity,
        out GeneratedColumn? generated,
        out SyntaxToken? collateKeyword,
        out IReadOnlyList<SyntaxToken>? collation,
        out SyntaxToken? scopeKeyword,
        out IReadOnlyList<SyntaxToken>? scopeName,
        out IReadOnlyList<TableConstraint> constraints)
    {
        defaultClause = null;
        identity = null;
        generated = null;
        collateKeyword = null;
        collation = null;
        scopeKeyword = null;
        scopeName = null;
        var items = new List<TableConstraint>();
        while (true)
        {
            if (IdentifierEquals(Keyword.Default))
            {
                if (defaultClause is not null || identity is not null || generated is not null)
                {
                    throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
                }

                defaultClause = ParseDefaultClause();
                continue;
            }

            if (IdentifierEquals(Keyword.Generated))
            {
                if (defaultClause is not null || identity is not null || generated is not null)
                {
                    throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
                }

                ParseGenerated(out identity, out generated);
                continue;
            }

            if (_current.Kind == SyntaxKind.CollateKeyword)
            {
                if (collateKeyword is not null)
                {
                    throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
                }

                collateKeyword = Advance();
                collation = ParseQualifiedName();
                continue;
            }

            if (IdentifierEquals(Keyword.Scope))
            {
                if (scopeKeyword is not null)
                {
                    throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
                }

                scopeKeyword = Advance();
                scopeName = ParseQualifiedName();
                continue;
            }

            if (!IsColumnConstraintStart())
            {
                break;
            }

            items.Add(ParseColumnConstraint());
        }

        constraints = items;
    }

    private LikeClause ParseLikeClause()
    {
        var likeKeyword = Expect(SyntaxKind.LikeKeyword);
        var tableName = ParseQualifiedName();
        var options = new List<LikeOption>();
        var end = tableName[^1].Span;
        while (IdentifierEquals(Keyword.Including) || IdentifierEquals(Keyword.Excluding))
        {
            var kindKeyword = Advance();
            if (!IdentifierEquals(Keyword.Identity) && !IdentifierEquals(Keyword.Defaults) && !IdentifierEquals(Keyword.Generated))
            {
                throw new SqlParseException($"Expected IDENTITY, DEFAULTS, or GENERATED, found {_current.Kind}", _current.Position);
            }

            var option = Advance();
            options.Add(new LikeOption
            {
                Span = SourceSpan.From(kindKeyword, option),
                KindKeyword = kindKeyword,
                Option = option,
            });
            end = option.Span;
        }

        return new LikeClause
        {
            Span = SourceSpan.From(likeKeyword, end),
            LikeKeyword = likeKeyword,
            TableName = tableName,
            Options = options,
        };
    }

    private RefIsClause ParseRefIs()
    {
        var refKeyword = Advance();
        var isKeyword = Expect(SyntaxKind.IsKeyword);
        var name = Expect(SyntaxKind.Identifier);
        SyntaxToken? generation = null;
        SyntaxToken? generatedKeyword = null;
        var end = name.Span;
        if (IdentifierEquals(Keyword.System) || _current.Kind == SyntaxKind.UserKeyword || IdentifierEquals(Keyword.Derived))
        {
            generation = Advance();
            var generatedToken = ExpectIdent(Keyword.Generated);
            generatedKeyword = generatedToken;
            end = generatedToken.Span;
        }

        return new RefIsClause
        {
            Span = SourceSpan.From(refKeyword, end),
            RefKeyword = refKeyword,
            IsKeyword = isKeyword,
            Name = name,
            Generation = generation,
            GeneratedKeyword = generatedKeyword,
        };
    }

    private TableConstraint ParseTableConstraint() => ParseConstraint(column: false);

    private TableConstraint ParseColumnConstraint() => ParseConstraint(column: true);

    private TableConstraint ParseConstraint(bool column)
    {
        var start = _current;
        SyntaxToken? constraintKeyword = null;
        SyntaxToken? constraintName = null;
        if (IdentifierEquals(Keyword.Constraint))
        {
            constraintKeyword = Advance();
            constraintName = Expect(SyntaxKind.Identifier);
        }

        SyntaxToken? notKeyword = null;
        SyntaxToken? nullKeyword = null;
        SyntaxToken? uniqueKeyword = null;
        SyntaxToken? nullsKeyword = null;
        SyntaxToken? nullsNotKeyword = null;
        SyntaxToken? distinctKeyword = null;
        SyntaxToken? primaryKeyword = null;
        SyntaxToken? keyKeyword = null;
        SyntaxToken? foreignKeyword = null;
        SyntaxToken? columnsOpen = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        SyntaxToken? columnsClose = null;
        SyntaxToken? referencesKeyword = null;
        IReadOnlyList<SyntaxToken>? referenceTable = null;
        SyntaxToken? referenceOpen = null;
        IReadOnlyList<SyntaxToken>? referenceColumns = null;
        SyntaxToken? referenceClose = null;
        SyntaxToken? matchKeyword = null;
        SyntaxToken? matchType = null;
        IReadOnlyList<ReferentialAction> actions = [];
        SyntaxToken? checkKeyword = null;
        SyntaxToken? checkOpen = null;
        Expression? check = null;
        SyntaxToken? checkClose = null;
        if (!column && IdentifierEquals(Keyword.Foreign))
        {
            foreignKeyword = Advance();
            keyKeyword = ExpectIdent(Keyword.Key);
            ParseRequiredNameList(out var keyOpen, out var keyColumns, out var keyClose);
            columnsOpen = keyOpen;
            columns = keyColumns;
            columnsClose = keyClose;
            ParseReferences(out var referencesToken, out var referenced, out var refOpen, out var refColumns, out var refClose);
            referencesKeyword = referencesToken;
            referenceTable = referenced;
            referenceOpen = refOpen;
            referenceColumns = refColumns;
            referenceClose = refClose;
            ParseMatchAndActions(out matchKeyword, out matchType, out actions);
        }
        else if (IdentifierEquals(Keyword.Primary) || _current.Kind == SyntaxKind.UniqueKeyword)
        {
            if (IdentifierEquals(Keyword.Primary))
            {
                primaryKeyword = Advance();
                keyKeyword = ExpectIdent(Keyword.Key);
            }
            else
            {
                uniqueKeyword = Advance();
                ParseNullsDistinct(out nullsKeyword, out nullsNotKeyword, out distinctKeyword);
            }

            if (!column)
            {
                ParseRequiredNameList(out var keyOpen, out var keyColumns, out var keyClose);
                columnsOpen = keyOpen;
                columns = keyColumns;
                columnsClose = keyClose;
            }
        }
        else if (IdentifierEquals(Keyword.Check))
        {
            checkKeyword = Advance();
            checkOpen = Expect(SyntaxKind.OpenParen);
            check = ParseExpression();
            checkClose = Expect(SyntaxKind.CloseParen);
        }
        else if (IdentifierEquals(Keyword.References))
        {
            ParseReferences(out var referencesToken, out var referenced, out var refOpen, out var refColumns, out var refClose);
            referencesKeyword = referencesToken;
            referenceTable = referenced;
            referenceOpen = refOpen;
            referenceColumns = refColumns;
            referenceClose = refClose;
            ParseMatchAndActions(out matchKeyword, out matchType, out actions);
        }
        else if (column && _current.Kind == SyntaxKind.NotKeyword)
        {
            notKeyword = Advance();
            nullKeyword = Expect(SyntaxKind.NullKeyword);
        }
        else
        {
            throw new SqlParseException($"Expected constraint, found {_current.Kind}", _current.Position);
        }

        ParseConstraintCharacteristics(out var deferrableNot, out var deferrable, out var initially, out var initiallyWhen);
        var end = initiallyWhen?.Span
            ?? deferrable?.Span
            ?? (actions.Count > 0 ? actions[^1].Span : (SourceSpan?)null)
            ?? referenceClose?.Span
            ?? referenceTable?[^1].Span
            ?? checkClose?.Span
            ?? columnsClose?.Span
            ?? nullKeyword?.Span
            ?? keyKeyword?.Span
            ?? uniqueKeyword?.Span
            ?? constraintName?.Span
            ?? start.Span;
        return new TableConstraint
        {
            Span = SourceSpan.From(start, end),
            ConstraintKeyword = constraintKeyword,
            ConstraintName = constraintName,
            NotKeyword = notKeyword,
            NullKeyword = nullKeyword,
            UniqueKeyword = uniqueKeyword,
            NullsKeyword = nullsKeyword,
            NullsNotKeyword = nullsNotKeyword,
            DistinctKeyword = distinctKeyword,
            PrimaryKeyword = primaryKeyword,
            KeyKeyword = keyKeyword,
            ForeignKeyword = foreignKeyword,
            ColumnsOpen = columnsOpen,
            Columns = columns,
            ColumnsClose = columnsClose,
            ReferencesKeyword = referencesKeyword,
            ReferenceTable = referenceTable,
            ReferenceOpen = referenceOpen,
            ReferenceColumns = referenceColumns,
            ReferenceClose = referenceClose,
            MatchKeyword = matchKeyword,
            MatchType = matchType,
            Actions = actions,
            CheckKeyword = checkKeyword,
            CheckOpen = checkOpen,
            Check = check,
            CheckClose = checkClose,
            DeferrableNotKeyword = deferrableNot,
            DeferrableKeyword = deferrable,
            InitiallyKeyword = initially,
            InitiallyWhen = initiallyWhen,
        };
    }

    private void ParseNullsDistinct(
        out SyntaxToken? nullsKeyword,
        out SyntaxToken? notKeyword,
        out SyntaxToken? distinctKeyword)
    {
        nullsKeyword = null;
        notKeyword = null;
        distinctKeyword = null;
        if (!IdentifierEquals(Keyword.Nulls))
        {
            return;
        }

        nullsKeyword = Advance();
        if (_current.Kind == SyntaxKind.NotKeyword)
        {
            notKeyword = Advance();
        }

        distinctKeyword = Expect(SyntaxKind.DistinctKeyword);
    }

    private void ParseReferences(
        out SyntaxToken referencesKeyword,
        out IReadOnlyList<SyntaxToken> table,
        out SyntaxToken? openParen,
        out IReadOnlyList<SyntaxToken>? columns,
        out SyntaxToken? closeParen)
    {
        referencesKeyword = ExpectIdent(Keyword.References);
        table = ParseQualifiedName();
        openParen = null;
        columns = null;
        closeParen = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            ParseRequiredNameList(out var nameOpen, out var nameColumns, out var nameClose);
            openParen = nameOpen;
            columns = nameColumns;
            closeParen = nameClose;
        }
    }

    private void ParseMatchAndActions(
        out SyntaxToken? matchKeyword,
        out SyntaxToken? matchType,
        out IReadOnlyList<ReferentialAction> actions)
    {
        matchKeyword = null;
        matchType = null;
        var items = new List<ReferentialAction>();
        while (true)
        {
            if (matchKeyword is null && _current.Kind == SyntaxKind.MatchKeyword)
            {
                matchKeyword = Advance();
                if (_current.Kind is not (SyntaxKind.FullKeyword or SyntaxKind.PartialKeyword)
                    && !IdentifierEquals(Keyword.Simple))
                {
                    throw new SqlParseException($"Expected FULL, PARTIAL, or SIMPLE, found {_current.Kind}", _current.Position);
                }

                matchType = Advance();
                continue;
            }

            if (_current.Kind != SyntaxKind.OnKeyword)
            {
                break;
            }

            var onKeyword = Advance();
            if (_current.Kind != SyntaxKind.UpdateKeyword && !IdentifierEquals(Keyword.Delete))
            {
                throw new SqlParseException($"Expected UPDATE or DELETE, found {_current.Kind}", _current.Position);
            }

            var eventKeyword = Advance();
            SyntaxToken? noKeyword = null;
            SyntaxToken? setKeyword = null;
            SyntaxToken action;
            if (_current.Kind == SyntaxKind.SetKeyword)
            {
                setKeyword = Advance();
                if (_current.Kind != SyntaxKind.NullKeyword && !IdentifierEquals(Keyword.Default))
                {
                    throw new SqlParseException($"Expected NULL or DEFAULT, found {_current.Kind}", _current.Position);
                }

                action = Advance();
            }
            else if (IdentifierEquals(Keyword.No))
            {
                noKeyword = Advance();
                action = ExpectIdent(Keyword.Action);
            }
            else if (IdentifierEquals(Keyword.Cascade) || IdentifierEquals(Keyword.Restrict))
            {
                action = Advance();
            }
            else
            {
                throw new SqlParseException($"Expected referential action, found {_current.Kind}", _current.Position);
            }

            items.Add(new ReferentialAction
            {
                Span = SourceSpan.From(onKeyword, action),
                OnKeyword = onKeyword,
                Event = eventKeyword,
                NoKeyword = noKeyword,
                SetKeyword = setKeyword,
                Action = action,
            });
        }

        actions = items;
    }

    private void ParseConstraintCharacteristics(
        out SyntaxToken? notKeyword,
        out SyntaxToken? deferrableKeyword,
        out SyntaxToken? initiallyKeyword,
        out SyntaxToken? initiallyWhen)
    {
        notKeyword = null;
        deferrableKeyword = null;
        initiallyKeyword = null;
        initiallyWhen = null;
        while (true)
        {
            if (deferrableKeyword is null && IsDeferrableStart())
            {
                if (_current.Kind == SyntaxKind.NotKeyword)
                {
                    notKeyword = Advance();
                }

                deferrableKeyword = ExpectIdent(Keyword.Deferrable);
                continue;
            }

            if (initiallyKeyword is null && IdentifierEquals(Keyword.Initially))
            {
                initiallyKeyword = Advance();
                if (!IdentifierEquals(Keyword.Immediate) && !IdentifierEquals(Keyword.Deferred))
                {
                    throw new SqlParseException($"Expected IMMEDIATE or DEFERRED, found {_current.Kind}", _current.Position);
                }

                initiallyWhen = Advance();
                continue;
            }

            break;
        }
    }

    private void ParseGenerated(out IdentityColumn? identity, out GeneratedColumn? generated)
    {
        var generatedKeyword = Advance();
        ParseGenerationRule(out var alwaysKeyword, out var byKeyword, out var defaultKeyword);
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        if (IdentifierEquals(Keyword.Identity))
        {
            var identityKeyword = Advance();
            SyntaxToken? openParen = null;
            IReadOnlyList<SequenceOption> options = [];
            SyntaxToken? closeParen = null;
            var end = identityKeyword.Span;
            if (_current.Kind == SyntaxKind.OpenParen)
            {
                openParen = Advance();
                var items = new List<SequenceOption>();
                while (_current.Kind != SyntaxKind.CloseParen)
                {
                    items.Add(ParseSequenceOption());
                }

                options = items;
                var closeToken = Expect(SyntaxKind.CloseParen);
                closeParen = closeToken;
                end = closeToken.Span;
            }

            identity = new IdentityColumn
            {
                Span = SourceSpan.From(generatedKeyword, end),
                GeneratedKeyword = generatedKeyword,
                AlwaysKeyword = alwaysKeyword,
                ByKeyword = byKeyword,
                DefaultKeyword = defaultKeyword,
                AsKeyword = asKeyword,
                IdentityKeyword = identityKeyword,
                OpenParen = openParen,
                Options = options,
                CloseParen = closeParen,
            };
            generated = null;
            return;
        }

        identity = null;
        if (IdentifierEquals(Keyword.Row))
        {
            var rowKeyword = Advance();
            SyntaxToken bound;
            if (IdentifierEquals(Keyword.Start))
            {
                bound = Advance();
            }
            else if (_current.Kind == SyntaxKind.EndKeyword)
            {
                bound = Advance();
            }
            else
            {
                throw new SqlParseException($"Expected START or END, found {_current.Kind}", _current.Position);
            }

            generated = new GeneratedColumn
            {
                Span = SourceSpan.From(generatedKeyword, bound),
                GeneratedKeyword = generatedKeyword,
                AlwaysKeyword = alwaysKeyword,
                ByKeyword = byKeyword,
                DefaultKeyword = defaultKeyword,
                AsKeyword = asKeyword,
                RowKeyword = rowKeyword,
                Bound = bound,
            };
            return;
        }

        var open = Expect(SyntaxKind.OpenParen);
        var expression = ParseExpression();
        var close = Expect(SyntaxKind.CloseParen);
        generated = new GeneratedColumn
        {
            Span = SourceSpan.From(generatedKeyword, close),
            GeneratedKeyword = generatedKeyword,
            AlwaysKeyword = alwaysKeyword,
            ByKeyword = byKeyword,
            DefaultKeyword = defaultKeyword,
            AsKeyword = asKeyword,
            OpenParen = open,
            Expression = expression,
            CloseParen = close,
        };
    }

    private void ParseGenerationRule(
        out SyntaxToken? alwaysKeyword,
        out SyntaxToken? byKeyword,
        out SyntaxToken? defaultKeyword)
    {
        alwaysKeyword = null;
        byKeyword = null;
        defaultKeyword = null;
        if (IdentifierEquals(Keyword.Always))
        {
            alwaysKeyword = Advance();
            return;
        }

        if (_current.Kind != SyntaxKind.ByKeyword)
        {
            throw new SqlParseException($"Expected ALWAYS or BY DEFAULT, found {_current.Kind}", _current.Position);
        }

        byKeyword = Advance();
        defaultKeyword = ExpectIdent(Keyword.Default);
    }

    private bool IsBasicSequenceOption() =>
        _current.Kind == SyntaxKind.CycleKeyword
        || IdentifierEquals(Keyword.Increment)
        || IdentifierEquals(Keyword.MaxValue)
        || IdentifierEquals(Keyword.MinValue)
        || (IdentifierEquals(Keyword.No)
            && (NextKind == SyntaxKind.CycleKeyword || NextEquals(Keyword.MaxValue) || NextEquals(Keyword.MinValue)));

    private SequenceOption ParseSequenceOption()
    {
        if (IdentifierEquals(Keyword.No))
        {
            var noKeyword = Advance();
            if (_current.Kind == SyntaxKind.CycleKeyword
                || IdentifierEquals(Keyword.MaxValue)
                || IdentifierEquals(Keyword.MinValue))
            {
                var name = Advance();
                return new SequenceOption
                {
                    Span = SourceSpan.From(noKeyword, name),
                    NoKeyword = noKeyword,
                    Name = name,
                };
            }

            throw new SqlParseException($"Expected MAXVALUE, MINVALUE, or CYCLE, found {_current.Kind}", _current.Position);
        }

        if (_current.Kind == SyntaxKind.CycleKeyword)
        {
            var cycle = Advance();
            return new SequenceOption { Span = cycle.Span, Name = cycle };
        }

        if (IdentifierEquals(Keyword.Start))
        {
            var start = Advance();
            var withKeyword = Expect(SyntaxKind.WithKeyword);
            var value = ParseExpression();
            return new SequenceOption
            {
                Span = SourceSpan.From(start, value.Span),
                Name = start,
                WithKeyword = withKeyword,
                Value = value,
            };
        }

        if (IdentifierEquals(Keyword.Increment))
        {
            var increment = Advance();
            var byKeyword = Expect(SyntaxKind.ByKeyword);
            var value = ParseExpression();
            return new SequenceOption
            {
                Span = SourceSpan.From(increment, value.Span),
                Name = increment,
                ByKeyword = byKeyword,
                Value = value,
            };
        }

        if (IdentifierEquals(Keyword.MaxValue) || IdentifierEquals(Keyword.MinValue))
        {
            var name = Advance();
            var value = ParseExpression();
            return new SequenceOption
            {
                Span = SourceSpan.From(name, value.Span),
                Name = name,
                Value = value,
            };
        }

        throw new SqlParseException($"Expected sequence option, found {_current.Kind}", _current.Position);
    }

    private DefaultClause ParseDefaultClause()
    {
        var defaultKeyword = Advance();
        var value = ParseExpression();
        return new DefaultClause
        {
            Span = SourceSpan.From(defaultKeyword, value.Span),
            DefaultKeyword = defaultKeyword,
            Value = value,
        };
    }

    private void ParseWithData(out SyntaxToken withKeyword, out SyntaxToken? noKeyword, out SyntaxToken dataKeyword)
    {
        withKeyword = Expect(SyntaxKind.WithKeyword);
        noKeyword = IdentifierEquals(Keyword.No) ? Advance() : null;
        dataKeyword = ExpectIdent(Keyword.Data);
    }

    private void ParseOnCommit(
        out SyntaxToken? onKeyword,
        out SyntaxToken? commitKeyword,
        out SyntaxToken? action,
        out SyntaxToken? rowsKeyword)
    {
        onKeyword = null;
        commitKeyword = null;
        action = null;
        rowsKeyword = null;
        if (_current.Kind != SyntaxKind.OnKeyword)
        {
            return;
        }

        onKeyword = Advance();
        commitKeyword = ExpectIdent(Keyword.Commit);
        if (!IdentifierEquals(Keyword.Preserve) && !IdentifierEquals(Keyword.Delete))
        {
            throw new SqlParseException($"Expected PRESERVE or DELETE, found {_current.Kind}", _current.Position);
        }

        action = Advance();
        rowsKeyword = ExpectIdent(Keyword.Rows);
    }

    private void ParseRequiredNameList(
        out SyntaxToken openParen,
        out IReadOnlyList<SyntaxToken> names,
        out SyntaxToken closeParen)
    {
        openParen = Expect(SyntaxKind.OpenParen);
        names = ParseNameList();
        closeParen = Expect(SyntaxKind.CloseParen);
    }

    private List<IReadOnlyList<SyntaxToken>> ParseSchemaNameList()
    {
        var names = new List<IReadOnlyList<SyntaxToken>> { ParseQualifiedName() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            names.Add(ParseQualifiedName());
        }

        return names;
    }

    private SyntaxToken ParseDropBehavior()
    {
        if (!IdentifierEquals(Keyword.Cascade) && !IdentifierEquals(Keyword.Restrict))
        {
            throw new SqlParseException($"Expected CASCADE or RESTRICT, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    private SyntaxToken ExpectIdent(string keyword)
    {
        if (!IdentifierEquals(keyword))
        {
            throw new SqlParseException($"Expected {keyword.ToUpperInvariant()}, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    private bool IsAsColumnList()
    {
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return false;
        }

        var index = _index;
        if (index >= _tokens.Count || _tokens[index].Kind != SyntaxKind.Identifier)
        {
            return false;
        }

        while (index < _tokens.Count && _tokens[index].Kind == SyntaxKind.Identifier)
        {
            index++;
            if (index >= _tokens.Count)
            {
                return false;
            }

            if (_tokens[index].Kind == SyntaxKind.Comma)
            {
                index++;
                continue;
            }

            if (_tokens[index].Kind != SyntaxKind.CloseParen)
            {
                return false;
            }

            var after = index + 1;
            return after < _tokens.Count && _tokens[after].Kind == SyntaxKind.AsKeyword;
        }

        return false;
    }

    private bool IsTableConstraintStart() =>
        IdentifierEquals(Keyword.Constraint)
        || IdentifierEquals(Keyword.Primary)
        || _current.Kind == SyntaxKind.UniqueKeyword
        || IdentifierEquals(Keyword.Foreign)
        || IdentifierEquals(Keyword.Check);

    private bool IsColumnConstraintStart() =>
        IsTableConstraintStart()
        || _current.Kind == SyntaxKind.NotKeyword
        || IdentifierEquals(Keyword.References);

    private bool IsPeriodDefinition() =>
        IdentifierEquals(Keyword.Period) && NextKind == SyntaxKind.ForKeyword;

    private bool IsSystemVersioning() =>
        IdentifierEquals(Keyword.System) && NextEquals(Keyword.Versioning);

    private void ParseSystemVersioning(
        out SyntaxToken? withKeyword,
        out SyntaxToken? systemKeyword,
        out SyntaxToken? versioningKeyword)
    {
        withKeyword = null;
        systemKeyword = null;
        versioningKeyword = null;
        if (_current.Kind != SyntaxKind.WithKeyword || !NextEquals(Keyword.System))
        {
            return;
        }

        var versioningIndex = _index + 1;
        if (versioningIndex >= _tokens.Count || !TokenEquals(_tokens[versioningIndex], Keyword.Versioning))
        {
            return;
        }

        withKeyword = Advance();
        systemKeyword = Advance();
        versioningKeyword = Advance();
    }

    private PeriodDefinition ParsePeriodDefinition()
    {
        var periodKeyword = Advance();
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var name = Expect(SyntaxKind.Identifier);
        var openParen = Expect(SyntaxKind.OpenParen);
        var startColumn = Expect(SyntaxKind.Identifier);
        var comma = Expect(SyntaxKind.Comma);
        var endColumn = Expect(SyntaxKind.Identifier);
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PeriodDefinition
        {
            Span = SourceSpan.From(periodKeyword, closeParen),
            PeriodKeyword = periodKeyword,
            ForKeyword = forKeyword,
            Name = name,
            OpenParen = openParen,
            StartColumn = startColumn,
            Comma = comma,
            EndColumn = endColumn,
            CloseParen = closeParen,
        };
    }

    private bool IsRefIs() =>
        IdentifierEquals(Keyword.Ref) && NextKind == SyntaxKind.IsKeyword;

    private bool IsWithOptions() =>
        _current.Kind == SyntaxKind.Identifier
        && NextKind == SyntaxKind.WithKeyword
        && _index + 1 < _tokens.Count
        && TokenEquals(_tokens[_index + 1], Keyword.Options);

    private bool IsDeferrableStart() =>
        IdentifierEquals(Keyword.Deferrable)
        || (_current.Kind == SyntaxKind.NotKeyword && NextEquals(Keyword.Deferrable));
}
