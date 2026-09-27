namespace QlParse;

internal sealed partial class Parser
{
    internal AlterColumnAction ParseAlterColumn()
    {
        var alterKeyword = Advance();
        var columnKeyword = IdentifierEquals(Keyword.Column) ? Advance() : (SyntaxToken?)null;
        var name = Expect(SyntaxKind.Identifier);

        if (_current.Kind == SyntaxKind.SetKeyword)
        {
            return ParseAlterColumnSet(alterKeyword, columnKeyword, name);
        }

        if (IdentifierEquals(Keyword.Drop))
        {
            return ParseAlterColumnDrop(alterKeyword, columnKeyword, name);
        }

        if (IdentifierEquals(Keyword.Add))
        {
            return ParseAlterColumnAdd(alterKeyword, columnKeyword, name);
        }

        if (IdentifierEquals(Keyword.Restart))
        {
            return ParseAlterColumnRestart(alterKeyword, columnKeyword, name);
        }

        throw new SqlParseException($"Expected column alter action, found {_current.Kind}", _current.Position);
    }

    private AlterColumnAction ParseAlterColumnSet(SyntaxToken alterKeyword, SyntaxToken? columnKeyword, SyntaxToken name)
    {
        var setKeyword = Advance();
        if (IdentifierEquals(Keyword.Data))
        {
            var dataKeyword = Advance();
            var typeKeyword = ExpectIdent(Keyword.Type);
            var dataType = ParseDataType();
            return new AlterColumnAction
            {
                Span = SourceSpan.From(alterKeyword, dataType.Span),
                AlterKeyword = alterKeyword,
                ColumnKeyword = columnKeyword,
                Name = name,
                SetKeyword = setKeyword,
                DataKeyword = dataKeyword,
                TypeKeyword = typeKeyword,
                DataType = dataType,
            };
        }

        if (IdentifierEquals(Keyword.Default))
        {
            var defaultClause = ParseDefaultClause();
            return new AlterColumnAction
            {
                Span = SourceSpan.From(alterKeyword, defaultClause.Span),
                AlterKeyword = alterKeyword,
                ColumnKeyword = columnKeyword,
                Name = name,
                SetKeyword = setKeyword,
                Default = defaultClause,
            };
        }

        if (_current.Kind == SyntaxKind.NotKeyword)
        {
            var notKeyword = Advance();
            var nullToken = Expect(SyntaxKind.NullKeyword);
            return new AlterColumnAction
            {
                Span = SourceSpan.From(alterKeyword, nullToken.Span),
                AlterKeyword = alterKeyword,
                ColumnKeyword = columnKeyword,
                Name = name,
                SetKeyword = setKeyword,
                NotKeyword = notKeyword,
                NullKeyword = nullToken,
            };
        }

        if (IdentifierEquals(Keyword.Generated))
        {
            return ParseAlterColumnSetGenerated(alterKeyword, columnKeyword, name, setKeyword);
        }

        if (IsBasicSequenceOption())
        {
            var option = ParseSequenceOption();
            return new AlterColumnAction
            {
                Span = SourceSpan.From(alterKeyword, option.Span),
                AlterKeyword = alterKeyword,
                ColumnKeyword = columnKeyword,
                Name = name,
                SetKeyword = setKeyword,
                IdentityOption = option,
            };
        }

        throw new SqlParseException($"Expected column alter action, found {_current.Kind}", _current.Position);
    }

    private AlterColumnAction ParseAlterColumnSetGenerated(SyntaxToken alterKeyword, SyntaxToken? columnKeyword, SyntaxToken name, SyntaxToken setKeyword)
    {
        var generatedToken = Advance();
        ParseGenerationRule(out var alwaysToken, out var byToken, out var generationDefaultToken);
        var end = (generationDefaultToken ?? alwaysToken ?? generatedToken).Span;
        return new AlterColumnAction
        {
            Span = SourceSpan.From(alterKeyword, end),
            AlterKeyword = alterKeyword,
            ColumnKeyword = columnKeyword,
            Name = name,
            SetKeyword = setKeyword,
            GeneratedKeyword = generatedToken,
            AlwaysKeyword = alwaysToken,
            ByKeyword = byToken,
            DefaultKeyword = generationDefaultToken,
        };
    }

    private AlterColumnAction ParseAlterColumnDrop(SyntaxToken alterKeyword, SyntaxToken? columnKeyword, SyntaxToken name)
    {
        var dropKeyword = Advance();
        if (IdentifierEquals(Keyword.Default))
        {
            var defaultToken = Advance();
            return new AlterColumnAction
            {
                Span = SourceSpan.From(alterKeyword, defaultToken.Span),
                AlterKeyword = alterKeyword,
                ColumnKeyword = columnKeyword,
                Name = name,
                DropKeyword = dropKeyword,
                DefaultKeyword = defaultToken,
            };
        }

        if (_current.Kind == SyntaxKind.NotKeyword)
        {
            var notKeyword = Advance();
            var nullToken = Expect(SyntaxKind.NullKeyword);
            return new AlterColumnAction
            {
                Span = SourceSpan.From(alterKeyword, nullToken.Span),
                AlterKeyword = alterKeyword,
                ColumnKeyword = columnKeyword,
                Name = name,
                DropKeyword = dropKeyword,
                NotKeyword = notKeyword,
                NullKeyword = nullToken,
            };
        }

        if (IdentifierEquals(Keyword.Scope))
        {
            var scopeKeyword = Advance();
            var dropBehavior = ParseDropBehavior();
            return new AlterColumnAction
            {
                Span = SourceSpan.From(alterKeyword, dropBehavior.Span),
                AlterKeyword = alterKeyword,
                ColumnKeyword = columnKeyword,
                Name = name,
                DropKeyword = dropKeyword,
                ScopeKeyword = scopeKeyword,
                Behavior = dropBehavior,
            };
        }

        if (IdentifierEquals(Keyword.Identity))
        {
            var identityToken = Advance();
            return new AlterColumnAction
            {
                Span = SourceSpan.From(alterKeyword, identityToken.Span),
                AlterKeyword = alterKeyword,
                ColumnKeyword = columnKeyword,
                Name = name,
                DropKeyword = dropKeyword,
                IdentityKeyword = identityToken,
            };
        }

        throw new SqlParseException($"Expected DROP action, found {_current.Kind}", _current.Position);
    }

