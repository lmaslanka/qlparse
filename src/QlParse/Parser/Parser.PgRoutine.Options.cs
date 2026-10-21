namespace QlParse;

// Postgres routine attributes, shared by CREATE and ALTER FUNCTION / PROCEDURE. Every attribute is a
// RoutineCharacteristic: Name is its first word and Tail the remaining words, so IMMUTABLE, PARALLEL SAFE,
// COST 100, SECURITY DEFINER and LANGUAGE plpgsql all share one shape. SET keeps its values in Values
// and TRANSFORM its types in Types.
internal sealed partial class Parser
{
    private readonly record struct PgRoutineOptions(
        List<RoutineCharacteristic> Characteristics,
        SyntaxToken? AsKeyword,
        SyntaxToken? Definition,
        SyntaxToken? LinkSymbol);

    private PgRoutineOptions ParsePgRoutineOptions()
    {
        var characteristics = new List<RoutineCharacteristic>();
        SyntaxToken? asKeyword = null;
        SyntaxToken? definition = null;
        SyntaxToken? linkSymbol = null;
        while (true)
        {
            if (_current.Kind == SyntaxKind.AsKeyword && asKeyword is null)
            {
                asKeyword = Advance();
                definition = Expect(SyntaxKind.String);
                linkSymbol = TryParsePgLinkSymbol();
                continue;
            }

            if (TryParsePgRoutineAttribute(allowReset: false) is not { } attribute)
            {
                break;
            }

            characteristics.Add(attribute);
        }

        return new PgRoutineOptions(characteristics, asKeyword, definition, linkSymbol);
    }

    // AS 'obj_file', 'link_symbol' for C functions.
    private SyntaxToken? TryParsePgLinkSymbol()
    {
        if (_current.Kind != SyntaxKind.Comma)
        {
            return null;
        }

        Advance();
        return Expect(SyntaxKind.String);
    }

    private RoutineCharacteristic? TryParsePgRoutineAttribute(bool allowReset) =>
        TryParsePgRoutineFlag() ?? TryParsePgRoutineValueAttribute() ?? TryParsePgRoutineClauseAttribute(allowReset)
        ?? TryParseIsoRoutineCharacteristic();

    // The ISO characteristics (DETERMINISTIC, NO SQL, PARAMETER STYLE, SPECIFIC name, ...) stay accepted
    // under Postgres, so ISO routine definitions keep parsing when the Postgres flag is set.
    private RoutineCharacteristic? TryParseIsoRoutineCharacteristic()
    {
        if (!IdentifierEquals(Keyword.Specific))
        {
            return IsCharacteristicStart() ? ParseCharacteristic() : null;
        }

        var specific = Advance();
        var specificName = ParseQualifiedName();
        return new RoutineCharacteristic { Span = SourceSpan.From(specific, specificName[^1]), Name = specific, Tail = specificName };
    }

    // IMMUTABLE | STABLE | VOLATILE | STRICT | [NOT] LEAKPROOF | WINDOW
    private RoutineCharacteristic? TryParsePgRoutineFlag()
    {
        if (_current.Kind == SyntaxKind.NotKeyword && NextEquals(Keyword.RoutineLeakproof))
        {
            var notKeyword = Advance();
            var leakproof = Advance();
            return new RoutineCharacteristic { Span = SourceSpan.From(notKeyword, leakproof), NotKeyword = notKeyword, Name = leakproof };
        }

        if (_current.Kind == SyntaxKind.WindowKeyword || IsPgRoutineFlagWord())
        {
            var name = Advance();
            return new RoutineCharacteristic { Span = name.Span, Name = name };
        }

        return null;
    }

    private bool IsPgRoutineFlagWord() =>
        IdentifierEquals(Keyword.RoutineImmutable) || IdentifierEquals(Keyword.RoutineStable)
        || IdentifierEquals(Keyword.RoutineVolatile) || IdentifierEquals(Keyword.RoutineStrict)
        || IdentifierEquals(Keyword.RoutineLeakproof);

