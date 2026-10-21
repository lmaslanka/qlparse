namespace QlParse;

internal sealed partial class Parser
{
    // Reached from ParseTableReference only in Postgres mode, when a bare name is followed by `*`
    // (explicit descendant inclusion) or FROM (the `ROWS FROM (...)` table function).
    internal TableSource ParsePgTableReferenceTail(IReadOnlyList<SyntaxToken> nameParts, SystemTimeClause? systemTime)
    {
        if (_current.Kind == SyntaxKind.FromKeyword)
        {
            if (nameParts.Count == 1 && TokenEquals(nameParts[0], Keyword.Rows))
            {
                return ParseRowsFrom(nameParts[0]);
            }

            return new TableReference
            {
                Span = SourceSpan.From(nameParts[0].Span, systemTime?.Span ?? nameParts[^1].Span),
                NameParts = nameParts,
                SystemTime = systemTime,
            };
        }

        var star = Advance();
        var alias = ParsePgAliasClause();
        return new DescendantTable
        {
            Span = SourceSpan.From(nameParts[0], alias.End ?? star),
            NameParts = nameParts,
            Star = star,
            AsKeyword = alias.AsKeyword,
            Alias = alias.Alias,
            ColumnOpenParen = alias.ColumnOpenParen,
            Columns = alias.Columns,
            ColumnCloseParen = alias.ColumnCloseParen,
        };
    }

    // Postgres FROM ONLY name [[AS] alias [(columns)]] without the ISO parentheses.
    internal OnlyTable ParsePgOnlyTableReference()
    {
        var onlyKeyword = Advance();
        var nameParts = ParseQualifiedName();
        var alias = ParsePgAliasClause();
        return new OnlyTable
        {
            Span = SourceSpan.From(onlyKeyword, alias.End ?? nameParts[^1]),
            OnlyKeyword = onlyKeyword,
            NameParts = nameParts,
            AsKeyword = alias.AsKeyword,
            Alias = alias.Alias,
            ColumnOpenParen = alias.ColumnOpenParen,
            Columns = alias.Columns,
            ColumnCloseParen = alias.ColumnCloseParen,
        };
    }

    private readonly record struct PgAlias(
        SyntaxToken? AsKeyword,
        SyntaxToken? Alias,
        SyntaxToken? ColumnOpenParen,
        IReadOnlyList<SyntaxToken>? Columns,
        SyntaxToken? ColumnCloseParen,
        SyntaxToken? End);

    private PgAlias ParsePgAliasClause()
    {
        SyntaxToken? asKeyword = null;
        SyntaxToken alias;
        if (_current.Kind == SyntaxKind.AsKeyword)
        {
            asKeyword = Advance();
            alias = Expect(SyntaxKind.Identifier);
        }
        else
        {
            if (_current.Kind != SyntaxKind.Identifier || IsPgReservedClauseWord() || TokenEquals(_current, Keyword.MatchRecognize))
            {
                return default;
            }

            alias = Advance();
        }

        ParseOptionalColumnList(out var columnOpen, out var columns, out var columnClose);
        return new PgAlias(asKeyword, alias, columnOpen, columns, columnClose, columnClose ?? alias);
    }

