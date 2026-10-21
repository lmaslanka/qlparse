namespace QlParse;

// Building blocks shared by the SQL Server database, object and security statements (workstream F):
// `name [= value] [(nested)]` options and option lists, `class::name` securables, encryption mechanisms, and
// the clause sequences of keys, certificates, credentials and server audits.
internal sealed partial class Parser
{
    // One option: KEYWORDS [= value] [(nested, ...)]. Several words before `=` (`DECRYPTION BY PASSWORD = 'p'`,
    // `ACTIVE FOR BEGIN_DIALOG = ON`) all belong to the option's name; without `=` only the first word does and the
    // next word is its value (`RECOVERY SIMPLE`, `ENCRYPTION ON`).
    internal TsqlObjectOption ParseTsqlObjectOption()
    {
        var keywords = new List<SyntaxToken> { ExpectTsqlWord() };
        if (IsTsqlWordRunThenEquals())
        {
            while (_current.Kind != SyntaxKind.EqualsToken)
            {
                keywords.Add(Advance());
            }
        }

        return FinishTsqlObjectOption(keywords);
    }

    // The `= value` and `(nested, ...)` tail of an option whose keywords are already consumed.
    private TsqlObjectOption FinishTsqlObjectOption(List<SyntaxToken> keywords)
    {
        var equalsToken = _current.Kind == SyntaxKind.EqualsToken ? Advance() : (SyntaxToken?)null;
        var value = ParseTsqlObjectOptionValue(equalsToken is not null);
        var options = _current.Kind == SyntaxKind.OpenParen ? ParseTsqlObjectOptionList() : null;
        if (equalsToken is not null && value.Count == 0 && options is null)
        {
            throw new SqlParseException($"Expected option value, found {_current.Kind}", _current.Position);
        }

        var end = options?.Span ?? (value.Count > 0 ? value[^1].Span : equalsToken?.Span ?? keywords[^1].Span);
        return new TsqlObjectOption
        {
            Span = SourceSpan.From(keywords.Count > 0 ? keywords[0].Span : end, end),
            Keywords = keywords,
            EqualsToken = equalsToken,
            Value = value,
            Options = options,
        };
    }

    // `(option, ...)`
    internal TsqlObjectOptionList ParseTsqlObjectOptionList()
    {
        var openParen = Expect(SyntaxKind.OpenParen);
        var items = ParseTsqlObjectOptionItems();
        var closeParen = Expect(SyntaxKind.CloseParen);
        return new TsqlObjectOptionList
        {
            Span = SourceSpan.From(openParen, closeParen),
            OpenParen = openParen,
            Items = items,
            CloseParen = closeParen,
        };
    }

    // `option, ...` without parentheses, as after WITH.
    internal TsqlObjectOptionList ParseTsqlBareObjectOptionList()
    {
        var items = ParseTsqlObjectOptionItems();
        return new TsqlObjectOptionList { Span = SourceSpan.From(items[0].Span, items[^1].Span), Items = items };
    }

    // Items are comma-separated; `name = value` may also directly follow the previous item, as in
    // `PASSWORD = 'new' OLD_PASSWORD = 'old'`.
    private List<TsqlObjectOption> ParseTsqlObjectOptionItems()
    {
        var items = new List<TsqlObjectOption> { ParseTsqlObjectOption() };
        while (true)
        {
            if (_current.Kind == SyntaxKind.Comma)
            {
                Advance();
                items.Add(ParseTsqlObjectOption());
                continue;
            }

            if (_current.Kind != SyntaxKind.Identifier || NextKind != SyntaxKind.EqualsToken)
            {
                return items;
            }

            items.Add(ParseTsqlObjectOption());
        }
    }

    // A value is one token (anything after `=`; a literal or a non-statement word otherwise), optionally signed,
    // followed by a unit or `%` after a number, HASHED / MUST_CHANGE after a string, or `.name` parts.
    private IReadOnlyList<SyntaxToken> ParseTsqlObjectOptionValue(bool afterEquals)
    {
        if (!IsTsqlObjectOptionValueStart(afterEquals))
        {
            return [];
        }

        var value = new List<SyntaxToken>();
        if (_current.Kind is SyntaxKind.MinusToken or SyntaxKind.PlusToken)
        {
            value.Add(Advance());
        }

        value.Add(Advance());
        while (IsTsqlObjectOptionValueContinuation(value[^1]))
        {
            value.Add(Advance());
        }

        return value;
    }

