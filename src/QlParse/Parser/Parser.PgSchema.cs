namespace QlParse;

// Workstream E: Postgres schema objects (types, extensions, foreign data, operators, aggregates,
// collations, text search, replication, event triggers). The hook runs before ISO dispatch for every
// Postgres statement, so it rejects anything that is not CREATE / ALTER / DROP / IMPORT by token kind
// and length before comparing any text, and returns null for forms the ISO grammar already parses.
internal sealed partial class Parser
{
    internal Query? TryParsePgSchemaStatement()
    {
        if (_current.Kind != SyntaxKind.Identifier)
        {
            return null;
        }

        return _current.Length switch
        {
            Len4 => IdentifierEquals(Keyword.Drop) ? TryParsePgSchemaDrop() : null,
            Len5 => IdentifierEquals(Keyword.Alter) ? TryParsePgSchemaAlter() : null,
            Len6 => TryParsePgSchemaLen6(),
            _ => null,
        };
    }

    private Query? TryParsePgSchemaLen6()
    {
        if (IdentifierEquals(Keyword.Create))
        {
            return TryParsePgSchemaCreate();
        }

        return IdentifierEquals(Keyword.PgImport) && NextEquals(Keyword.Foreign) ? ParsePgImportForeignSchema() : null;
    }

    private Query? TryParsePgSchemaCreate()
    {
        var nextKind = NextKind;
        if (nextKind == SyntaxKind.Identifier)
        {
            return TryParsePgCreateObject(_index);
        }

        if (nextKind == SyntaxKind.OrKeyword)
        {
            return IsKeywordAt(_index + 1, Keyword.PgReplace) ? TryParsePgCreateOrReplace(_index + 2) : null;
        }

        return nextKind == SyntaxKind.UserKeyword && IsKeywordAt(_index + 1, Keyword.PgMapping) ? ParsePgUserMapping() : null;
    }

    // CREATE OR REPLACE is shared with views and routines, so only the schema-object kinds are claimed here.
    private Query? TryParsePgCreateOrReplace(int index)
    {
        if (IsKeywordAt(index, Keyword.Trigger)
            || (IsKeywordAt(index, Keyword.Constraint) && IsKeywordAt(index + 1, Keyword.Trigger)))
        {
            return ParseCreateTrigger();
        }

        if (IsKeywordAt(index, Keyword.PgAggregate))
        {
            return ParsePgCreateAggregate();
        }

        return IsPgLanguageAt(index) ? ParsePgCreateLanguage() : null;
    }

    private bool IsPgLanguageAt(int index)
    {
        if (IsKeywordAt(index, Keyword.PgTrusted))
        {
            index++;
        }

        if (IsKeywordAt(index, Keyword.PgProcedural))
        {
            index++;
        }

        return IsKeywordAt(index, Keyword.Language);
    }

    private Query? TryParsePgCreateObject(int index)
    {
        var text = _tokens[index].TextOf(_source);
        return text.Length switch
        {
            Len4 => TryParsePgCreateLen4(text, index),
            Len5 => TextEquals(text, Keyword.PgEvent) && IsKeywordAt(index + 1, Keyword.Trigger) ? ParsePgCreateEventTrigger() : null,
            Len6 => TryParsePgCreateLen6(text, index),
            Len7 => TryParsePgCreateLen7(text, index),
            Len8 => TryParsePgCreateLen8(text),
            Len9 => TryParsePgCreateLen9(text, index),
            Len10 => TryParsePgCreateLen10(text, index),
            Len11 => TextEquals(text, Keyword.PgPublication) ? ParsePgCreatePublication() : null,
            Len12 => TextEquals(text, Keyword.PgSubscription) ? ParsePgCreateSubscription() : null,
            _ => null,
        };
    }

    private Query? TryParsePgCreateLen4(ReadOnlySpan<char> text, int index)
    {
        if (TextEquals(text, Keyword.Type))
        {
            return IsPgCreateTypeForm(index + 1) ? ParsePgCreateType() : null;
        }

        return TextEquals(text, Keyword.PgText) && IsPgSearchAt(index + 1) ? ParsePgCreateTextSearch() : null;
    }

    private Query? TryParsePgCreateLen6(ReadOnlySpan<char> text, int index)
    {
        if (TextEquals(text, Keyword.PgServer))
        {
            return ParsePgCreateServer();
        }

        return TextEquals(text, Keyword.PgAccess) && IsKeywordAt(index + 1, Keyword.Method) ? ParsePgCreateAccessMethod() : null;
    }

    private Query? TryParsePgCreateLen7(ReadOnlySpan<char> text, int index)
    {
        if (TextEquals(text, Keyword.Foreign))
        {
            return TryParsePgCreateForeign(index + 1);
        }

        if (TextEquals(text, Keyword.PgTrusted))
        {
            return IsPgLanguageAt(index) ? ParsePgCreateLanguage() : null;
        }

        return TextEquals(text, Keyword.Default) && IsKeywordAt(index + 1, Keyword.PgConversion) ? ParsePgCreateConversion() : null;
    }