    // ROWS FROM (function [AS (column type, ...)] [, ...]) [WITH ORDINALITY] [[AS] alias [(columns)]]
    private RowsFromTable ParseRowsFrom(SyntaxToken rowsKeyword)
    {
        var fromKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var functions = new List<RowsFromFunction> { ParseRowsFromFunction() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            functions.Add(ParseRowsFromFunction());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        var ordinality = ParsePgOrdinality();
        var alias = ParsePgAliasClause();
        return new RowsFromTable
        {
            Span = SourceSpan.From(rowsKeyword.Span, alias.End?.Span ?? ordinality?.Span ?? closeParen.Span),
            RowsKeyword = rowsKeyword,
            FromKeyword = fromKeyword,
            OpenParen = openParen,
            Functions = functions,
            CloseParen = closeParen,
            Ordinality = ordinality,
            AsKeyword = alias.AsKeyword,
            Alias = alias.Alias,
            ColumnOpenParen = alias.ColumnOpenParen,
            Columns = alias.Columns,
            ColumnCloseParen = alias.ColumnCloseParen,
        };
    }

    private RowsFromFunction ParseRowsFromFunction()
    {
        var function = ParseExpression();
        if (_current.Kind != SyntaxKind.AsKeyword)
        {
            return new RowsFromFunction { Span = function.Span, Function = function };
        }

        var asKeyword = Advance();
        var openParen = Expect(SyntaxKind.OpenParen);
        var definitions = new List<FieldDefinition> { ParseColumnDefinitionElement() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            definitions.Add(ParseColumnDefinitionElement());
        }

        var closeParen = Expect(SyntaxKind.CloseParen);
        return new RowsFromFunction
        {
            Span = SourceSpan.From(function.Span, closeParen),
            Function = function,
            AsKeyword = asKeyword,
            OpenParen = openParen,
            ColumnDefinitions = definitions,
            CloseParen = closeParen,
        };
    }

    private FieldDefinition ParseColumnDefinitionElement() => FinishColumnDefinition(Expect(SyntaxKind.Identifier));

    private FieldDefinition FinishColumnDefinition(SyntaxToken name)
    {
        var type = ParseDataType();
        return new FieldDefinition { Span = SourceSpan.From(name, type.Span), Name = name, Type = type };
    }

    private OrdinalityClause? ParsePgOrdinality()
    {
        if (_current.Kind != SyntaxKind.WithKeyword || !NextIsOrdinality())
        {
            return null;
        }

        var withKeyword = Advance();
        var ordinalityKeyword = Advance();
        return new OrdinalityClause
        {
            Span = SourceSpan.From(withKeyword, ordinalityKeyword),
            WithKeyword = withKeyword,
            OrdinalityKeyword = ordinalityKeyword,
        };
    }

    // A function in FROM: [WITH ORDINALITY] then [AS] alias [(columns | column definitions)] or AS (column definitions).
    internal TableFunction ParsePgRoutineTable(
        IReadOnlyList<SyntaxToken> nameParts,
        SyntaxToken openParen,
        IReadOnlyList<Expression> arguments,
        SyntaxToken closeParen)
    {
        var ordinality = ParsePgOrdinality();
        var correlation = ParsePgFunctionCorrelation();
        return new TableFunction
        {
            Span = SourceSpan.From(nameParts[0].Span, correlation.End?.Span ?? ordinality?.Span ?? closeParen.Span),
            NameParts = nameParts,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
            Ordinality = ordinality,
            AsKeyword = correlation.AsKeyword,
            Alias = correlation.Alias,
            ColumnOpenParen = correlation.ColumnOpenParen,
            Columns = correlation.Columns,
            ColumnDefinitions = correlation.Definitions ?? [],
            ColumnCloseParen = correlation.ColumnCloseParen,
        };
    }

    private readonly record struct PgFunctionCorrelation(
        SyntaxToken? AsKeyword,
        SyntaxToken? Alias,
        SyntaxToken? ColumnOpenParen,
        IReadOnlyList<SyntaxToken>? Columns,
        IReadOnlyList<FieldDefinition>? Definitions,
        SyntaxToken? ColumnCloseParen,
        SyntaxToken? End);

    private PgFunctionCorrelation ParsePgFunctionCorrelation()
    {
        var asKeyword = _current.Kind == SyntaxKind.AsKeyword ? Advance() : (SyntaxToken?)null;
        var alias = _current.Kind == SyntaxKind.Identifier && !IsPgReservedClauseWord() && !TokenEquals(_current, Keyword.MatchRecognize)
            ? Advance()
            : (SyntaxToken?)null;
        if (_current.Kind != SyntaxKind.OpenParen || (asKeyword is null && alias is null))
        {
            if (asKeyword is not null && alias is null)
            {
                throw new SqlParseException("Expected alias or column definition list", _current.Position);
            }

            return new PgFunctionCorrelation(asKeyword, alias, null, null, null, null, alias);
        }

        var list = ParsePgColumnNamesOrDefinitions();
        return new PgFunctionCorrelation(asKeyword, alias, list.OpenParen, list.Names, list.Definitions, list.CloseParen, list.CloseParen);
    }

    private readonly record struct PgColumnList(
        SyntaxToken OpenParen,
        IReadOnlyList<SyntaxToken>? Names,
        IReadOnlyList<FieldDefinition>? Definitions,
        SyntaxToken CloseParen);

    // `(a, b)` column aliases, or `(a int, b text)` column definitions for record-returning functions.
    private PgColumnList ParsePgColumnNamesOrDefinitions()
    {
        var openParen = Advance();
        var first = Expect(SyntaxKind.Identifier);
        if (_current.Kind is SyntaxKind.Comma or SyntaxKind.CloseParen)
        {
            var names = new List<SyntaxToken> { first };
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                names.Add(Expect(SyntaxKind.Identifier));
            }

            return new PgColumnList(openParen, names, null, Expect(SyntaxKind.CloseParen));
        }

        var definitions = new List<FieldDefinition> { FinishColumnDefinition(first) };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            definitions.Add(ParseColumnDefinitionElement());
        }

        return new PgColumnList(openParen, null, definitions, Expect(SyntaxKind.CloseParen));
    }
}
