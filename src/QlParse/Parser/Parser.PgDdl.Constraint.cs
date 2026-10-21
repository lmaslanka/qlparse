namespace QlParse;

internal sealed partial class Parser
{
    internal readonly record struct PgConstraintAttributes(
        SyntaxToken? InheritNoKeyword,
        SyntaxToken? InheritKeyword,
        SyntaxToken? ValidNotKeyword,
        SyntaxToken? ValidKeyword,
        SourceSpan? End);

    internal readonly record struct PgUsingIndexName(SyntaxToken? UsingKeyword, SyntaxToken? IndexKeyword, SyntaxToken? Name);

    // Constraint kinds only Postgres has, reached where the ISO constraint parser would fail.
    internal TableConstraint ParsePgConstraintKind(bool column, SyntaxToken start, SyntaxToken? constraintKeyword, SyntaxToken? constraintName)
    {
        if (!column && IdentifierEquals(Keyword.Exclude))
        {
            return ParsePgExclusionConstraint(start, constraintKeyword, constraintName);
        }

        if (column && _current.Kind == SyntaxKind.NullKeyword)
        {
            return ParsePgNullConstraint(start, constraintKeyword, constraintName);
        }

        throw new SqlParseException($"Expected constraint, found {_current.Kind}", _current.Position);
    }

    // `PRIMARY KEY USING INDEX name` / `UNIQUE USING INDEX name` (ALTER TABLE ... ADD only).
    internal PgUsingIndexName ParsePgUsingIndexName()
    {
        var usingKeyword = Advance();
        var indexKeyword = Advance();
        return new PgUsingIndexName(usingKeyword, indexKeyword, Expect(SyntaxKind.Identifier));
    }

    internal bool IsPgExcludeStart() =>
        IdentifierEquals(Keyword.Exclude) && NextKind is SyntaxKind.UsingKeyword or SyntaxKind.OpenParen;

    internal PgConstraintAttributes ParsePgConstraintTail() =>
        IsPostgres ? ParsePgConstraintAttributes() : default;

    // PRIMARY KEY / UNIQUE key columns, the ALTER TABLE `USING INDEX name` form, and index parameters.
    internal UniqueKeyColumns ParsePgUniqueKeyColumns(bool column)
    {
        var usingIndex = !column && IsPgUsingIndexName() ? ParsePgUsingIndexName() : default;
        SyntaxToken? openParen = null;
        IReadOnlyList<SyntaxToken>? columns = null;
        SyntaxToken? closeParen = null;
        if (!column && usingIndex.Name is null)
        {
            ParseRequiredNameList(out var keyOpen, out var keyColumns, out var keyClose);
            openParen = keyOpen;
            columns = keyColumns;
            closeParen = keyClose;
        }

        var indexParameters = TryParsePgIndexParameters();
        var end = indexParameters?.Span ?? usingIndex.Name?.Span ?? closeParen?.Span;
        return new UniqueKeyColumns(openParen, columns, closeParen, usingIndex, indexParameters, end);
    }

    // NOT VALID and NO INHERIT after a constraint's ISO characteristics, in any order.
    internal PgConstraintAttributes ParsePgConstraintAttributes()
    {
        SyntaxToken? inheritNo = null;
        SyntaxToken? inherit = null;
        SyntaxToken? validNot = null;
        SyntaxToken? valid = null;
        SourceSpan? end = null;
        while (true)
        {
            if (inherit is null && IdentifierEquals(Keyword.No) && NextEquals(Keyword.DdlInherit))
            {
                inheritNo = Advance();
                var inheritToken = Advance();
                inherit = inheritToken;
                end = inheritToken.Span;
                continue;
            }

            if (valid is null && _current.Kind == SyntaxKind.NotKeyword && NextEquals(Keyword.DdlValid))
            {
                validNot = Advance();
                var validToken = Advance();
                valid = validToken;
                end = validToken.Span;
                continue;
            }

            return new PgConstraintAttributes(inheritNo, inherit, validNot, valid, end);
        }
    }

