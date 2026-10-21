namespace QlParse;

internal sealed partial class Parser
{
    // Extras is null when neither COLLATE nor USING is present.
    internal readonly record struct PgTypeConversion(PgAlterColumnExtras? Extras, SourceSpan? End);

    // `[COLLATE collation] [USING expression]` after ALTER COLUMN ... TYPE.
    internal PgTypeConversion ParsePgTypeConversion()
    {
        SyntaxToken? collateKeyword = null;
        IReadOnlyList<SyntaxToken>? collation = null;
        SyntaxToken? usingKeyword = null;
        Expression? usingExpression = null;
        if (_current.Kind == SyntaxKind.CollateKeyword)
        {
            collateKeyword = Advance();
            collation = ParseQualifiedName();
        }

        if (_current.Kind == SyntaxKind.UsingKeyword)
        {
            usingKeyword = Advance();
            usingExpression = ParseExpression();
        }

        var end = usingExpression?.Span ?? collation?[^1].Span;
        var extras = end is null
            ? null
            : new PgAlterColumnExtras
            {
                CollateKeyword = collateKeyword,
                Collation = collation,
                UsingKeyword = usingKeyword,
                Using = usingExpression,
            };
        return new PgTypeConversion(extras, end);
    }

    // `ALTER [COLUMN] c TYPE t` shorthand and `ALTER [COLUMN] c RESET (...)`.
    internal AlterColumnAction? TryParsePgAlterColumnAction(SyntaxToken alterKeyword, SyntaxToken? columnKeyword, SyntaxToken name)
    {
        var head = new AlterColumnHead(alterKeyword, columnKeyword, name);
        if (IdentifierEquals(Keyword.Type))
        {
            return ParseAlterColumnType(head, null, null);
        }

        if (!IdentifierEquals(Keyword.DdlReset) || NextKind != SyntaxKind.OpenParen)
        {
            return TryParsePgAlterColumnOptions(head);
        }

        var resetKeyword = Advance();
        var parameters = ParseStorageParameterList();
        return new AlterColumnAction
        {
            Span = SourceSpan.From(alterKeyword, parameters.Span),
            AlterKeyword = alterKeyword,
            ColumnKeyword = columnKeyword,
            Name = name,
            Postgres = new PgAlterColumnExtras
            {
                ResetKeyword = resetKeyword,
                Parameters = parameters,
            },
        };
    }

    // `SET STATISTICS n` | `SET STORAGE mode` | `SET COMPRESSION method` | `SET (attribute_option = value, ...)`.
    internal AlterColumnAction ParsePgAlterColumnSet(AlterColumnHead head, SyntaxToken setKeyword)
    {
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            var parameters = ParseStorageParameterList();
            return new AlterColumnAction
            {
                Span = SourceSpan.From(head.AlterKeyword, parameters.Span),
                AlterKeyword = head.AlterKeyword,
                ColumnKeyword = head.ColumnKeyword,
                Name = head.Name,
                SetKeyword = setKeyword,
                Postgres = new PgAlterColumnExtras { Parameters = parameters },
            };
        }

        if (IdentifierEquals(Keyword.DdlStatistics))
        {
            var statisticsKeyword = Advance();
            var value = ParseExpression();
            return new AlterColumnAction
            {
                Span = SourceSpan.From(head.AlterKeyword, value.Span),
                AlterKeyword = head.AlterKeyword,
                ColumnKeyword = head.ColumnKeyword,
                Name = head.Name,
                SetKeyword = setKeyword,
                Postgres = new PgAlterColumnExtras
                {
                    AttributeKeyword = statisticsKeyword,
                    AttributeValue = value,
                },
            };
        }

        if (!IdentifierEquals(Keyword.DdlStorage) && !IdentifierEquals(Keyword.Compression))
        {
            throw new SqlParseException($"Expected column alter action, found {_current.Kind}", _current.Position);
        }