    private bool IsTsqlObjectOptionValueStart(bool afterEquals)
    {
        if (_current.Kind is SyntaxKind.MinusToken or SyntaxKind.PlusToken)
        {
            return NextKind == SyntaxKind.Number;
        }

        if (afterEquals)
        {
            return _current.Kind is not (SyntaxKind.Comma or SyntaxKind.OpenParen or SyntaxKind.CloseParen
                or SyntaxKind.Semicolon or SyntaxKind.EndOfFile or SyntaxKind.BatchSeparator or SyntaxKind.EqualsToken);
        }

        if (_current.Kind == SyntaxKind.Identifier)
        {
            return NextKind != SyntaxKind.EqualsToken && IsTsqlBareValueWord(_current.TextOf(_source));
        }

        return _current.Kind is SyntaxKind.String or SyntaxKind.Number or SyntaxKind.BinaryLiteral
            or SyntaxKind.MoneyLiteral or SyntaxKind.EmbeddedHost or SyntaxKind.OnKeyword or SyntaxKind.FullKeyword
            or SyntaxKind.AllKeyword or SyntaxKind.NullKeyword or SyntaxKind.TrueKeyword or SyntaxKind.FalseKeyword;
    }

    // A word value without `=` must not be a reserved word that starts the next statement; OFF, DEFAULT, PRIMARY and
    // CURRENT are reserved but are common option values.
    private static bool IsTsqlBareValueWord(ReadOnlySpan<char> text) =>
        !Keyword.IsTsqlReserved(text) || TextEquals(text, Keyword.SecOff) || TextEquals(text, Keyword.Default)
            || TextEquals(text, Keyword.Primary) || TextEquals(text, Keyword.Current);

    private bool IsTsqlObjectOptionValueContinuation(SyntaxToken previous)
    {
        if (previous.Kind == SyntaxKind.Number)
        {
            return _current.Kind == SyntaxKind.PercentToken || IsTsqlValueUnit();
        }

        if (previous.Kind is SyntaxKind.String or SyntaxKind.BinaryLiteral)
        {
            return IdentifierEquals(Keyword.SecHashed) || IdentifierEquals(Keyword.SecMustChange);
        }

        return previous.Kind == SyntaxKind.Identifier
            ? IsTsqlValueWordContinuation(previous)
            : previous.Kind == SyntaxKind.Dot && _current.Kind == SyntaxKind.Identifier;
    }

    // A unit after a number: `10 MB`, `2 DAYS`.
    private bool IsTsqlValueUnit() =>
        _current.Kind == SyntaxKind.Identifier && NextKind != SyntaxKind.EqualsToken && !Keyword.IsTsqlReserved(_current.TextOf(_source));

    // MUST_CHANGE after HASHED, or the `.` of a dotted name.
    private bool IsTsqlValueWordContinuation(SyntaxToken previous)
    {
        if (TokenEquals(previous, Keyword.SecHashed))
        {
            return IdentifierEquals(Keyword.SecMustChange);
        }

        return _current.Kind == SyntaxKind.Dot && NextKind == SyntaxKind.Identifier;
    }

    // [class ::] name — the class is every word before `::` (`OBJECT`, `XML SCHEMA COLLECTION`, `ASYMMETRIC KEY`).
    internal TsqlSecurable ParseTsqlSecurable()
    {
        var classLength = TsqlSecurableClassLength();
        if (classLength == 0)
        {
            var bareName = ParseQualifiedName();
            return new TsqlSecurable { Span = SourceSpan.From(bareName[0], bareName[^1]), Name = bareName };
        }

        var classKeywords = new List<SyntaxToken>(classLength);
        for (var i = 0; i < classLength; i++)
        {
            classKeywords.Add(Advance());
        }

        var colonColon = Advance();
        var name = ParseQualifiedName();
        return new TsqlSecurable
        {
            Span = SourceSpan.From(classKeywords[0], name[^1]),
            ClassKeywords = classKeywords,
            ColonColon = colonColon,
            Name = name,
        };
    }

    private int TsqlSecurableClassLength()
    {
        var index = _index - 1;
        while (index < _tokens.Count && IsTsqlWordToken(_tokens[index]))
        {
            index++;
        }

        var length = index - (_index - 1);
        return length > 0 && index < _tokens.Count && _tokens[index].Kind == SyntaxKind.DoubleColonToken ? length : 0;
    }

