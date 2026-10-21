using System.Runtime.CompilerServices;

namespace QlParse;

internal sealed partial class Parser
{
    internal InsertStatement ParseInsert()
    {
        var insertKeyword = Advance();
        var intoKeyword = ExpectIdent(Keyword.Into);
        var tableName = ParseQualifiedName();
        var alias = _current.Kind == SyntaxKind.AsKeyword && IsPostgres ? ParseInsertAlias() : default;
        var source = ParseInsertSource();
        var pg = IsPostgres ? ParsePgInsertTail(alias) : default;
        return new InsertStatement
        {
            Span = SourceSpan.From(insertKeyword.Span, pg.End ?? source.End),
            InsertKeyword = insertKeyword,
            IntoKeyword = intoKeyword,
            TableName = tableName,
            ColumnOpenParen = source.ColumnOpenParen,
            Columns = source.Columns,
            ColumnCloseParen = source.ColumnCloseParen,
            Override = source.Override,
            DefaultKeyword = source.DefaultKeyword,
            ValuesKeyword = source.ValuesKeyword,
            Query = source.Query,
            Postgres = pg.Extras,
        };
    }

    private readonly record struct InsertSource(
        SyntaxToken? ColumnOpenParen,
        IReadOnlyList<SyntaxToken>? Columns,
        SyntaxToken? ColumnCloseParen,
        OverrideClause? Override,
        SyntaxToken? DefaultKeyword,
        SyntaxToken? ValuesKeyword,
        Query? Query,
        SourceSpan End);

    private InsertSource ParseInsertSource()
    {
        if (IdentifierEquals(Keyword.Default))
        {
            var defaultKeyword = Advance();
            var valuesKeyword = Expect(SyntaxKind.ValuesKeyword);
            return new InsertSource(null, null, null, null, defaultKeyword, valuesKeyword, null, valuesKeyword.Span);
        }

        ParseOptionalColumnList(out var columnOpen, out var columns, out var columnClose);
        var overriding = ParseOverrideClause();
        var query = _current.Kind == SyntaxKind.Identifier && IsSqlServer ? ParseTsqlInsertSource() : ParseQuery();
        return new InsertSource(columnOpen, columns, columnClose, overriding, null, null, query, query.Span);
    }

    internal UpdateStatement ParseUpdate()
    {
        var updateKeyword = Advance();
        var target = ParseTargetTable();
        var portion = ParsePortion();
        var setKeyword = Expect(SyntaxKind.SetKeyword);
        var assignments = ParseSetClauses();
        var from = IsPostgres ? ParseFromClause() : EmptyFromClause;
        ParseDmlWhere(out var where, out var positioned);
        var returning = IsPostgres ? ParseOptionalReturning() : null;
        var end = returning?.Span ?? positioned?.Span ?? where?.Span ?? from.End ?? assignments[^1].Span;
        return new UpdateStatement
        {
            Span = SourceSpan.From(updateKeyword, end),
            UpdateKeyword = updateKeyword,
            Target = target.Table,
            Portion = portion,
            SetKeyword = setKeyword,
            Assignments = assignments,
            Where = where,
            Positioned = positioned,
            Postgres = IsPostgres ? BuildPgUpdateExtras(target.DescendantStar, from, returning) : null,
        };
    }

    private static readonly FromClause EmptyFromClause = new(null, null, [], [], null);

    internal DeleteStatement ParseDelete()
    {
        var deleteKeyword = Advance();
        var fromKeyword = Expect(SyntaxKind.FromKeyword);
        var target = ParseTargetTable();
        var portion = ParsePortion();
        var usingClause = _current.Kind == SyntaxKind.UsingKeyword && IsPostgres ? ParsePgUsingClause() : EmptyFromClause;
        ParseDmlWhere(out var where, out var positioned);
        var returning = IsPostgres ? ParseOptionalReturning() : null;
        var end = returning?.Span ?? positioned?.Span ?? where?.Span ?? usingClause.End ?? target.Table.Span;
        return new DeleteStatement
        {
            Span = SourceSpan.From(deleteKeyword, end),
            DeleteKeyword = deleteKeyword,
            FromKeyword = fromKeyword,
            Target = target.Table,
            Portion = portion,
            Where = where,
            Positioned = positioned,
            Postgres = IsPostgres ? BuildPgDeleteExtras(target.DescendantStar, usingClause, returning) : null,
        };
    }

    internal MergeStatement ParseMerge()
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

