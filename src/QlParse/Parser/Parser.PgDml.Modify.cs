namespace QlParse;

internal sealed partial class Parser
{
    internal readonly record struct InsertAlias(SyntaxToken? AsKeyword, SyntaxToken? Alias);

    // INSERT INTO table AS alias (Postgres requires the AS).
    internal InsertAlias ParseInsertAlias()
    {
        var asKeyword = Advance();
        var alias = Expect(SyntaxKind.Identifier);
        return new InsertAlias(asKeyword, alias);
    }

    internal readonly record struct PgInsertTail(OnConflictClause? OnConflict, ReturningClause? Returning, SourceSpan? End);

    internal PgInsertTail ParsePgInsertTail()
    {
        var onConflict = _current.Kind == SyntaxKind.OnKeyword && NextEquals(Keyword.Conflict) ? ParseOnConflict() : null;
        var returning = ParseOptionalReturning();
        return new PgInsertTail(onConflict, returning, returning?.Span ?? onConflict?.Span);
    }

    // ON CONFLICT [conflict_target] DO NOTHING | DO UPDATE SET ... [WHERE condition]
    private OnConflictClause ParseOnConflict()
    {
        var onKeyword = Advance();
        var conflictKeyword = Advance();
        var target = ParseConflictTarget();
        var doKeyword = ExpectIdent(Keyword.Do);
        if (IdentifierEquals(Keyword.Nothing))
        {
            var nothingKeyword = Advance();
            return new OnConflictClause
            {
                Span = SourceSpan.From(onKeyword, nothingKeyword),
                OnKeyword = onKeyword,
                ConflictKeyword = conflictKeyword,
                Target = target,
                DoKeyword = doKeyword,
                NothingKeyword = nothingKeyword,
            };
        }

        var updateKeyword = Expect(SyntaxKind.UpdateKeyword);
        var setKeyword = Expect(SyntaxKind.SetKeyword);
        var assignments = ParseSetClauses();
        var where = _current.Kind == SyntaxKind.WhereKeyword ? ParseWhereClause() : null;
        return new OnConflictClause
        {
            Span = SourceSpan.From(onKeyword, where?.Span ?? assignments[^1].Span),
            OnKeyword = onKeyword,
            ConflictKeyword = conflictKeyword,
            Target = target,
            DoKeyword = doKeyword,
            UpdateKeyword = updateKeyword,
            SetKeyword = setKeyword,
            Assignments = assignments,
            Where = where,
        };
    }

    private ConflictTarget? ParseConflictTarget()
    {
        if (_current.Kind == SyntaxKind.OpenParen)
        {
            return ParseConflictIndexTarget();
        }

        if (_current.Kind != SyntaxKind.OnKeyword)
        {
            return null;
        }

        var onKeyword = Advance();
        var constraintKeyword = ExpectIdent(Keyword.Constraint);
        var constraintName = Expect(SyntaxKind.Identifier);
        return new ConflictTarget
        {
            Span = SourceSpan.From(onKeyword, constraintName),
            OnKeyword = onKeyword,
            ConstraintKeyword = constraintKeyword,
            ConstraintName = constraintName,
        };
    }

