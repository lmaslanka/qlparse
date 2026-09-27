namespace QlParse;

internal sealed partial class Parser
{
    internal TableConstraint ParseTableConstraint() => ParseConstraint(column: false);

    internal TableConstraint ParseColumnConstraint() => ParseConstraint(column: true);

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

        if (!column && IdentifierEquals(Keyword.Foreign))
        {
            return ParseForeignKeyConstraint(start, constraintKeyword, constraintName);
        }

        if (IdentifierEquals(Keyword.Primary) || _current.Kind == SyntaxKind.UniqueKeyword)
        {
            return ParsePrimaryOrUniqueConstraint(column, start, constraintKeyword, constraintName);
        }

        if (IdentifierEquals(Keyword.Check))
        {
            return ParseCheckConstraint(start, constraintKeyword, constraintName);
        }

        if (IdentifierEquals(Keyword.References))
        {
            return ParseColumnReferencesConstraint(start, constraintKeyword, constraintName);
        }

        if (column && _current.Kind == SyntaxKind.NotKeyword)
        {
            return ParseColumnNotNullConstraint(start, constraintKeyword, constraintName);
        }

        throw new SqlParseException($"Expected constraint, found {_current.Kind}", _current.Position);
    }

    private TableConstraint ParseForeignKeyConstraint(SyntaxToken start, SyntaxToken? constraintKeyword, SyntaxToken? constraintName)
    {
        var foreignKeyword = Advance();
        var keyKeyword = ExpectIdent(Keyword.Key);
        ParseRequiredNameList(out var columnsOpen, out var columns, out var columnsClose);
        ParseReferences(out var referencesKeyword, out var referenceTable, out var referenceOpen, out var referenceColumns, out var referenceClose);
        ParseMatchAndActions(out var matchKeyword, out var matchType, out var actions);
        ParseConstraintCharacteristics(out var deferrableNot, out var deferrable, out var initially, out var initiallyWhen);
        var end = initiallyWhen?.Span
            ?? deferrable?.Span
            ?? (actions.Count > 0 ? actions[^1].Span : (SourceSpan?)null)
            ?? referenceClose?.Span
            ?? referenceTable[^1].Span;
        return new TableConstraint
        {
            Span = SourceSpan.From(start, end),
            ConstraintKeyword = constraintKeyword,
            ConstraintName = constraintName,
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
            MatchKeyword = matchKeyword,
            MatchType = matchType,
            Actions = actions,
            DeferrableNotKeyword = deferrableNot,
            DeferrableKeyword = deferrable,
            InitiallyKeyword = initially,
            InitiallyWhen = initiallyWhen,
        };
    }

    private TableConstraint ParsePrimaryOrUniqueConstraint(bool column, SyntaxToken start, SyntaxToken? constraintKeyword, SyntaxToken? constraintName)
    {
        SyntaxToken? primaryKeyword = null;
        SyntaxToken? keyKeyword = null;
        SyntaxToken? uniqueKeyword = null;
        SyntaxToken? nullsKeyword = null;
        SyntaxToken? nullsNotKeyword = null;
        SyntaxToken? distinctKeyword = null;
        if (IdentifierEquals(Keyword.Primary))
        {
            primaryKeyword = Advance();
            keyKeyword = ExpectIdent(Keyword.Key);
        }
        else
        {
            uniqueKeyword = Advance();
            ParseNullsDistinct(out var nullsToken, out var nullsNotToken, out var distinctToken);
            nullsKeyword = nullsToken;
            nullsNotKeyword = nullsNotToken;
            distinctKeyword = distinctToken;
        }

        SyntaxToken? columnsOpen = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        SyntaxToken? columnsClose = null;
        if (!column)
        {
            ParseRequiredNameList(out var keyOpen, out var keyColumns, out var keyClose);
            columnsOpen = keyOpen;
            columns = keyColumns;
            columnsClose = keyClose;
        }

        ParseConstraintCharacteristics(out var deferrableNot, out var deferrable, out var initially, out var initiallyWhen);
        var end = initiallyWhen?.Span
            ?? deferrable?.Span
            ?? columnsClose?.Span
            ?? keyKeyword?.Span
            ?? uniqueKeyword?.Span
            ?? constraintName?.Span
            ?? start.Span;
        return new TableConstraint
        {
            Span = SourceSpan.From(start, end),
            ConstraintKeyword = constraintKeyword,
            ConstraintName = constraintName,
            PrimaryKeyword = primaryKeyword,
            KeyKeyword = keyKeyword,
            UniqueKeyword = uniqueKeyword,
            NullsKeyword = nullsKeyword,
            NullsNotKeyword = nullsNotKeyword,
            DistinctKeyword = distinctKeyword,
            ColumnsOpen = columnsOpen,
            Columns = columns,
            ColumnsClose = columnsClose,
            DeferrableNotKeyword = deferrableNot,
            DeferrableKeyword = deferrable,
            InitiallyKeyword = initially,
            InitiallyWhen = initiallyWhen,
        };
    }

    private TableConstraint ParseCheckConstraint(SyntaxToken start, SyntaxToken? constraintKeyword, SyntaxToken? constraintName)
    {
        var checkKeyword = Advance();
        var checkOpen = Expect(SyntaxKind.OpenParen);
        var check = ParseExpression();
        var checkClose = Expect(SyntaxKind.CloseParen);
        ParseConstraintCharacteristics(out var deferrableNot, out var deferrable, out var initially, out var initiallyWhen);
        var end = initiallyWhen?.Span ?? deferrable?.Span ?? checkClose.Span;
        return new TableConstraint
        {
            Span = SourceSpan.From(start, end),
            ConstraintKeyword = constraintKeyword,
            ConstraintName = constraintName,
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

    private TableConstraint ParseColumnReferencesConstraint(SyntaxToken start, SyntaxToken? constraintKeyword, SyntaxToken? constraintName)
    {
        ParseReferences(out var referencesKeyword, out var referenceTable, out var referenceOpen, out var referenceColumns, out var referenceClose);
        ParseMatchAndActions(out var matchKeyword, out var matchType, out var actions);
        ParseConstraintCharacteristics(out var deferrableNot, out var deferrable, out var initially, out var initiallyWhen);
        var end = initiallyWhen?.Span
            ?? deferrable?.Span
            ?? (actions.Count > 0 ? actions[^1].Span : (SourceSpan?)null)
            ?? referenceClose?.Span
            ?? referenceTable[^1].Span;
        return new TableConstraint
        {
            Span = SourceSpan.From(start, end),
            ConstraintKeyword = constraintKeyword,
            ConstraintName = constraintName,
            ReferencesKeyword = referencesKeyword,
            ReferenceTable = referenceTable,
            ReferenceOpen = referenceOpen,
            ReferenceColumns = referenceColumns,
            ReferenceClose = referenceClose,
            MatchKeyword = matchKeyword,
            MatchType = matchType,
            Actions = actions,
            DeferrableNotKeyword = deferrableNot,
            DeferrableKeyword = deferrable,
            InitiallyKeyword = initially,
            InitiallyWhen = initiallyWhen,
        };
    }

    private TableConstraint ParseColumnNotNullConstraint(SyntaxToken start, SyntaxToken? constraintKeyword, SyntaxToken? constraintName)
    {
        var notKeyword = Advance();
        var nullKeyword = Expect(SyntaxKind.NullKeyword);
        ParseConstraintCharacteristics(out var deferrableNot, out var deferrable, out var initially, out var initiallyWhen);
        var end = initiallyWhen?.Span ?? deferrable?.Span ?? nullKeyword.Span;
        return new TableConstraint
        {
            Span = SourceSpan.From(start, end),
            ConstraintKeyword = constraintKeyword,
            ConstraintName = constraintName,
            NotKeyword = notKeyword,
            NullKeyword = nullKeyword,
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
            var (noKeyword, setKeyword, action) = ParseReferentialActionValue();
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

    private (SyntaxToken? NoKeyword, SyntaxToken? SetKeyword, SyntaxToken Action) ParseReferentialActionValue()
    {
        if (_current.Kind == SyntaxKind.SetKeyword)
        {
            var setKeyword = Advance();
            if (_current.Kind != SyntaxKind.NullKeyword && !IdentifierEquals(Keyword.Default))
            {
                throw new SqlParseException($"Expected NULL or DEFAULT, found {_current.Kind}", _current.Position);
            }

            return (null, setKeyword, Advance());
        }

        if (IdentifierEquals(Keyword.No))
        {
            var noKeyword = Advance();
            return (noKeyword, null, ExpectIdent(Keyword.Action));
        }

        if (IdentifierEquals(Keyword.Cascade) || IdentifierEquals(Keyword.Restrict))
        {
            return (null, null, Advance());
        }

        throw new SqlParseException($"Expected referential action, found {_current.Kind}", _current.Position);
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

    internal void ParseGenerated(out IdentityColumn? identity, out GeneratedColumn? generated)
    {
        var generatedKeyword = Advance();
        ParseGenerationRule(out var alwaysKeyword, out var byKeyword, out var defaultKeyword);
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        if (IdentifierEquals(Keyword.Identity))
        {
            identity = ParseIdentityColumn(generatedKeyword, alwaysKeyword, byKeyword, defaultKeyword, asKeyword);
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
            else
            {
                if (_current.Kind == SyntaxKind.EndKeyword)
                {
                    bound = Advance();
                }
                else
                {
                    throw new SqlParseException($"Expected START or END, found {_current.Kind}", _current.Position);
                }
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

    private IdentityColumn ParseIdentityColumn(
        SyntaxToken generatedKeyword,
        SyntaxToken? alwaysKeyword,
        SyntaxToken? byKeyword,
        SyntaxToken? defaultKeyword,
        SyntaxToken asKeyword)
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

        return new IdentityColumn
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

    internal bool IsBasicSequenceOption() =>
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

    internal DefaultClause ParseDefaultClause()
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

    internal void ParseWithData(out SyntaxToken withKeyword, out SyntaxToken? noKeyword, out SyntaxToken dataKeyword)
    {
        withKeyword = Expect(SyntaxKind.WithKeyword);
        noKeyword = IdentifierEquals(Keyword.No) ? Advance() : null;
        dataKeyword = ExpectIdent(Keyword.Data);
    }

    internal void ParseOnCommit(
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

    internal List<IReadOnlyList<SyntaxToken>> ParseSchemaNameList()
    {
        var names = new List<IReadOnlyList<SyntaxToken>> { ParseQualifiedName() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            names.Add(ParseQualifiedName());
        }

        return names;
    }

    internal SyntaxToken ParseDropBehavior()
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

    internal bool IsAsColumnList()
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

    internal bool IsColumnConstraintStart() =>
        IsTableConstraintStart()
        || _current.Kind == SyntaxKind.NotKeyword
        || IdentifierEquals(Keyword.References);

    internal bool IsPeriodDefinition() =>
        IdentifierEquals(Keyword.Period) && NextKind == SyntaxKind.ForKeyword;

    internal bool IsSystemVersioning() =>
        IdentifierEquals(Keyword.System) && NextEquals(Keyword.Versioning);

    internal void ParseSystemVersioning(
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

    internal PeriodDefinition ParsePeriodDefinition()
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

    internal bool IsRefIs() =>
        IdentifierEquals(Keyword.Ref) && NextKind == SyntaxKind.IsKeyword;

    internal bool IsWithOptions() =>
        _current.Kind == SyntaxKind.Identifier
        && NextKind == SyntaxKind.WithKeyword
        && _index + 1 < _tokens.Count
        && TokenEquals(_tokens[_index + 1], Keyword.Options);

    private bool IsDeferrableStart() =>
        IdentifierEquals(Keyword.Deferrable)
        || (_current.Kind == SyntaxKind.NotKeyword && NextEquals(Keyword.Deferrable));
}