    // Column-level `NULL`: Postgres accepts it as an explicit (no-op) nullability constraint.
    internal TableConstraint ParsePgNullConstraint(SyntaxToken start, SyntaxToken? constraintKeyword, SyntaxToken? constraintName)
    {
        var nullKeyword = Advance();
        return new TableConstraint
        {
            Span = SourceSpan.From(start, nullKeyword),
            ConstraintKeyword = constraintKeyword,
            ConstraintName = constraintName,
            NullKeyword = nullKeyword,
        };
    }

    internal bool IsPgUsingIndexName() =>
        _current.Kind == SyntaxKind.UsingKeyword
        && NextEquals(Keyword.Index)
        && !IsKeywordAt(_index + 1, Keyword.DdlTablespace);

    // `[INCLUDE (cols)] [NULLS [NOT] DISTINCT] [WITH (params)] [USING INDEX TABLESPACE ts | TABLESPACE ts]`:
    // the index options shared by CREATE INDEX and PRIMARY KEY / UNIQUE / EXCLUDE constraints.
    internal IndexParameters? TryParsePgIndexParameters()
    {
        var start = _current;
        var include = ParsePgInclude();
        var nulls = IdentifierEquals(Keyword.Nulls) ? ParsePgNullsDistinct() : default;
        var (withKeyword, parameters) = TryParsePgWithParameters();
        var tablespace = ParsePgIndexTablespace();
        var end = tablespace.Name?.Span
            ?? parameters?.Span
            ?? nulls.Distinct?.Span
            ?? include.CloseParen?.Span;
        if (end is not { } endSpan)
        {
            return null;
        }

        return new IndexParameters
        {
            Span = SourceSpan.From(start, endSpan),
            IncludeKeyword = include.Keyword,
            IncludeOpen = include.OpenParen,
            Include = include.Columns,
            IncludeClose = include.CloseParen,
            NullsKeyword = nulls.Nulls,
            NullsNotKeyword = nulls.Not,
            DistinctKeyword = nulls.Distinct,
            WithKeyword = withKeyword,
            Parameters = parameters,
            UsingKeyword = tablespace.UsingKeyword,
            IndexKeyword = tablespace.IndexKeyword,
            TablespaceKeyword = tablespace.Keyword,
            Tablespace = tablespace.Name,
        };
    }

    private readonly record struct PgInclude(
        SyntaxToken? Keyword,
        SyntaxToken? OpenParen,
        IReadOnlyList<SyntaxToken>? Columns,
        SyntaxToken? CloseParen);

    private readonly record struct PgNullsDistinct(SyntaxToken? Nulls, SyntaxToken? Not, SyntaxToken? Distinct);

    private readonly record struct PgIndexTablespace(
        SyntaxToken? UsingKeyword,
        SyntaxToken? IndexKeyword,
        SyntaxToken? Keyword,
        SyntaxToken? Name);

    private PgInclude ParsePgInclude()
    {
        if (!IdentifierEquals(Keyword.DdlInclude))
        {
            return default;
        }

        var includeKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var columns = ParseNameList();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new PgInclude(includeKeyword, openParen, columns, closeParen);
    }

    private PgNullsDistinct ParsePgNullsDistinct()
    {
        ParseNullsDistinct(out var nullsKeyword, out var notKeyword, out var distinctKeyword);
        return new PgNullsDistinct(nullsKeyword, notKeyword, distinctKeyword);
    }

    private PgIndexTablespace ParsePgIndexTablespace()
    {
        if (_current.Kind == SyntaxKind.UsingKeyword && NextEquals(Keyword.Index) && IsKeywordAt(_index + 1, Keyword.DdlTablespace))
        {
            var usingKeyword = Advance();
            var indexKeyword = Advance();
            var tablespaceKeyword = Advance();
            return new PgIndexTablespace(usingKeyword, indexKeyword, tablespaceKeyword, Expect(SyntaxKind.Identifier));
        }

        var (keyword, name) = TryParsePgTablespaceClause();
        return new PgIndexTablespace(null, null, keyword, name);
    }