    private Query? TryParsePgCreateForeign(int index)
    {
        if (IsKeywordAt(index, Keyword.Data) && IsKeywordAt(index + 1, Keyword.Wrapper))
        {
            return ParsePgCreateForeignDataWrapper();
        }

        return IsKeywordAt(index, Keyword.Table) ? ParsePgCreateForeignTable() : null;
    }

    private Query? TryParsePgCreateLen8(ReadOnlySpan<char> text)
    {
        if (TextEquals(text, Keyword.Operator))
        {
            return ParsePgCreateOperatorAny();
        }

        return TextEquals(text, Keyword.Language) ? ParsePgCreateLanguage() : null;
    }

    private Query? TryParsePgCreateLen9(ReadOnlySpan<char> text, int index)
    {
        if (TextEquals(text, Keyword.PgExtension))
        {
            return ParsePgCreateExtension();
        }

        if (TextEquals(text, Keyword.PgAggregate))
        {
            return ParsePgCreateAggregate();
        }

        return TextEquals(text, Keyword.Collation) && IsPgCreateCollationForm(index + 1) ? ParsePgCreateCollation() : null;
    }

    private Query? TryParsePgCreateLen10(ReadOnlySpan<char> text, int index)
    {
        if (TextEquals(text, Keyword.PgProcedural))
        {
            return IsPgLanguageAt(index) ? ParsePgCreateLanguage() : null;
        }

        if (TextEquals(text, Keyword.PgConversion))
        {
            return ParsePgCreateConversion();
        }

        if (TextEquals(text, Keyword.PgStatistics))
        {
            return ParsePgCreateStatistics();
        }

        return TextEquals(text, Keyword.Constraint) && IsKeywordAt(index + 1, Keyword.Trigger) ? ParseCreateTrigger() : null;
    }

    // ISO owns CREATE TYPE ... AS <type> / AS (...) / UNDER; Postgres adds the shell, base, ENUM and RANGE forms.
    private bool IsPgCreateTypeForm(int index)
    {
        index = SkipQualifiedNameAt(index);
        if (index >= _tokens.Count)
        {
            return false;
        }

        var kind = _tokens[index].Kind;
        if (kind is SyntaxKind.EndOfFile or SyntaxKind.Semicolon or SyntaxKind.OpenParen)
        {
            return true;
        }

        return kind == SyntaxKind.AsKeyword
            && (IsKeywordAt(index + 1, Keyword.PgEnum) || IsKeywordAt(index + 1, Keyword.Range));
    }

    // ISO owns CREATE COLLATION name FOR charset FROM source; anything else is the Postgres form.
    private bool IsPgCreateCollationForm(int index)
    {
        if (IsKeywordAt(index, Keyword.If))
        {
            return true;
        }

        index = SkipQualifiedNameAt(index);
        return index < _tokens.Count && _tokens[index].Kind != SyntaxKind.ForKeyword;
    }

    // SEARCH is a reserved keyword token (SEARCH DEPTH FIRST), so TEXT SEARCH is matched by kind.
    internal bool IsPgSearchAt(int index) => index < _tokens.Count && _tokens[index].Kind == SyntaxKind.SearchKeyword;

    private int SkipQualifiedNameAt(int index)
    {
        if (index >= _tokens.Count || _tokens[index].Kind != SyntaxKind.Identifier)
        {
            return index;
        }

        index++;
        while (index + 1 < _tokens.Count && _tokens[index].Kind == SyntaxKind.Dot && _tokens[index + 1].Kind == SyntaxKind.Identifier)
        {
            index += Len2;
        }

        return index;
    }

    private Query? TryParsePgSchemaDrop()
    {
        if (NextKind == SyntaxKind.UserKeyword)
        {
            return IsKeywordAt(_index + 1, Keyword.PgMapping) ? ParsePgUserMapping() : null;
        }

        var kind = MatchPgObjectKind(_index);
        if (kind.Shape is PgObjectShape.None or PgObjectShape.Trigger)
        {
            return null;
        }

        return kind.HasIsoDrop && IsIsoDropTail(_index + kind.Count) ? null : ParsePgDrop(kind);
    }

    // DROP TYPE / DOMAIN / COLLATION name {CASCADE|RESTRICT} is the ISO statement; keep its node.
    private bool IsIsoDropTail(int index)
    {
        var afterName = SkipQualifiedNameAt(index);
        if (afterName == index || afterName + 1 >= _tokens.Count)
        {
            return false;
        }

        var behavior = _tokens[afterName];
        return (TokenEquals(behavior, Keyword.Cascade) || TokenEquals(behavior, Keyword.Restrict))
            && _tokens[afterName + 1].Kind is SyntaxKind.EndOfFile or SyntaxKind.Semicolon;
    }

    private Query? TryParsePgSchemaAlter()
    {
        if (NextKind == SyntaxKind.UserKeyword)
        {
            return IsKeywordAt(_index + 1, Keyword.PgMapping) ? ParsePgUserMapping() : null;
        }

        var kind = MatchPgObjectKind(_index);
        return kind.Shape is PgObjectShape.None or PgObjectShape.Cast || kind.HasIsoAlter ? null : ParsePgAlter(kind);
    }
}
