namespace QlParse;

internal sealed partial class Parser
{
    private bool IsCreateAssertion() =>
        IdentifierEquals(Keyword.Create) && NextEquals(Keyword.Assertion);

    private bool IsDropAssertion() =>
        IdentifierEquals(Keyword.Drop) && NextEquals(Keyword.Assertion);

    private bool IsCreateCharacterSet() =>
        IdentifierEquals(Keyword.Create) && NextEquals(Keyword.Character) && NextNextIsSet();

    private bool IsDropCharacterSet() =>
        IdentifierEquals(Keyword.Drop) && NextEquals(Keyword.Character) && NextNextIsSet();

    private bool IsCreateCollation() =>
        IdentifierEquals(Keyword.Create) && NextEquals(Keyword.Collation);

    private bool IsDropCollation() =>
        IdentifierEquals(Keyword.Drop) && NextEquals(Keyword.Collation);

    private bool IsCreateTranslation() =>
        IdentifierEquals(Keyword.Create) && NextEquals(Keyword.Translation);

    private bool IsDropTranslation() =>
        IdentifierEquals(Keyword.Drop) && NextEquals(Keyword.Translation);

    private bool IsCreateSequence() =>
        IdentifierEquals(Keyword.Create) && NextEquals(Keyword.Sequence);

    private bool IsDropSequence() =>
        IdentifierEquals(Keyword.Drop) && NextEquals(Keyword.Sequence);

    private bool IsCreateIndex()
    {
        if (!IdentifierEquals(Keyword.Create))
        {
            return false;
        }

        var index = _index;
        if (index < _tokens.Count && _tokens[index].Kind == SyntaxKind.UniqueKeyword)
        {
            index++;
        }

        if (index < _tokens.Count
            && (TokenEquals(_tokens[index], Keyword.Clustered) || TokenEquals(_tokens[index], Keyword.Nonclustered)))
        {
            index++;
        }

        return index < _tokens.Count && TokenEquals(_tokens[index], Keyword.Index);
    }

    private bool IsAlterIndex() =>
        IdentifierEquals(Keyword.Alter) && NextEquals(Keyword.Index);

    private bool IsDropIndex() =>
        IdentifierEquals(Keyword.Drop) && NextEquals(Keyword.Index);

    private bool IsComment() =>
        IdentifierEquals(Keyword.Comment) && NextKind == SyntaxKind.OnKeyword;

    private bool NextNextIsSet() =>
        _index + 1 < _tokens.Count && _tokens[_index + 1].Kind == SyntaxKind.SetKeyword;