    // `[CONSTRAINT name] EXCLUDE [USING method] (element WITH operator [, ...]) index_parameters [WHERE (predicate)]`.
    internal TableConstraint ParsePgExclusionConstraint(SyntaxToken start, SyntaxToken? constraintKeyword, SyntaxToken? constraintName)
    {
        var exclusion = ParsePgExclusionClause();
        ParseConstraintCharacteristics(out var deferrableNot, out var deferrable, out var initially, out var initiallyWhen);
        var attributes = ParsePgConstraintAttributes();
        var end = attributes.End ?? initiallyWhen?.Span ?? deferrable?.Span ?? exclusion.Span;
        return new TableConstraint
        {
            Span = SourceSpan.From(start, end),
            ConstraintKeyword = constraintKeyword,
            ConstraintName = constraintName,
            Exclusion = exclusion,
            DeferrableNotKeyword = deferrableNot,
            DeferrableKeyword = deferrable,
            InitiallyKeyword = initially,
            InitiallyWhen = initiallyWhen,
            InheritNoKeyword = attributes.InheritNoKeyword,
            InheritKeyword = attributes.InheritKeyword,
            ValidNotKeyword = attributes.ValidNotKeyword,
            ValidKeyword = attributes.ValidKeyword,
        };
    }

    private ExclusionClause ParsePgExclusionClause()
    {
        var excludeKeyword = Advance();
        var (usingKeyword, method) = TryParsePgUsingMethod();
        var openParen = Expect(SyntaxKind.OpenParen);
        var elements = new List<ExclusionElement> { ParsePgExclusionElement() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            elements.Add(ParsePgExclusionElement());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        var indexParameters = TryParsePgIndexParameters();
        SyntaxToken? whereKeyword = null;
        SyntaxToken? whereOpen = null;
        Expression? where = null;
        SyntaxToken? whereClose = null;
        if (_current.Kind == SyntaxKind.WhereKeyword)
        {
            whereKeyword = Advance();
            whereOpen = Expect(SyntaxKind.OpenParen);
            where = ParseExpression();
            whereClose = Expect(SyntaxKind.CloseParen);
        }

        var end = whereClose?.Span ?? indexParameters?.Span ?? closeParen.Span;
        return new ExclusionClause
        {
            Span = SourceSpan.From(excludeKeyword, end),
            ExcludeKeyword = excludeKeyword,
            UsingKeyword = usingKeyword,
            Method = method,
            OpenParen = openParen,
            Elements = elements,
            CloseParen = closeParen,
            IndexParameters = indexParameters,
            WhereKeyword = whereKeyword,
            WhereOpen = whereOpen,
            Where = where,
            WhereClose = whereClose,
        };
    }

    private ExclusionElement ParsePgExclusionElement()
    {
        var column = ParseIndexColumn();
        var withKeyword = Expect(SyntaxKind.WithKeyword);
        if (!IsOperatorKind(_current.Kind))
        {
            throw new SqlParseException($"Expected operator, found {_current.Kind}", _current.Position);
        }

        var operatorToken = Advance();
        return new ExclusionElement
        {
            Span = SourceSpan.From(column.Span, operatorToken),
            Column = column,
            WithKeyword = withKeyword,
            Operator = operatorToken,
        };
    }

    // `ON DELETE SET NULL (cols)` / `SET DEFAULT (cols)` (Postgres 15).
    internal (SyntaxToken? OpenParen, IReadOnlyList<SyntaxToken>? Columns, SyntaxToken? CloseParen) TryParsePgActionColumns()
    {
        if (_current.Kind != SyntaxKind.OpenParen)
        {
            return (null, null, null);
        }

        var openParen = Advance();
        var columns = ParseNameList();
        return (openParen, columns, Expect(SyntaxKind.CloseParen));
    }
}