    // [EXTERNAL] SECURITY {DEFINER | INVOKER} | PARALLEL {SAFE | RESTRICTED | UNSAFE} | COST n | ROWS n | SUPPORT name
    private RoutineCharacteristic? TryParsePgRoutineValueAttribute()
    {
        if (IdentifierEquals(Keyword.UtilSecurity) || (IdentifierEquals(Keyword.External) && NextEquals(Keyword.UtilSecurity)))
        {
            var name = Advance();
            List<SyntaxToken> tail = TokenEquals(name, Keyword.External) ? [Advance()] : [];
            tail.Add(ExpectPgRoutineWord(Keyword.RoutineDefiner, Keyword.RoutineInvoker));
            return new RoutineCharacteristic { Span = SourceSpan.From(name, tail[^1]), Name = name, Tail = tail };
        }

        if (IdentifierEquals(Keyword.RoutineParallel))
        {
            var name = Advance();
            var level = IdentifierEquals(Keyword.RoutineRestricted) ? Advance() : ExpectPgRoutineWord(Keyword.RoutineSafe, Keyword.RoutineUnsafe);
            return new RoutineCharacteristic { Span = SourceSpan.From(name, level), Name = name, Tail = [level] };
        }

        if (IdentifierEquals(Keyword.RoutineCost) || IdentifierEquals(Keyword.Rows))
        {
            var name = Advance();
            var value = Expect(SyntaxKind.Number);
            return new RoutineCharacteristic { Span = SourceSpan.From(name, value), Name = name, Tail = [value] };
        }

        if (IdentifierEquals(Keyword.RoutineSupport))
        {
            var name = Advance();
            var function = ParseQualifiedName();
            return new RoutineCharacteristic { Span = SourceSpan.From(name, function[^1]), Name = name, Tail = function };
        }

        return null;
    }

    private SyntaxToken ExpectPgRoutineWord(string first, string second)
    {
        if (!IdentifierEquals(first) && !IdentifierEquals(second))
        {
            throw new SqlParseException($"Expected {first.ToUpperInvariant()} or {second.ToUpperInvariant()}, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    // LANGUAGE name | TRANSFORM FOR TYPE t [, ...] | CALLED ON NULL INPUT | RETURNS NULL ON NULL INPUT
    // | SET param {TO | =} value | SET param FROM CURRENT | RESET {param | ALL} (ALTER only)
    private RoutineCharacteristic? TryParsePgRoutineClauseAttribute(bool allowReset)
    {
        if (IdentifierEquals(Keyword.Language))
        {
            var name = Advance();
            var language = ExpectPgNameOrString();
            return new RoutineCharacteristic { Span = SourceSpan.From(name, language), Name = name, Tail = [language] };
        }

        if (IdentifierEquals(Keyword.Transform) && NextKind == SyntaxKind.ForKeyword)
        {
            return ParsePgRoutineTransform();
        }

        if (IdentifierEquals(Keyword.Called) || (IdentifierEquals(Keyword.Returns) && NextKind == SyntaxKind.NullKeyword))
        {
            return ParseSqlDataCharacteristic();
        }

        if (_current.Kind == SyntaxKind.SetKeyword)
        {
            return ParsePgRoutineSet();
        }

        return allowReset && IdentifierEquals(Keyword.PgReset) ? ParsePgRoutineReset() : null;
    }

    private RoutineCharacteristic ParsePgRoutineTransform()
    {
        var name = Advance();
        var tail = new List<SyntaxToken>();
        var types = new List<DataType>();
        do
        {
            if (types.Count > 0)
            {
                Advance();
            }

            tail.Add(Expect(SyntaxKind.ForKeyword));
            tail.Add(ExpectIdent(Keyword.Type));
            types.Add(ParseDataType());
        }
        while (_current.Kind == SyntaxKind.Comma);

        return new RoutineCharacteristic { Span = SourceSpan.From(name, types[^1].Span), Name = name, Tail = tail, Types = types };
    }

    private RoutineCharacteristic ParsePgRoutineSet()
    {
        var setKeyword = Advance();
        var tail = ParseQualifiedName();
        if (_current.Kind == SyntaxKind.FromKeyword && NextEquals(Keyword.Current))
        {
            tail.Add(Advance());
            tail.Add(Advance());
            return new RoutineCharacteristic { Span = SourceSpan.From(setKeyword, tail[^1]), Name = setKeyword, Tail = tail };
        }

        tail.Add(_current.Kind == SyntaxKind.EqualsToken ? Advance() : ExpectIdent(Keyword.To));
        var values = ParseConfigurationValues();
        if (values.DefaultKeyword is { } defaultKeyword)
        {
            tail.Add(defaultKeyword);
        }

        return new RoutineCharacteristic
        {
            Span = SourceSpan.From(setKeyword, values.End),
            Name = setKeyword,
            Tail = tail,
            Values = values.Values,
        };
    }

    private RoutineCharacteristic ParsePgRoutineReset()
    {
        var resetKeyword = Advance();
        List<SyntaxToken> tail = _current.Kind == SyntaxKind.AllKeyword ? [Advance()] : ParseQualifiedName();
        return new RoutineCharacteristic { Span = SourceSpan.From(resetKeyword, tail[^1]), Name = resetKeyword, Tail = tail };
    }
}
