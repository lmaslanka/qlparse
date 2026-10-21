namespace QlParse;

// CREATE [OR REPLACE] {FUNCTION | PROCEDURE} under Postgres: parameter lists with optional names and
// defaults, RETURNS [SETOF] type / RETURNS TABLE (...), %TYPE references, the attribute list, the
// AS 'definition' clause and the SQL-standard bodies (RETURN expr, BEGIN ATOMIC ... END).
internal sealed partial class Parser
{
    private readonly record struct PgParameterList(SyntaxToken OpenParen, IReadOnlyList<RoutineParameter> Parameters, SyntaxToken CloseParen);

    private readonly record struct PgReturnsClause(
        SyntaxToken? ReturnsKeyword,
        SyntaxToken? SetofKeyword,
        DataType? Type,
        PgTypeAttribute? TypeAttribute,
        PgReturnsTable? Table,
        SyntaxToken? AsKeyword = null,
        SyntaxToken? Locator = null);

    private readonly record struct IsoRoutineBody(
        SyntaxToken? SqlKeyword, Query? Body, SyntaxToken? ExternalKeyword, SyntaxToken? NameKeyword, SyntaxToken? ExternalName);

    private CreateRoutineStatement ParsePgCreateRoutine()
    {
        var createKeyword = Advance();
        SyntaxToken? orKeyword = _current.Kind == SyntaxKind.OrKeyword ? Advance() : null;
        SyntaxToken? replaceKeyword = orKeyword is null ? null : Advance();
        var routineKeyword = Advance();
        var name = ParseQualifiedName();
        var parameters = ParsePgParameterList();
        var returns = ParsePgReturnsClause();
        var options = ParsePgRoutineOptions();
        var body = options.Definition is { } definition
            ? ParsePgRoutineBody(definition, PgBodyLanguageOf(options.Characteristics))
            : new PgRoutineBody(TryParsePgStandardBody(), [], []);
        var isoBody = options.Definition is null && body.Body is null ? TryParseIsoRoutineBody() : default;
        return new CreateRoutineStatement
        {
            Span = SourceSpan.From(createKeyword, PgPreviousToken()),
            CreateKeyword = createKeyword,
            OrKeyword = orKeyword,
            ReplaceKeyword = replaceKeyword,
            RoutineKeyword = routineKeyword,
            Name = name,
            OpenParen = parameters.OpenParen,
            Parameters = parameters.Parameters,
            CloseParen = parameters.CloseParen,
            ReturnsKeyword = returns.ReturnsKeyword,
            SetofKeyword = returns.SetofKeyword,
            ReturnsType = returns.Type,
            ReturnsTypeAttribute = returns.TypeAttribute,
            ReturnsTable = returns.Table,
            ReturnsAsKeyword = returns.AsKeyword,
            ReturnsLocator = returns.Locator,
            Characteristics = options.Characteristics,
            AsKeyword = options.AsKeyword,
            Definition = options.Definition,
            LinkSymbol = options.LinkSymbol,
            SqlKeyword = isoBody.SqlKeyword,
            Body = body.Body ?? isoBody.Body,
            ExternalKeyword = isoBody.ExternalKeyword,
            NameKeyword = isoBody.NameKeyword,
            ExternalName = isoBody.ExternalName,
            BodyTokens = body.Tokens,
            BodyTrivia = body.Trivia,
        };
    }

    private PgParameterList ParsePgParameterList()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var parameters = new List<RoutineParameter>();
        if (_current.Kind != SyntaxKind.CloseParen)
        {
            parameters.Add(ParsePgRoutineParameter());
            while (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                parameters.Add(ParsePgRoutineParameter());
            }
        }

