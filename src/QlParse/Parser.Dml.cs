namespace QlParse;

internal sealed partial class Parser
{
    private InsertStatement ParseInsert()
    {
        var insertKeyword = Advance();
        var intoKeyword = ExpectIdent(Keyword.Into);
        var tableName = ParseQualifiedName();
        if (IdentifierEquals(Keyword.Default))
        {
            var defaultKeyword = Advance();
            var valuesKeyword = Expect(SyntaxKind.ValuesKeyword);
            return new InsertStatement
            {
                Span = SourceSpan.From(insertKeyword, valuesKeyword),
                InsertKeyword = insertKeyword,
                IntoKeyword = intoKeyword,
                TableName = tableName,
                DefaultKeyword = defaultKeyword,
                ValuesKeyword = valuesKeyword,
            };
        }

        ParseOptionalColumnList(out var columnOpen, out var columns, out var columnClose);
        var overriding = ParseOverrideClause();
        var query = ParseQuery();
        return new InsertStatement
        {
            Span = SourceSpan.From(insertKeyword, query.Span),
            InsertKeyword = insertKeyword,
            IntoKeyword = intoKeyword,
            TableName = tableName,
            ColumnOpenParen = columnOpen,
            Columns = columns,
            ColumnCloseParen = columnClose,
            Override = overriding,
            Query = query,
        };
    }

    private UpdateStatement ParseUpdate()
    {
        var updateKeyword = Advance();
        var target = ParseTargetTable();
        var portion = ParsePortion();
        var setKeyword = Expect(SyntaxKind.SetKeyword);
        var assignments = ParseSetClauses();
        ParseDmlWhere(out var where, out var positioned);
        var end = positioned?.Span ?? where?.Span ?? assignments[^1].Span;
        return new UpdateStatement
        {
            Span = SourceSpan.From(updateKeyword, end),
            UpdateKeyword = updateKeyword,
            Target = target,
            Portion = portion,
            SetKeyword = setKeyword,
            Assignments = assignments,
            Where = where,
            Positioned = positioned,
        };
    }

    private DeleteStatement ParseDelete()
    {
        var deleteKeyword = Advance();
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var target = ParseTargetTable();
        var portion = ParsePortion();
        ParseDmlWhere(out var where, out var positioned);
        var end = positioned?.Span ?? where?.Span ?? target.Span;
        return new DeleteStatement
        {
            Span = SourceSpan.From(deleteKeyword, end),
            DeleteKeyword = deleteKeyword,
            FromKeyword = fromKeyword,
            Target = target,
            Portion = portion,
            Where = where,
            Positioned = positioned,
        };
    }

    private MergeStatement ParseMerge()
    {
        var mergeKeyword = Advance();
        var intoKeyword = ExpectIdent(Keyword.Into);
        var target = ParseTargetTable();
        var usingKeyword = Expect(SyntaxKind.UsingKeyword);
        var source = ParseTableSource();
        var joins = ParseJoins();
        var onKeyword = Expect(SyntaxKind.OnKeyword);
        var condition = ParseExpression();
        if (_current.Kind != SyntaxKind.WhenKeyword)
        {
            throw new SqlParseException($"Expected {SyntaxKind.WhenKeyword}, found {_current.Kind}", _current.Position);
        }

        var whens = new List<MergeWhenClause>();
        while (_current.Kind == SyntaxKind.WhenKeyword)
        {
            whens.Add(ParseMergeWhen());
        }

        return new MergeStatement
        {
            Span = SourceSpan.From(mergeKeyword, whens[^1].Span),
            MergeKeyword = mergeKeyword,
            IntoKeyword = intoKeyword,
            Target = target,
            UsingKeyword = usingKeyword,
            Source = source,
            SourceJoins = joins,
            OnKeyword = onKeyword,
            Condition = condition,
            Whens = whens,
        };
    }