    private CreateAssertionStatement ParseCreateAssertion()
    {
        var createKeyword = Advance();
        var assertionKeyword = ExpectIdent(Keyword.Assertion);
        var name = ParseQualifiedName();
        var checkKeyword = ExpectIdent(Keyword.Check);
        var openParen = Expect(SyntaxKind.OpenParen);
        var check = ParseExpression();
        var closeParen = Expect(SyntaxKind.CloseParen);
        ParseConstraintCharacteristics(out var notKeyword, out var deferrable, out var initially, out var initiallyWhen);
        var end = initiallyWhen?.Span ?? deferrable?.Span ?? closeParen.Span;
        return new CreateAssertionStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            AssertionKeyword = assertionKeyword,
            Name = name,
            CheckKeyword = checkKeyword,
            OpenParen = openParen,
            Check = check,
            CloseParen = closeParen,
            DeferrableNotKeyword = notKeyword,
            DeferrableKeyword = deferrable,
            InitiallyKeyword = initially,
            InitiallyWhen = initiallyWhen,
        };
    }

    private DropAssertionStatement ParseDropAssertion()
    {
        var dropKeyword = Advance();
        var assertionKeyword = ExpectIdent(Keyword.Assertion);
        var name = ParseQualifiedName();
        var behavior = ParseDropBehavior();
        return new DropAssertionStatement
        {
            Span = SourceSpan.From(dropKeyword, behavior),
            DropKeyword = dropKeyword,
            AssertionKeyword = assertionKeyword,
            Name = name,
            Behavior = behavior,
        };
    }

    private CreateCharacterSetStatement ParseCreateCharacterSet()
    {
        var createKeyword = Advance();
        var characterKeyword = ExpectIdent(Keyword.Character);
        var setKeyword = Expect(SyntaxKind.SetKeyword);
        var name = ParseQualifiedName();
        var asKeyword = _current.Kind == SyntaxKind.AsKeyword ? Advance() : (SyntaxToken?)null;
        var getKeyword = ExpectIdent(Keyword.Get);
        var source = ParseQualifiedName();
        SyntaxToken? collateKeyword = null;
        IReadOnlyList<SyntaxToken>? collation = null;
        var end = source[^1].Span;
        if (_current.Kind == SyntaxKind.CollateKeyword)
        {
            collateKeyword = Advance();
            collation = ParseQualifiedName();
            end = collation[^1].Span;
        }

        return new CreateCharacterSetStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            CharacterKeyword = characterKeyword,
            SetKeyword = setKeyword,
            Name = name,
            AsKeyword = asKeyword,
            GetKeyword = getKeyword,
            Source = source,
            CollateKeyword = collateKeyword,
            Collation = collation,
        };
    }

    private DropCharacterSetStatement ParseDropCharacterSet()
    {
        var dropKeyword = Advance();
        var characterKeyword = ExpectIdent(Keyword.Character);
        var setKeyword = Expect(SyntaxKind.SetKeyword);
        var name = ParseQualifiedName();
        return new DropCharacterSetStatement
        {
            Span = SourceSpan.From(dropKeyword, name[^1]),
            DropKeyword = dropKeyword,
            CharacterKeyword = characterKeyword,
            SetKeyword = setKeyword,
            Name = name,
        };
    }

    private CreateCollationStatement ParseCreateCollation()
    {
        var createKeyword = Advance();
        var collationKeyword = ExpectIdent(Keyword.Collation);
        var name = ParseQualifiedName();
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var characterSet = ParseQualifiedName();
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var source = ParseQualifiedName();
        SyntaxToken? noKeyword = null;
        SyntaxToken? padKeyword = null;
        SyntaxToken? spaceKeyword = null;
        var end = source[^1].Span;
        if (IdentifierEquals(Keyword.No) || IdentifierEquals(Keyword.Pad))
        {
            if (IdentifierEquals(Keyword.No))
            {
                noKeyword = Advance();
            }

            var pad = ExpectIdent(Keyword.Pad);
            padKeyword = pad;
            if (noKeyword is null)
            {
                var space = ExpectIdent(Keyword.Space);
                spaceKeyword = space;
                end = space.Span;
            }
            else
            {
                end = pad.Span;
            }
        }

        return new CreateCollationStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            CollationKeyword = collationKeyword,
            Name = name,
            ForKeyword = forKeyword,
            CharacterSet = characterSet,
            FromKeyword = fromKeyword,
            Source = source,
            NoKeyword = noKeyword,
            PadKeyword = padKeyword,
            SpaceKeyword = spaceKeyword,
        };
    }

    private DropCollationStatement ParseDropCollation()
    {
        var dropKeyword = Advance();
        var collationKeyword = ExpectIdent(Keyword.Collation);
        var name = ParseQualifiedName();
        var behavior = ParseDropBehavior();
        return new DropCollationStatement
        {
            Span = SourceSpan.From(dropKeyword, behavior),
            DropKeyword = dropKeyword,
            CollationKeyword = collationKeyword,
            Name = name,
            Behavior = behavior,
        };
    }

    private CreateTranslationStatement ParseCreateTranslation()
    {
        var createKeyword = Advance();
        var translationKeyword = ExpectIdent(Keyword.Translation);
        var name = ParseQualifiedName();
        var forKeyword = Expect(SyntaxKind.ForKeyword);
        var sourceCharacterSet = ParseQualifiedName();
        var toKeyword = ExpectIdent(Keyword.To);
        var targetCharacterSet = ParseQualifiedName();
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        IReadOnlyList<SyntaxToken>? existing = null;
        RoutineDesignator? routine = null;
        SourceSpan end;
        if (IsRoutineDesignatorStart())
        {
            routine = ParseRoutineDesignator();
            end = routine.Span;
        }
        else
        {
            existing = ParseQualifiedName();
            end = existing[^1].Span;
        }

        return new CreateTranslationStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            TranslationKeyword = translationKeyword,
            Name = name,
            ForKeyword = forKeyword,
            SourceCharacterSet = sourceCharacterSet,
            ToKeyword = toKeyword,
            TargetCharacterSet = targetCharacterSet,
            FromKeyword = fromKeyword,
            Existing = existing,
            Routine = routine,
        };
    }

    private DropTranslationStatement ParseDropTranslation()
    {
        var dropKeyword = Advance();
        var translationKeyword = ExpectIdent(Keyword.Translation);
        var name = ParseQualifiedName();
        return new DropTranslationStatement
        {
            Span = SourceSpan.From(dropKeyword, name[^1]),
            DropKeyword = dropKeyword,
            TranslationKeyword = translationKeyword,
            Name = name,
        };
    }

    private CreateSequenceStatement ParseCreateSequence()
    {
        var createKeyword = Advance();
        var sequenceKeyword = ExpectIdent(Keyword.Sequence);
        var name = ParseQualifiedName();
        SyntaxToken? asKeyword = null;
        DataType? dataType = null;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            dataType = ParseDataType();
        }

        var options = new List<SequenceOption>();
        while (IsSequenceOption())
        {
            options.Add(ParseSequenceOption());
        }

        var end = name[^1].Span;
        if (dataType is not null)
        {
            end = dataType.Span;
        }

        if (options.Count > 0)
        {
            end = options[^1].Span;
        }

        return new CreateSequenceStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            SequenceKeyword = sequenceKeyword,
            Name = name,
            AsKeyword = asKeyword,
            DataType = dataType,
            Options = options,
        };
    }

    private DropSequenceStatement ParseDropSequence()
    {
        var dropKeyword = Advance();
        var sequenceKeyword = ExpectIdent(Keyword.Sequence);
        var name = ParseQualifiedName();
        var behavior = ParseDropBehavior();
        return new DropSequenceStatement
        {
            Span = SourceSpan.From(dropKeyword, behavior),
            DropKeyword = dropKeyword,
            SequenceKeyword = sequenceKeyword,
            Name = name,
            Behavior = behavior,
        };
    }

    private CreateIndexStatement ParseCreateIndex()
    {
        var createKeyword = Advance();
        var unique = _current.Kind == SyntaxKind.UniqueKeyword ? Advance() : (SyntaxToken?)null;
        SyntaxToken? clustered = null;
        if (IdentifierEquals(Keyword.Clustered) || IdentifierEquals(Keyword.Nonclustered))
        {
            clustered = Advance();
        }

        var indexKeyword = ExpectIdent(Keyword.Index);
        var name = ParseQualifiedName();
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var tableName = ParseQualifiedName();
        var openParen = Expect(SyntaxKind.OpenParen);
        var columns = new List<IndexColumn> { ParseIndexColumn() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            columns.Add(ParseIndexColumn());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        SyntaxToken? whereKeyword = null;
        Expression? where = null;
        var end = closeParen.Span;
        if (_current.Kind == SyntaxKind.WhereKeyword)
        {
            whereKeyword = Advance();
            where = ParseExpression();
            end = where.Span;
        }

        return new CreateIndexStatement
        {
            Span = SourceSpan.From(createKeyword, end),
            CreateKeyword = createKeyword,
            UniqueKeyword = unique,
            ClusteredKeyword = clustered,
            IndexKeyword = indexKeyword,
            Name = name,
            OnKeyword = onKeyword,
            TableName = tableName,
            OpenParen = openParen,
            Columns = columns,
            CloseParen = closeParen,
            WhereKeyword = whereKeyword,
            Where = where,
        };
    }

    private AlterIndexStatement ParseAlterIndex()
    {
        var alterKeyword = Advance();
        var indexKeyword = ExpectIdent(Keyword.Index);
        var name = ParseQualifiedName();
        if (IdentifierEquals(Keyword.Rename))
        {
            var renameKeyword = Advance();
            var toKeyword = ExpectIdent(Keyword.To);
            var newName = ParseQualifiedName();
            return new AlterIndexStatement
            {
                Span = SourceSpan.From(alterKeyword, newName[^1]),
                AlterKeyword = alterKeyword,
                IndexKeyword = indexKeyword,
                Name = name,
                RenameKeyword = renameKeyword,
                ToKeyword = toKeyword,
                NewName = newName,
            };
        }

        if (_current.Kind != SyntaxKind.OnKeyword)
        {
            throw new SqlParseException($"Expected RENAME or ON, found {_current.Kind}", _current.Position);
        }

        var onKeyword = Advance();
        var tableName = ParseQualifiedName();
        if (!IdentifierEquals(Keyword.Rebuild) && !IdentifierEquals(Keyword.Reorganize) && !IdentifierEquals(Keyword.Disable))
        {
            throw new SqlParseException($"Expected REBUILD, REORGANIZE, or DISABLE, found {_current.Kind}", _current.Position);
        }

        var action = Advance();
        return new AlterIndexStatement
        {
            Span = SourceSpan.From(alterKeyword, action),
            AlterKeyword = alterKeyword,
            IndexKeyword = indexKeyword,
            Name = name,
            OnKeyword = onKeyword,
            TableName = tableName,
            Action = action,
        };
    }

    private DropIndexStatement ParseDropIndex()
    {
        var dropKeyword = Advance();
        var indexKeyword = ExpectIdent(Keyword.Index);
        var name = ParseQualifiedName();
        SyntaxToken? onKeyword = null;
        IReadOnlyList<SyntaxToken>? tableName = null;
        var end = name[^1].Span;
        if (_current.Kind == SyntaxKind.OnKeyword)
        {
            onKeyword = Advance();
            tableName = ParseQualifiedName();
            end = tableName[^1].Span;
        }

        SyntaxToken? behavior = null;
        if (IdentifierEquals(Keyword.Cascade) || IdentifierEquals(Keyword.Restrict))
        {
            var behaviorToken = Advance();
            behavior = behaviorToken;
            end = behaviorToken.Span;
        }

        return new DropIndexStatement
        {
            Span = SourceSpan.From(dropKeyword, end),
            DropKeyword = dropKeyword,
            IndexKeyword = indexKeyword,
            Name = name,
            OnKeyword = onKeyword,
            TableName = tableName,
            Behavior = behavior,
        };
    }

    private CommentStatement ParseComment()
    {
        var commentKeyword = Advance();
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        if (_current.Kind is SyntaxKind.EndOfFile or SyntaxKind.IsKeyword)
        {
            throw new SqlParseException($"Expected object kind, found {_current.Kind}", _current.Position);
        }

        var kind = Advance();
        var name = ParseQualifiedName();
        var isKeyword = Expect(SyntaxKind.IsKeyword);
        if (_current.Kind != SyntaxKind.String && _current.Kind != SyntaxKind.NullKeyword)
        {
            throw new SqlParseException($"Expected string or NULL, found {_current.Kind}", _current.Position);
        }

        var value = Advance();
        return new CommentStatement
        {
            Span = SourceSpan.From(commentKeyword, value),
            CommentKeyword = commentKeyword,
            OnKeyword = onKeyword,
            Kind = kind,
            Name = name,
            IsKeyword = isKeyword,
            Value = value,
        };
    }

    private IndexColumn ParseIndexColumn()
    {
        var expression = ParseExpression();
        SyntaxToken? direction = null;
        if (_current.Kind is SyntaxKind.AscKeyword or SyntaxKind.DescKeyword)
        {
            direction = Advance();
        }

        SyntaxToken? nullsKeyword = null;
        SyntaxToken? nullsOrder = null;
        if (IdentifierEquals(Keyword.Nulls))
        {
            nullsKeyword = Advance();
            if (!IdentifierEquals(Keyword.First) && !IdentifierEquals(Keyword.Last))
            {
                throw new SqlParseException($"Expected FIRST or LAST, found {_current.Kind}", _current.Position);
            }

            nullsOrder = Advance();
        }

        var end = nullsOrder?.Span ?? direction?.Span ?? expression.Span;
        return new IndexColumn
        {
            Span = SourceSpan.From(expression.Span, end),
            Expression = expression,
            Direction = direction,
            NullsKeyword = nullsKeyword,
            NullsOrder = nullsOrder,
        };
    }

    private bool IsSequenceOption() =>
        _current.Kind == SyntaxKind.CycleKeyword
        || IdentifierEquals(Keyword.Start)
        || IdentifierEquals(Keyword.Increment)
        || IdentifierEquals(Keyword.MaxValue)
        || IdentifierEquals(Keyword.MinValue)
        || (IdentifierEquals(Keyword.No)
            && (NextKind == SyntaxKind.CycleKeyword || NextEquals(Keyword.MaxValue) || NextEquals(Keyword.MinValue)));

    private bool IsRoutineDesignatorStart() =>
        IdentifierEquals(Keyword.Specific)
        || IdentifierEquals(Keyword.Function)
        || IdentifierEquals(Keyword.Procedure)
        || IdentifierEquals(Keyword.Routine)
        || IdentifierEquals(Keyword.Method)
        || IdentifierEquals(Keyword.Instance)
        || IdentifierEquals(Keyword.Static)
        || IdentifierEquals(Keyword.Constructor);
}