        return new PgParameterList(openParen, parameters, Expect(SyntaxKind.CloseParen));
    }

    // [IN | OUT | INOUT | VARIADIC] [name] type [%TYPE | %ROWTYPE] [{DEFAULT | =} expr]
    internal RoutineParameter ParsePgRoutineParameter()
    {
        var start = _current;
        SyntaxToken? mode = IsPgArgumentMode() && NextKind is not SyntaxKind.Comma and not SyntaxKind.CloseParen ? Advance() : null;
        SyntaxToken? name = IsPgParameterName() ? Advance() : null;
        var type = ParseDataType();
        var typeAttribute = TryParsePgTypeAttribute();
        SyntaxToken? defaultToken = IdentifierEquals(Keyword.Default) || _current.Kind == SyntaxKind.EqualsToken ? Advance() : null;
        var defaultValue = defaultToken is null ? null : ParseExpression();
        return new RoutineParameter
        {
            Span = SourceSpan.From(start, PgPreviousToken()),
            Mode = mode,
            Name = name,
            Type = type,
            TypeAttribute = typeAttribute,
            DefaultToken = defaultToken,
            Default = defaultValue,
        };
    }

    // A parameter name is an identifier followed by the start of a type, unless the two words are
    // one multi-word type name (double precision, character varying, bit varying).
    private bool IsPgParameterName() =>
        IsParameterName() && !NextEquals(Keyword.Default) && !NextEquals(Keyword.Varying);

    internal PgTypeAttribute? TryParsePgTypeAttribute()
    {
        if (_current.Kind != SyntaxKind.PercentToken || !(NextEquals(Keyword.Type) || NextEquals(Keyword.PlpgsqlRowtype)))
        {
            return null;
        }

        var percent = Advance();
        var attribute = Advance();
        return new PgTypeAttribute { Span = SourceSpan.From(percent, attribute), Percent = percent, Attribute = attribute };
    }

    // RETURNS [SETOF] type, or RETURNS TABLE (name type, ...). RETURNS NULL ON NULL INPUT is an attribute.
    private PgReturnsClause ParsePgReturnsClause()
    {
        if (!IdentifierEquals(Keyword.Returns) || NextKind == SyntaxKind.NullKeyword)
        {
            return default;
        }

        var returnsKeyword = Advance();
        if (IdentifierEquals(Keyword.Table) && NextKind == SyntaxKind.OpenParen)
        {
            var tableKeyword = Advance();
            var columns = ParsePgParameterList();
            var table = new PgReturnsTable
            {
                Span = SourceSpan.From(tableKeyword, columns.CloseParen),
                TableKeyword = tableKeyword,
                OpenParen = columns.OpenParen,
                Columns = columns.Parameters,
                CloseParen = columns.CloseParen,
            };
            return new PgReturnsClause(returnsKeyword, null, null, null, table);
        }

        SyntaxToken? setof = IdentifierEquals(Keyword.RoutineSetof) ? Advance() : null;
        var type = ParseDataType();
        var typeAttribute = TryParsePgTypeAttribute();
        if (_current.Kind == SyntaxKind.AsKeyword && NextEquals(Keyword.Locator))
        {
            var asKeyword = Advance();
            return new PgReturnsClause(returnsKeyword, setof, type, typeAttribute, null, asKeyword, Advance());
        }

        return new PgReturnsClause(returnsKeyword, setof, type, typeAttribute, null);
    }

    // The ISO bodies (SQL statement, EXTERNAL [NAME name]) stay accepted under Postgres.
    private IsoRoutineBody TryParseIsoRoutineBody()
    {
        if (!IdentifierEquals(Keyword.Sql) && !IdentifierEquals(Keyword.External))
        {
            return default;
        }

        ParseRoutineBody(out var sqlKeyword, out var body, out var externalKeyword, out var nameKeyword, out var externalName);
        return new IsoRoutineBody(sqlKeyword, body, externalKeyword, nameKeyword, externalName);
    }

    // PG14 SQL-standard bodies: RETURN expr, or BEGIN ATOMIC statements END.
    private Query? TryParsePgStandardBody()
    {
        if (IdentifierEquals(Keyword.Return))
        {
            return ParseReturn();
        }

        return IdentifierEquals(Keyword.Begin) && NextEquals(Keyword.Atomic) ? ParseCompound() : null;
    }
}
