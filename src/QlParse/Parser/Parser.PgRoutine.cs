namespace QlParse;

// Workstream G: functions, procedures and PL/pgSQL. The hook runs last in the Postgres chain, before
// ISO dispatch, so it rejects anything that is not DO / CALL / CREATE / ALTER / DROP by token kind and
// length before comparing any text.
internal sealed partial class Parser
{
    internal Query? TryParsePgRoutineStatement()
    {
        if (_current.Kind != SyntaxKind.Identifier)
        {
            return null;
        }

        return _current.Length switch
        {
            Len2 => IdentifierEquals(Keyword.Do) && IsPgDoStart() ? ParsePgDo() : null,
            Len4 => TryParsePgRoutineLen4(),
            Len5 => IdentifierEquals(Keyword.Alter) && IsPgRoutineKindAt(_index) ? ParsePgAlterRoutine() : null,
            Len6 => IdentifierEquals(Keyword.Create) ? TryParsePgCreateRoutine() : null,
            _ => null,
        };
    }

    private Query? TryParsePgRoutineLen4()
    {
        if (IdentifierEquals(Keyword.Call))
        {
            return ParsePgCall();
        }

        return IdentifierEquals(Keyword.Drop) && IsPgRoutineKindAt(_index) ? ParsePgDropRoutine() : null;
    }

    private Query? TryParsePgCreateRoutine()
    {
        var index = _index;
        if (NextKind == SyntaxKind.OrKeyword && IsKeywordAt(index + 1, Keyword.PgReplace))
        {
            index += Len2;
        }

        return IsKeywordAt(index, Keyword.Function) || IsKeywordAt(index, Keyword.Procedure) ? ParsePgCreateRoutine() : null;
    }

    private bool IsPgRoutineKindAt(int index) =>
        IsKeywordAt(index, Keyword.Function) || IsKeywordAt(index, Keyword.Procedure) || IsKeywordAt(index, Keyword.Routine);

    private bool IsPgDoStart() => NextKind == SyntaxKind.String || NextEquals(Keyword.Language);

    // The last token consumed so far: _current is _tokens[_index - 1].
    private SyntaxToken PgPreviousToken() => _tokens[_index - Len2];

    // DO [LANGUAGE lang] code, or DO code LANGUAGE lang; the language defaults to plpgsql.
    private PgDoStatement ParsePgDo()
    {
        var doKeyword = Advance();
        SyntaxToken? languageKeyword = null;
        SyntaxToken? language = null;
        if (IdentifierEquals(Keyword.Language))
        {
            languageKeyword = Advance();
            language = ExpectPgNameOrString();
        }

        var code = Expect(SyntaxKind.String);
        if (languageKeyword is null && IdentifierEquals(Keyword.Language))
        {
            languageKeyword = Advance();
            language = ExpectPgNameOrString();
        }

        var body = ParsePgRoutineBody(code, language is { } name ? PgBodyLanguageOf(name) : PgBodyLanguage.Plpgsql);
        return new PgDoStatement
        {
            Span = SourceSpan.From(doKeyword, PgPreviousToken()),
            DoKeyword = doKeyword,
            LanguageKeyword = languageKeyword,
            Language = language,
            Code = code,
            Body = body.Body,
            BodyTokens = body.Tokens,
            BodyTrivia = body.Trivia,
        };
    }

    // CALL name(args): Postgres arguments may be named (a => 1, a := 1) or VARIADIC.
    private CallStatement ParsePgCall()
    {
        var callKeyword = Advance();
        var name = ParseQualifiedName();
        var openParen = Expect(SyntaxKind.OpenParen);
        var arguments = ParsePgFunctionArguments();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new CallStatement
        {
            Span = SourceSpan.From(callKeyword, closeParen),
            CallKeyword = callKeyword,
            Name = name,
            OpenParen = openParen,
            Arguments = arguments,
            CloseParen = closeParen,
        };
    }

    // DROP {FUNCTION | PROCEDURE | ROUTINE} [IF EXISTS] name [(args)] [, ...] [CASCADE | RESTRICT]
    private PgDropStatement ParsePgDropRoutine() => ParsePgDrop(new PgObjectKind(1, PgObjectShape.Routine, false, false));

    // ALTER {FUNCTION | PROCEDURE | ROUTINE} name [(args)] followed by either a run of routine attributes
    // or one of the shared actions (RENAME TO, OWNER TO, SET SCHEMA, [NO] DEPENDS ON EXTENSION).
    private PgAlterStatement ParsePgAlterRoutine()
    {
        var alterKeyword = Advance();
        var objectKind = ParsePgObjectKindTokens(1);
        var target = ParsePgRoutineReference();
        var action = IsPgRoutineSharedActionStart() ? ParsePgAlterAction(PgAlterContext.General) : ParsePgRoutineAttributesAction();
        return new PgAlterStatement
        {
            Span = SourceSpan.From(alterKeyword, action.Span),
            AlterKeyword = alterKeyword,
            ObjectKind = objectKind,
            Target = target,
            Actions = [action],
        };
    }

    private bool IsPgRoutineSharedActionStart()
    {
        if (_current.Kind == SyntaxKind.SetKeyword)
        {
            return NextEquals(Keyword.Schema) && _index + 1 < _tokens.Count && _tokens[_index + 1].Kind == SyntaxKind.Identifier;
        }

        return IdentifierEquals(Keyword.Rename) || IdentifierEquals(Keyword.PgOwner) || IdentifierEquals(Keyword.PgDepends)
            || (IdentifierEquals(Keyword.No) && NextEquals(Keyword.PgDepends));
    }

    private PgRoutineAttributesAction ParsePgRoutineAttributesAction()
    {
        var start = _current;
        var characteristics = new List<RoutineCharacteristic>();
        while (TryParsePgRoutineAttribute(allowReset: true) is { } attribute)
        {
            characteristics.Add(attribute);
        }

        if (characteristics.Count == 0)
        {
            throw new SqlParseException($"Expected ALTER action, found {_current.Kind}", _current.Position);
        }

        SyntaxToken? restrict = IdentifierEquals(Keyword.Restrict) ? Advance() : null;
        return new PgRoutineAttributesAction
        {
            Span = SourceSpan.From(start, PgPreviousToken()),
            Characteristics = characteristics,
            RestrictKeyword = restrict,
        };
    }
}