        var returning = IsPostgres ? ParseOptionalReturning() : null;
        return new MergeStatement
        {
            Span = SourceSpan.From(mergeKeyword.Span, returning?.Span ?? whens[^1].Span),
            MergeKeyword = mergeKeyword,
            IntoKeyword = intoKeyword,
            Target = target.Table,
            UsingKeyword = usingKeyword,
            Source = source,
            SourceJoins = joins,
            OnKeyword = onKeyword,
            Condition = condition,
            Whens = whens,
            Postgres = target.DescendantStar is null && returning is null
                ? null
                : new PgMergeExtras { TargetDescendantStar = target.DescendantStar, Returning = returning },
        };
    }

    internal TruncateStatement ParseTruncate()
    {
        var truncateKeyword = Advance();
        if (IsPostgres)
        {
            return ParsePgTruncate(truncateKeyword);
        }

        var tableKeyword = ExpectIdent(Keyword.Table);
        var tableName = ParseQualifiedName();
        var identity = ParseTruncateIdentity();
        var end = identity.IdentityKeyword ?? tableName[^1];
        return new TruncateStatement
        {
            Span = SourceSpan.From(truncateKeyword, end),
            TruncateKeyword = truncateKeyword,
            TableKeyword = tableKeyword,
            TableName = tableName,
            RestartKeyword = identity.RestartKeyword,
            IdentityKeyword = identity.IdentityKeyword,
        };
    }

    internal readonly record struct TruncateIdentity(SyntaxToken? RestartKeyword, SyntaxToken? IdentityKeyword);

    private TruncateIdentity ParseTruncateIdentity()
    {
        if (!IdentifierEquals(Keyword.Continue) && !IdentifierEquals(Keyword.Restart))
        {
            return default;
        }

        var restart = Advance();
        if (!IdentifierEquals(Keyword.Identity))
        {
            throw new SqlParseException($"Expected IDENTITY, found {_current.Kind}", _current.Position);
        }

        return new TruncateIdentity(restart, Advance());
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
        var name = new List<SyntaxToken> { _current.Kind == SyntaxKind.Identifier ? Advance() : ExpectTsqlTableVariable() };
        while (_current.Kind == SyntaxKind.Dot)
        {
            Advance();
            name.Add(Expect(SyntaxKind.Identifier));
        }

        return name;
    }

    internal readonly record struct ParsedTargetTable(TargetTable Table, SyntaxToken? DescendantStar);

    private ParsedTargetTable ParseTargetTable()
    {
        var targetName = _current.Kind == SyntaxKind.OnlyKeyword ? ParseOnlyTargetName() : new TargetName(null, null, ParseQualifiedName(), null);
        var only = targetName.OnlyKeyword;
        var open = targetName.OpenParen;
        var name = targetName.NameParts;
        var close = targetName.CloseParen;
        var start = only ?? name[0];

        var star = _current.Kind == SyntaxKind.Star && IsPostgres ? Advance() : (SyntaxToken?)null;
        SyntaxToken? asKeyword = null;
        SyntaxToken? alias = null;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            alias = Expect(SyntaxKind.Identifier);
        }
        else
        {
            if (IsBareTargetAlias())
            {
                alias = Advance();
            }
        }

        var end = alias ?? star ?? close ?? name[^1];
        var table = new TargetTable
        {
            Span = SourceSpan.From(start, end),
            OnlyKeyword = only,
            OpenParen = open,
            NameParts = name,
            CloseParen = close,
            AsKeyword = asKeyword,
            Alias = alias,
        };
        return new ParsedTargetTable(table, star);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsBareTargetAlias() => _current.Kind == SyntaxKind.Identifier && !IsPgReservedClauseWord();

    private readonly record struct TargetName(
        SyntaxToken? OnlyKeyword,
        SyntaxToken? OpenParen,
        IReadOnlyList<SyntaxToken> NameParts,
        SyntaxToken? CloseParen);

    // ONLY (name), or (Postgres) ONLY name.
    private TargetName ParseOnlyTargetName()
    {
        var onlyKeyword = Advance();
        if (_current.Kind != SyntaxKind.OpenParen && IsPostgres)
        {
            return new TargetName(onlyKeyword, null, ParseQualifiedName(), null);
        }

        var open = Expect(SyntaxKind.OpenParen);
        var name = ParseQualifiedName();
        var close = Expect(SyntaxKind.CloseParen);
        return new TargetName(onlyKeyword, open, name, close);
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
        else
        {
            if (IdentifierEquals(Keyword.System))
            {
                kind = Advance();
            }
            else
            {
                throw new SqlParseException($"Expected USER or SYSTEM, found {_current.Kind}", _current.Position);
            }
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
        else
        {
            if (_current.Kind == SyntaxKind.WhereKeyword)
            {
                where = ParseWhereClause();
            }
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
        var byClause = ParseMergeByClause(notKeyword);
        var byKeyword = byClause.ByKeyword;
        var byKind = byClause.ByKind;
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

    private readonly record struct MergeByClause(SyntaxToken? ByKeyword, SyntaxToken? ByKind);

    private MergeByClause ParseMergeByClause(SyntaxToken? notKeyword)
    {
        if (notKeyword is null || _current.Kind != SyntaxKind.ByKeyword)
        {
            return new MergeByClause(null, null);
        }

        var byKeyword = Advance();
        if (!IdentifierEquals(Keyword.Source) && !IdentifierEquals(Keyword.Target))
        {
            throw new SqlParseException($"Expected SOURCE or TARGET, found {_current.Kind}", _current.Position);
        }

        return new MergeByClause(byKeyword, Advance());
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

        return TryParsePgMergeDoNothing()
            ?? throw new SqlParseException($"Expected UPDATE or DELETE, found {_current.Kind}", _current.Position);
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

    private MergeAction ParseMergeInsert()
    {
        if (!IdentifierEquals(Keyword.Insert))
        {
            return TryParsePgMergeDoNothing()
                ?? throw new SqlParseException($"Expected INSERT, found {_current.Kind}", _current.Position);
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
