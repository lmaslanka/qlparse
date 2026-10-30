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

    // `table (NOLOCK)`, `table (INDEX(ix), ...)`: the table hints SQL Server still takes without WITH, told apart
    // from a table-valued function call or a column alias list by the first hint.
    internal bool IsTsqlLegacyTableHints() =>
        NextKind == SyntaxKind.Identifier
        && _index + 1 < _tokens.Count
        && IsTsqlLegacyHintStart(_tokens[_index].TextOf(_source), _tokens[_index + 1].Kind);

    // `table [AS] alias (NOLOCK, ...)`: the shared table-reference parser leaves the list to ParseTsqlTableSourceTail
    // instead of reading it as column aliases. Only reached when a `(` follows the alias.
    internal bool IsTsqlAliasedTableHints() => IsSqlServer && IsTsqlLegacyTableHints();

    // `WITH (` before table hints, or the `(` of a legacy hint list.
    internal bool IsTsqlTableHintsStart() =>
        _current.Kind == SyntaxKind.WithKeyword
            ? NextKind == SyntaxKind.OpenParen
            : _current.Kind == SyntaxKind.OpenParen && IsTsqlLegacyTableHints();

    private static bool IsTsqlLegacyHintStart(ReadOnlySpan<char> word, SyntaxKind next) =>
        next is SyntaxKind.CloseParen or SyntaxKind.Comma
            ? Keyword.IsTsqlLegacyTableHint(word)
            : next is SyntaxKind.OpenParen or SyntaxKind.EqualsToken
                && word.Equals(Keyword.Index, StringComparison.OrdinalIgnoreCase);

    internal TsqlHintedTable ParseTsqlLegacyHintedReference(List<SyntaxToken> nameParts) =>
        ParseTsqlHintedTable(new TableReference { Span = SourceSpan.From(nameParts[0], nameParts[^1]), NameParts = nameParts });

    // Expect(kind), or the same word as a T-SQL identifier. Only reached once the keyword kind is absent,
    // so the ISO path keeps its single comparison and its error message.
    internal SyntaxToken ExpectTsqlWord(SyntaxKind kind, string word) => IdentifierEquals(word) ? Advance() : Expect(kind);
}