    private TruncateStatement ParseTruncate()
    {
        var truncateKeyword = Advance();
        var tableKeyword = ExpectIdent(Keyword.Table);
        var tableName = ParseQualifiedName();
        SyntaxToken? restart = null;
        SyntaxToken? identity = null;
        if (IdentifierEquals(Keyword.Continue) || IdentifierEquals(Keyword.Restart))
        {
            restart = Advance();
            if (!IdentifierEquals(Keyword.Identity))
            {
                throw new SqlParseException($"Expected IDENTITY, found {_current.Kind}", _current.Position);
            }

            identity = Advance();
        }

        var end = identity ?? tableName[^1];
        return new TruncateStatement
        {
            Span = SourceSpan.From(truncateKeyword, end),
            TruncateKeyword = truncateKeyword,
            TableKeyword = tableKeyword,
            TableName = tableName,
            RestartKeyword = restart,
            IdentityKeyword = identity,
        };
    }

    private PortionClause? ParsePortion()
    {
        if (_current.Kind != SyntaxKind.ForKeyword || !NextEquals(Keyword.Portion))
        {
            return null;
        }

        var forKeyword = Advance();
        var portionKeyword = Advance();
        var ofKeyword = Expect(SyntaxKind.OfKeyword);
        var name = Expect(SyntaxKind.Identifier);
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var start = ParseExpression(ComparisonBindingPower + 1);
        if (!IdentifierEquals(Keyword.To))
        {
            throw new SqlParseException($"Expected TO, found {_current.Kind}", _current.Position);
        }

        var toKeyword = Advance();
        var end = ParseExpression(ComparisonBindingPower + 1);
        return new PortionClause
        {
            Span = SourceSpan.From(forKeyword, end.Span),
            ForKeyword = forKeyword,
            PortionKeyword = portionKeyword,
            OfKeyword = ofKeyword,
            Name = name,
            FromKeyword = fromKeyword,
            Start = start,
            ToKeyword = toKeyword,
            End = end,
        };
    }

    private List<SyntaxToken> ParseQualifiedName()
    {
        var name = new List<SyntaxToken> { Expect(SyntaxKind.Identifier) };
        while (_current.Kind == SyntaxKind.Dot)
        {
            Advance();
            name.Add(Expect(SyntaxKind.Identifier));
        }

        return name;
    }

    private TargetTable ParseTargetTable()
    {
        SyntaxToken? only = null;
        SyntaxToken? open = null;
        SyntaxToken? close = null;
        IReadOnlyList<SyntaxToken> name;
        SyntaxToken start;
        if (_current.Kind == SyntaxKind.OnlyKeyword)
        {
            var onlyKeyword = Advance();
            only = onlyKeyword;
            open = Expect(SyntaxKind.OpenParen);
            name = ParseQualifiedName();
            close = Expect(SyntaxKind.CloseParen);
            start = onlyKeyword;
        }
        else
        {
            name = ParseQualifiedName();
            start = name[0];
        }

        SyntaxToken? asKeyword = null;
        SyntaxToken? alias = null;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            alias = Expect(SyntaxKind.Identifier);
        }
        else if (_current.Kind == SyntaxKind.Identifier)
        {
            alias = Advance();
        }