    // CERTIFICATE c | [SERVER] ASYMMETRIC KEY k | SYMMETRIC KEY k | SERVER CERTIFICATE c | SERVICE MASTER KEY |
    // PASSWORD = 'p' | PROVIDER p, optionally followed by `WITH PASSWORD = 'p'` or `WITH SIGNATURE = 0x...`, which
    // is kept as the option's nested (unparenthesized) list.
    internal TsqlObjectOption ParseTsqlKeyMechanism()
    {
        var keywords = new List<SyntaxToken> { ExpectTsqlWord() };
        if (TokenEquals(keywords[0], Keyword.SecServer))
        {
            keywords.Add(ExpectTsqlWord());
        }

        var kind = keywords[^1];
        if (TokenEquals(kind, Keyword.SecService))
        {
            keywords.Add(ExpectIdent(Keyword.SecMaster));
            keywords.Add(ExpectIdent(Keyword.Key));
            return FinishTsqlObjectOption(keywords);
        }

        if (TokenEquals(kind, Keyword.Asymmetric) || TokenEquals(kind, Keyword.Symmetric))
        {
            keywords.Add(ExpectIdent(Keyword.Key));
        }

        if (_current.Kind == SyntaxKind.EqualsToken)
        {
            return FinishTsqlObjectOption(keywords);
        }

        var name = Expect(SyntaxKind.Identifier);
        var with = IsTsqlMechanismWith() ? ParseTsqlSingleObjectOptionList() : null;
        return new TsqlObjectOption
        {
            Span = SourceSpan.From(keywords[0], with?.Span ?? name.Span),
            Keywords = keywords,
            Value = [name],
            Options = with,
        };
    }

    private bool IsTsqlMechanismWith() =>
        _current.Kind == SyntaxKind.WithKeyword && (NextEquals(Keyword.SecPassword) || NextEquals(Keyword.SecSignature));

    private TsqlObjectOptionList ParseTsqlSingleObjectOptionList()
    {
        var option = ParseTsqlObjectOption();
        return new TsqlObjectOptionList { Span = option.Span, Items = [option] };
    }

    // mechanism, ...
    private TsqlObjectOptionList ParseTsqlKeyMechanisms()
    {
        var items = new List<TsqlObjectOption> { ParseTsqlKeyMechanism() };
        while (_current.Kind == SyntaxKind.Comma)
        {
            Advance();
            items.Add(ParseTsqlKeyMechanism());
        }

        return new TsqlObjectOptionList { Span = SourceSpan.From(items[0].Span, items[^1].Span), Items = items };
    }

    // Keywords that are already consumed, followed by a single (possibly qualified) name as the value.
    private TsqlObjectOption ParseTsqlNamedClause(List<SyntaxToken> keywords)
    {
        var name = ParseQualifiedName();
        return new TsqlObjectOption
        {
            Span = SourceSpan.From(keywords[0], name[^1]),
            Keywords = keywords,
            Value = name,
        };
    }

    private static TsqlObjectOption TsqlKeywordClause(List<SyntaxToken> keywords, TsqlObjectOptionList? options) => new()
    {
        Span = SourceSpan.From(keywords[0].Span, options?.Span ?? keywords[^1].Span),
        Keywords = keywords,
        Options = options,
    };

    // Statement-level words: identifiers and keyword tokens.
    private static bool IsTsqlWordToken(SyntaxToken token) =>
        token.Kind == SyntaxKind.Identifier || token.Kind is >= SyntaxKind.SelectKeyword and <= SyntaxKind.WidthBucketKeyword;

    private SyntaxToken ExpectTsqlWord()
    {
        if (!IsTsqlWordToken(_current))
        {
            throw new SqlParseException($"Expected option name, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    // The current word and the words after it run straight into `=`.
    private bool IsTsqlWordRunThenEquals()
    {
        if (!IsTsqlWordToken(_current))
        {
            return false;
        }

        var index = _index;
        while (index < _tokens.Count && IsTsqlWordToken(_tokens[index]))
        {
            index++;
        }

        return index < _tokens.Count && _tokens[index].Kind == SyntaxKind.EqualsToken;
    }

    private bool IsTsqlKindAt(int index, string keyword) =>
        index < _tokens.Count && TokenEquals(_tokens[index], keyword);

    // Consumes the verb and the given number of object-kind words.
    private List<SyntaxToken> TakeTsqlWords(int count)
    {
        var words = new List<SyntaxToken>(count);
        for (var i = 0; i < count; i++)
        {
            words.Add(Advance());
        }

        return words;
    }
}
