namespace QlParse;

// SQL Server gap closing (workstream H). Under SqlOptions.SqlServer only the T-SQL reserved keywords keep
// their ISO keyword kinds (see Keyword.TsqlKind), so DATE, TIME, POSITION, WINDOW, OFFSET, ... are plain
// identifiers. The T-SQL forms that still give such a word a meaning match it by text: an unquoted word
// that is an ISO keyword never lexes as an identifier without the flag, so these tests are T-SQL-only.
internal sealed partial class Parser
{
    // `name(`: CAST and TRIM keep their ISO grammar, MATCH is the graph predicate; anything else is an
    // ordinary call.
    internal Expression? TryParseTsqlGapsCall()
    {
        if (IdentifierEquals(Keyword.Cast))
        {
            return ParseCast();
        }

        if (IdentifierEquals(Keyword.Trim))
        {
            return ParseTrim();
        }

        return IdentifierEquals(Keyword.Match) ? ParseTsqlMatch() : null;
    }

    // Non-reserved words that end a table's implicit alias: MERGE's USING, and WINDOW when `name AS` follows.
    internal bool IsTsqlClauseWordAfterTable() =>
        IdentifierEquals(Keyword.Using)
        || (IdentifierEquals(Keyword.Window) && NextKind == SyntaxKind.Identifier && _index + 1 < _tokens.Count
            && _tokens[_index + 1].Kind == SyntaxKind.AsKeyword);

    // `.5`: a number without an integer part lexes as a dot and a number; written together they are one numeric
    // literal.
    internal Expression? TryParseTsqlGapsPrefix()
    {
        if (_current.Kind != SyntaxKind.Dot || NextKind != SyntaxKind.Number || _tokens[_index].Position != _current.Span.End)
        {
            return null;
        }

        var dot = Advance();
        var number = Advance();
        var literal = new SyntaxToken(SyntaxKind.Number, dot.Position, number.Span.End - dot.Position, dot.LeadingTriviaStart, dot.LeadingTriviaCount);
        return new LiteralExpression { Literal = literal, Span = literal.Span };
    }

    // `db..t.col`: an omitted name part is a zero-width identifier at the following dot, as in a FROM item
    // (see ExpectTsqlNamePart). Without SQL Server this fails exactly like Expect(Identifier).
    internal SyntaxToken ExpectTsqlOmittedMember() =>
        IsSqlServer && _current.Kind == SyntaxKind.Dot
            ? new SyntaxToken(SyntaxKind.Identifier, _current.Position, 0, _current.LeadingTriviaStart, 0)
            : Expect(SyntaxKind.Identifier);

    // `(query UNION query) ORDER BY ... [OFFSET ... FETCH ...]`: the ordering of a query expression whose last
    // operand is parenthesised (a trailing SELECT keeps its own ORDER BY).
    internal OrderedQuery ParseTsqlOrderedQuery(Query query)
    {
        var ordering = ParseTsqlSelectOrdering();
        return new OrderedQuery
        {
            Span = SourceSpan.From(query.Span, ordering.End ?? query.Span),
            Query = query,
            OrderBy = ordering.OrderBy,
            Offset = ordering.Offset,
            Fetch = ordering.Fetch,
        };
    }

    // `table (NOLOCK)`: the table hints SQL Server still takes without WITH, told apart from a table-valued
    // function call by the first word.
    internal bool IsTsqlLegacyTableHints() =>
        NextKind == SyntaxKind.Identifier
        && _index + 1 < _tokens.Count
        && _tokens[_index + 1].Kind is SyntaxKind.CloseParen or SyntaxKind.Comma
        && Keyword.IsTsqlLegacyTableHint(_tokens[_index].TextOf(_source));

    internal TsqlHintedTable ParseTsqlLegacyHintedReference(List<SyntaxToken> nameParts) =>
        ParseTsqlHintedTable(new TableReference { Span = SourceSpan.From(nameParts[0], nameParts[^1]), NameParts = nameParts });

    // `table [AS] alias (NOLOCK, ...)`: the shared alias parser reads the hints as a column alias list, which a
    // plain table cannot have in T-SQL, so a list of legacy hint words turns back into the hints it is.
    internal TableSource ConvertTsqlLegacyHintColumns(TableSource table)
    {
        if (table is not TableReference { Columns: { } columns } reference || !AreTsqlLegacyHints(columns))
        {
            return table;
        }

        List<TsqlHint> hints = [.. columns.Select(column => new TsqlHint { Span = column.Span, Keywords = [column] })];
        var unhinted = new TableReference
        {
            Span = SourceSpan.From(reference.NameParts[0].Span, reference.Alias!.Value.Span),
            NameParts = reference.NameParts,
            AsKeyword = reference.AsKeyword,
            Alias = reference.Alias,
            SystemTime = reference.SystemTime,
        };
        return new TsqlHintedTable
        {
            Span = reference.Span,
            Table = unhinted,
            OpenParen = reference.ColumnOpenParen!.Value,
            Hints = hints,
            CloseParen = reference.ColumnCloseParen!.Value,
        };
    }

    private bool AreTsqlLegacyHints(IReadOnlyList<SyntaxToken> columns)
    {
        foreach (var column in columns)
        {
            if (!Keyword.IsTsqlLegacyTableHint(column.TextOf(_source)))
            {
                return false;
            }
        }

        return true;
    }

    // Expect(kind), or the same word as a T-SQL identifier. Only reached once the keyword kind is absent,
    // so the ISO path keeps its single comparison and its error message.
    internal SyntaxToken ExpectTsqlWord(SyntaxKind kind, string word) => IdentifierEquals(word) ? Advance() : Expect(kind);
}