    private AlterColumnAction ParseAlterColumnAdd(SyntaxToken alterKeyword, SyntaxToken? columnKeyword, SyntaxToken name)
    {
        var addKeyword = Advance();
        var scopeKeyword = ExpectIdent(Keyword.Scope);
        var scopeName = ParseQualifiedName();
        return new AlterColumnAction
        {
            Span = SourceSpan.From(alterKeyword, scopeName[^1].Span),
            AlterKeyword = alterKeyword,
            ColumnKeyword = columnKeyword,
            Name = name,
            AddKeyword = addKeyword,
            ScopeKeyword = scopeKeyword,
            ScopeName = scopeName,
        };
    }

    private AlterColumnAction ParseAlterColumnRestart(SyntaxToken alterKeyword, SyntaxToken? columnKeyword, SyntaxToken name)
    {
        var restartToken = Advance();
        SyntaxToken? withKeyword = null;
        Expression? restartValue = null;
        var end = restartToken.Span;
        if (_current.Kind == SyntaxKind.WithKeyword)
        {
            withKeyword = Advance();
            restartValue = ParseExpression();
            end = restartValue.Span;
        }

        return new AlterColumnAction
        {
            Span = SourceSpan.From(alterKeyword, end),
            AlterKeyword = alterKeyword,
            ColumnKeyword = columnKeyword,
            Name = name,
            RestartKeyword = restartToken,
            WithKeyword = withKeyword,
            RestartValue = restartValue,
        };
    }

    internal void ParseTableElements(
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

        var tail = ParseColumnTail();
        var defaultClause = tail.DefaultClause;
        var identity = tail.Identity;
        var generated = tail.Generated;
        var collateKeyword = tail.CollateKeyword;
        var collation = tail.Collation;
        var scopeKeyword = tail.ScopeKeyword;
        var scopeName = tail.ScopeName;
        var constraints = tail.Constraints;
        var end = ParseColumnEnd(name, type, optionsKeyword, tail);

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

    private static SourceSpan ParseColumnEnd(SyntaxToken name, DataType? type, SyntaxToken? optionsKeyword, ColumnTail tail) =>
        (tail.Constraints.Count > 0 ? tail.Constraints[^1].Span : (SourceSpan?)null)
        ?? tail.ScopeName?[^1].Span
        ?? tail.Collation?[^1].Span
        ?? tail.Generated?.Span
        ?? tail.Identity?.Span
        ?? tail.DefaultClause?.Span
        ?? optionsKeyword?.Span
        ?? type?.Span
        ?? name.Span;

    private readonly record struct ColumnTail(
        DefaultClause? DefaultClause,
        IdentityColumn? Identity,
        GeneratedColumn? Generated,
        SyntaxToken? CollateKeyword,
        IReadOnlyList<SyntaxToken>? Collation,
        SyntaxToken? ScopeKeyword,
        IReadOnlyList<SyntaxToken>? ScopeName,
        IReadOnlyList<TableConstraint> Constraints);

    private ColumnTail ParseColumnTail()
    {
        DefaultClause? defaultClause = null;
        IdentityColumn? identity = null;
        GeneratedColumn? generated = null;
        SyntaxToken? collateKeyword = null;
        IReadOnlyList<SyntaxToken>? collation = null;
        SyntaxToken? scopeKeyword = null;
        IReadOnlyList<SyntaxToken>? scopeName = null;
        var items = new List<TableConstraint>();
        while (true)
        {
            if (IdentifierEquals(Keyword.Default))
            {
                EnsureColumnValueNotSet(defaultClause, identity, generated);
                defaultClause = ParseDefaultClause();
                continue;
            }

            if (IdentifierEquals(Keyword.Generated))
            {
                EnsureColumnValueNotSet(defaultClause, identity, generated);
                ParseGenerated(out var identityToken, out var generatedToken);
                identity = identityToken;
                generated = generatedToken;
                continue;
            }

            if (_current.Kind == SyntaxKind.CollateKeyword)
            {
                EnsureNotAlreadySet(collateKeyword);
                collateKeyword = Advance();
                collation = ParseQualifiedName();
                continue;
            }

            if (IdentifierEquals(Keyword.Scope))
            {
                EnsureNotAlreadySet(scopeKeyword);
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

        return new ColumnTail(defaultClause, identity, generated, collateKeyword, collation, scopeKeyword, scopeName, items);
    }

    private void EnsureColumnValueNotSet(DefaultClause? defaultClause, IdentityColumn? identity, GeneratedColumn? generated)
    {
        if (defaultClause is not null || identity is not null || generated is not null)
        {
            throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
        }
    }

    private void EnsureNotAlreadySet(SyntaxToken? existing)
    {
        if (existing is not null)
        {
            throw new SqlParseException($"Unexpected {_current.Kind}", _current.Position);
        }
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

}