        var attributeKeyword = Advance();
        var attributeName = Expect(SyntaxKind.Identifier);
        return new AlterColumnAction
        {
            Span = SourceSpan.From(head.AlterKeyword, attributeName),
            AlterKeyword = head.AlterKeyword,
            ColumnKeyword = head.ColumnKeyword,
            Name = head.Name,
            SetKeyword = setKeyword,
            Postgres = new PgAlterColumnExtras
            {
                AttributeKeyword = attributeKeyword,
                AttributeName = attributeName,
            },
        };
    }

    // `DROP EXPRESSION [IF EXISTS]`.
    internal AlterColumnAction ParsePgAlterColumnDropExpression(AlterColumnHead head, SyntaxToken dropKeyword)
    {
        var expressionKeyword = Advance();
        var ifExists = TryParsePgExistenceCheck();
        return new AlterColumnAction
        {
            Span = SourceSpan.From(head.AlterKeyword, ifExists?.Span ?? expressionKeyword.Span),
            AlterKeyword = head.AlterKeyword,
            ColumnKeyword = head.ColumnKeyword,
            Name = head.Name,
            DropKeyword = dropKeyword,
            Postgres = new PgAlterColumnExtras
            {
                ExpressionKeyword = expressionKeyword,
                IfExists = ifExists,
            },
        };
    }

    // `ADD GENERATED {ALWAYS | BY DEFAULT} AS IDENTITY [(sequence_options)]`.
    internal AlterColumnAction ParsePgAlterColumnAddIdentity(AlterColumnHead head, SyntaxToken addKeyword)
    {
        ParseGenerated(out var identity, out _);
        if (identity is null)
        {
            throw new SqlParseException($"Expected IDENTITY, found {_current.Kind}", _current.Position);
        }

        return new AlterColumnAction
        {
            Span = SourceSpan.From(head.AlterKeyword, identity.Span),
            AlterKeyword = head.AlterKeyword,
            ColumnKeyword = head.ColumnKeyword,
            Name = head.Name,
            AddKeyword = addKeyword,
            Postgres = new PgAlterColumnExtras { Identity = identity },
        };
    }

    internal bool IsPgSequenceOption() =>
        IdentifierEquals(Keyword.DdlCache)
        || IdentifierEquals(Keyword.DdlOwned)
        || IdentifierEquals(Keyword.Restart)
        || (IdentifierEquals(Keyword.Sequence) && NextEquals(Keyword.Name));

    // Postgres sequence options: `CACHE n`, `OWNED BY {table.column | NONE}`, `RESTART [[WITH] n]`,
    // `SEQUENCE NAME name` (identity columns only).
    internal SequenceOption ParsePgSequenceOption()
    {
        if (IdentifierEquals(Keyword.DdlCache))
        {
            var cache = Advance();
            var value = ParseExpression();
            return new SequenceOption { Span = SourceSpan.From(cache, value.Span), Name = cache, Value = value };
        }

        if (IdentifierEquals(Keyword.DdlOwned))
        {
            var owned = Advance();
            var byKeyword = Expect(SyntaxKind.ByKeyword);
            var target = ParseQualifiedName();
            return new SequenceOption
            {
                Span = SourceSpan.From(owned, target[^1]),
                Name = owned,
                ByKeyword = byKeyword,
                Postgres = new PgSequenceOptionExtras { Target = target },
            };
        }

        if (IdentifierEquals(Keyword.Restart))
        {
            return ParsePgRestartOption();
        }

        if (!IdentifierEquals(Keyword.Sequence) || !NextEquals(Keyword.Name))
        {
            throw new SqlParseException($"Expected sequence option, found {_current.Kind}", _current.Position);
        }

        var sequenceKeyword = Advance();
        var nameKeyword = Advance();
        var sequenceName = ParseQualifiedName();
        return new SequenceOption
        {
            Span = SourceSpan.From(sequenceKeyword, sequenceName[^1]),
            Name = sequenceKeyword,
            Postgres = new PgSequenceOptionExtras
            {
                NameKeyword = nameKeyword,
                Target = sequenceName,
            },
        };
    }

    private SequenceOption ParsePgRestartOption()
    {
        var restart = Advance();
        var withKeyword = _current.Kind == SyntaxKind.WithKeyword ? Advance() : (SyntaxToken?)null;
        Expression? value = null;
        if (withKeyword is not null || _current.Kind is SyntaxKind.Number or SyntaxKind.MinusToken or SyntaxKind.PlusToken)
        {
            value = ParseExpression();
        }

        return new SequenceOption
        {
            Span = SourceSpan.From(restart, value?.Span ?? restart.Span),
            Name = restart,
            WithKeyword = withKeyword,
            Value = value,
        };
    }
}