    // ( {column | (expression) | function(...)} [COLLATE collation] [opclass] [, ...] ) [WHERE predicate]
    private ConflictTarget ParseConflictIndexTarget()
    {
        var openParen = Advance();
        var elements = new List<ConflictTargetElement> { ParseConflictTargetElement() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            elements.Add(ParseConflictTargetElement());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        var where = _current.Kind == SyntaxKind.WhereKeyword ? ParseWhereClause() : null;
        return new ConflictTarget
        {
            Span = SourceSpan.From(openParen, where?.Span ?? closeParen.Span),
            OpenParen = openParen,
            Elements = elements,
            CloseParen = closeParen,
            Where = where,
        };
    }

    private ConflictTargetElement ParseConflictTargetElement()
    {
        var expression = ParseExpression();
        if (_current.Kind != SyntaxKind.Identifier)
        {
            return new ConflictTargetElement { Span = expression.Span, Expression = expression };
        }

        var operatorClass = ParseQualifiedName();
        return new ConflictTargetElement
        {
            Span = SourceSpan.From(expression.Span, operatorClass[^1]),
            Expression = expression,
            OperatorClass = operatorClass,
        };
    }

    internal ReturningClause? ParseOptionalReturning() =>
        IdentifierEquals(Keyword.Returning) ? ParseReturning() : null;

    // RETURNING [WITH ({OLD | NEW} AS alias [, ...])] {* | expression [[AS] name]} [, ...]
    private ReturningClause ParseReturning()
    {
        var returningKeyword = Advance();
        var outputs = _current.Kind == SyntaxKind.WithKeyword && NextKind == SyntaxKind.OpenParen
            ? ParseReturningOutputs()
            : default;
        var items = ParseSelectList();
        return new ReturningClause
        {
            Span = SourceSpan.From(returningKeyword, items[^1].Span),
            ReturningKeyword = returningKeyword,
            WithKeyword = outputs.WithKeyword,
            OpenParen = outputs.OpenParen,
            OutputAliases = outputs.Aliases ?? [],
            CloseParen = outputs.CloseParen,
            Items = items,
        };
    }

    private readonly record struct ReturningOutputs(
        SyntaxToken? WithKeyword,
        SyntaxToken? OpenParen,
        IReadOnlyList<ReturningOutputAlias>? Aliases,
        SyntaxToken? CloseParen);

    private ReturningOutputs ParseReturningOutputs()
    {
        var withKeyword = Advance();
        var openParen = Advance();
        var aliases = new List<ReturningOutputAlias> { ParseReturningOutputAlias() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            aliases.Add(ParseReturningOutputAlias());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new ReturningOutputs(withKeyword, openParen, aliases, closeParen);
    }

    private ReturningOutputAlias ParseReturningOutputAlias()
    {
        if (!IdentifierEquals(Keyword.Old) && !IdentifierEquals(Keyword.New))
        {
            throw new SqlParseException($"Expected OLD or NEW, found {_current.Kind}", _current.Position);
        }

        var kind = Advance();
        var asKeyword = Expect(SyntaxKind.AsKeyword);
        var alias = Expect(SyntaxKind.Identifier);
        return new ReturningOutputAlias
        {
            Span = SourceSpan.From(kind, alias),
            Kind = kind,
            AsKeyword = asKeyword,
            Alias = alias,
        };
    }

    // DELETE ... USING from_item [, ...]
    internal FromClause ParsePgUsingClause()
    {
        var usingKeyword = Advance();
        var from = ParseTableSource();
        var joins = ParseJoins();
        var extra = ParseCommaFrom();
        var end = (extra.Count > 0 ? extra[^1].Span : (SourceSpan?)null)
            ?? (joins.Count > 0 ? joins[^1].Span : (SourceSpan?)null)
            ?? from.Span;
        return new FromClause(usingKeyword, from, joins, extra, end);
    }

    // TRUNCATE [TABLE] [ONLY] name [*] [{RESTART | CONTINUE} IDENTITY]
    internal TruncateStatement ParsePgTruncate(SyntaxToken truncateKeyword)
    {
        var tableKeyword = IdentifierEquals(Keyword.Table) ? Advance() : (SyntaxToken?)null;
        var onlyKeyword = _current.Kind == SyntaxKind.OnlyKeyword ? Advance() : (SyntaxToken?)null;
        var tableName = ParseQualifiedName();
        var star = _current.Kind == SyntaxKind.Star ? Advance() : (SyntaxToken?)null;
        var identity = ParseTruncateIdentity();
        return new TruncateStatement
        {
            Span = SourceSpan.From(truncateKeyword, identity.IdentityKeyword ?? star ?? tableName[^1]),
            TruncateKeyword = truncateKeyword,
            TableKeyword = tableKeyword,
            OnlyKeyword = onlyKeyword,
            TableName = tableName,
            DescendantStar = star,
            RestartKeyword = identity.RestartKeyword,
            IdentityKeyword = identity.IdentityKeyword,
        };
    }
}