        var end = alias ?? close ?? name[^1];
        return new TargetTable
        {
            Span = SourceSpan.From(start, end),
            OnlyKeyword = only,
            OpenParen = open,
            NameParts = name,
            CloseParen = close,
            AsKeyword = asKeyword,
            Alias = alias,
        };
    }

    private OverrideClause? ParseOverrideClause()
    {
        if (!IdentifierEquals(Keyword.Overriding))
        {
            return null;
        }

        var overriding = Advance();
        SyntaxToken kind;
        if (_current.Kind == SyntaxKind.UserKeyword)
        {
            kind = Advance();
        }
        else if (IdentifierEquals(Keyword.System))
        {
            kind = Advance();
        }
        else
        {
            throw new SqlParseException($"Expected USER or SYSTEM, found {_current.Kind}", _current.Position);
        }

        if (!IdentifierEquals(Keyword.Value))
        {
            throw new SqlParseException($"Expected VALUE, found {_current.Kind}", _current.Position);
        }

        var valueKeyword = Advance();
        return new OverrideClause
        {
            Span = SourceSpan.From(overriding, valueKeyword),
            OverridingKeyword = overriding,
            Kind = kind,
            ValueKeyword = valueKeyword,
        };
    }

    private void ParseDmlWhere(out WhereClause? where, out PositionedClause? positioned)
    {
        where = null;
        positioned = null;
        if (IsPositionedWhere())
        {
            positioned = ParsePositionedWhere();
        }
        else if (_current.Kind == SyntaxKind.WhereKeyword)
        {
            where = ParseWhereClause();
        }
    }

    private bool IsPositionedWhere() =>
        _current.Kind == SyntaxKind.WhereKeyword
        && _index < _tokens.Count
        && TokenEquals(_tokens[_index], Keyword.Current)
        && _index + 1 < _tokens.Count
        && _tokens[_index + 1].Kind == SyntaxKind.OfKeyword;

    private PositionedClause ParsePositionedWhere()
    {
        var whereKeyword = Expect(SyntaxKind.WhereKeyword);
        if (!IdentifierEquals(Keyword.Current))
        {
            throw new SqlParseException($"Expected CURRENT, found {_current.Kind}", _current.Position);
        }

        var currentKeyword = Advance();
        var ofKeyword = Expect(SyntaxKind.OfKeyword);
        var cursor = Expect(SyntaxKind.Identifier);
        return new PositionedClause
        {
            Span = SourceSpan.From(whereKeyword, cursor),
            WhereKeyword = whereKeyword,
            CurrentKeyword = currentKeyword,
            OfKeyword = ofKeyword,
            Cursor = cursor,
        };
    }

    private List<SetClause> ParseSetClauses()
    {
        var clauses = new List<SetClause> { ParseSetClause() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            clauses.Add(ParseSetClause());
        }

        return clauses;
    }

    private SetClause ParseSetClause()
    {
        SyntaxToken? open = null;
        IReadOnlyList<SetTarget> targets;
        SyntaxToken? close = null;
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            open = Advance();
            targets = ParseSetTargets();
            close = Expect(SyntaxKind.CloseParen);
        }
        else
        {
            targets = [ParseSetTarget()];
        }

        var equals = Expect(SyntaxKind.EqualsToken);
        var start = open is SyntaxToken openParen ? openParen.Span : targets[0].Span;
        if (open is null && IdentifierEquals(Keyword.Default) && NextKind != SyntaxKind.OpenParen)
        {
            var defaultKeyword = Advance();
            return new SetClause
            {
                Span = SourceSpan.From(start, defaultKeyword),
                Targets = targets,
                EqualsToken = equals,
                DefaultKeyword = defaultKeyword,
            };
        }

        var value = ParseExpression();
        return new SetClause
        {
            Span = SourceSpan.From(start, value.Span),
            OpenParen = open,
            Targets = targets,
            CloseParen = close,
            EqualsToken = equals,
            Value = value,
        };
    }

    private List<SetTarget> ParseSetTargets()
    {
        var targets = new List<SetTarget> { ParseSetTarget() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            targets.Add(ParseSetTarget());
        }

        return targets;
    }

    private SetTarget ParseSetTarget()
    {
        var name = Expect(SyntaxKind.Identifier);
        if (_current.Kind != SyntaxKind.OpenBracket)
        {
            return new SetTarget { Span = name.Span, Name = name };
        }

        var open = Advance();
        var index = ParseExpression();
        var close = Expect(SyntaxKind.CloseBracket);
        return new SetTarget
        {
            Span = SourceSpan.From(name, close),
            Name = name,
            OpenBracket = open,
            Index = index,
            CloseBracket = close,
        };
    }

    private MergeWhenClause ParseMergeWhen()
    {
        var whenKeyword = Expect(SyntaxKind.WhenKeyword);
        var notKeyword = _current.Kind == SyntaxKind.NotKeyword ? Advance() : (SyntaxToken?)null;
        if (!IdentifierEquals(Keyword.Matched))
        {
            throw new SqlParseException($"Expected MATCHED, found {_current.Kind}", _current.Position);
        }

        var matchedKeyword = Advance();
        SyntaxToken? byKeyword = null;
        SyntaxToken? byKind = null;
        if (notKeyword is not null && _current.Kind == SyntaxKind.ByKeyword)
        {
            byKeyword = Advance();
            if (!IdentifierEquals(Keyword.Source) && !IdentifierEquals(Keyword.Target))
            {
                throw new SqlParseException($"Expected SOURCE or TARGET, found {_current.Kind}", _current.Position);
            }

            byKind = Advance();
        }

        SyntaxToken? andKeyword = null;
        Expression? condition = null;
        if (_current.Kind == SyntaxKind.AndKeyword)
        {
            andKeyword = Advance();
            condition = ParseExpression();
        }

        var thenKeyword = Expect(SyntaxKind.ThenKeyword);
        var bySource = byKind is SyntaxToken kind && TokenEquals(kind, Keyword.Source);
        var action = notKeyword is not null && !bySource
            ? ParseMergeInsert()
            : ParseMergeUpdateOrDelete();
        return new MergeWhenClause
        {
            Span = SourceSpan.From(whenKeyword, action.Span),
            WhenKeyword = whenKeyword,
            NotKeyword = notKeyword,
            MatchedKeyword = matchedKeyword,
            ByKeyword = byKeyword,
            ByKind = byKind,
            AndKeyword = andKeyword,
            Condition = condition,
            ThenKeyword = thenKeyword,
            Action = action,
        };
    }

    private MergeAction ParseMergeUpdateOrDelete()
    {
        if (_current.Kind == SyntaxKind.UpdateKeyword)
        {
            return ParseMergeUpdate();
        }

        if (IdentifierEquals(Keyword.Delete))
        {
            var deleteKeyword = Advance();
            return new MergeDeleteAction
            {
                Span = deleteKeyword.Span,
                DeleteKeyword = deleteKeyword,
            };
        }

        throw new SqlParseException($"Expected UPDATE or DELETE, found {_current.Kind}", _current.Position);
    }

    private MergeUpdateAction ParseMergeUpdate()
    {
        var updateKeyword = Expect(SyntaxKind.UpdateKeyword);
        var setKeyword = Expect(SyntaxKind.SetKeyword);
        var assignments = ParseSetClauses();
        return new MergeUpdateAction
        {
            Span = SourceSpan.From(updateKeyword, assignments[^1].Span),
            UpdateKeyword = updateKeyword,
            SetKeyword = setKeyword,
            Assignments = assignments,
        };
    }

    private MergeInsertAction ParseMergeInsert()
    {
        if (!IdentifierEquals(Keyword.Insert))
        {
            throw new SqlParseException($"Expected INSERT, found {_current.Kind}", _current.Position);
        }

        var insertKeyword = Advance();
        ParseOptionalColumnList(out var columnOpen, out var columns, out var columnClose);
        var overriding = ParseOverrideClause();
        var valuesKeyword = Expect(SyntaxKind.ValuesKeyword);
        var open = Expect(SyntaxKind.OpenParen);
        var values = ParseExpressionList();
        var close = Expect(SyntaxKind.CloseParen);
        return new MergeInsertAction
        {
            Span = SourceSpan.From(insertKeyword, close),
            InsertKeyword = insertKeyword,
            ColumnOpenParen = columnOpen,
            Columns = columns,
            ColumnCloseParen = columnClose,
            Override = overriding,
            ValuesKeyword = valuesKeyword,
            OpenParen = open,
            Values = values,
            CloseParen = close,
        };
    }
}
