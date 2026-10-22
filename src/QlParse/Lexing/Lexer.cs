namespace QlParse;

internal sealed partial class Lexer
{
    private const int CommentDelimiterLength = 2;
    private const int TwoCharTokenLength = 2;
    private const int ThreeCharTokenLength = 3;
    private const int UnicodeDelimitedPrefixLength = 2;
    private const int CharsPerTokenEstimate = 4;
    private const char StringQuote = '\'';
    private const char IdentifierQuote = '"';
    private const char Ampersand = '&';
    private const char Introducer = '_';
    private const char AtSign = '@';
    private const int IntroducerNameMinLength = 2;

    private readonly string _source;
    private readonly int _length;
    private readonly SqlOptions _flags;
    private readonly List<SyntaxTrivia> _trivia = [];
    private int _position;

    public Lexer(string source, SqlOptions flags = SqlOptions.None)
    {
        _source = source;
        _length = source.Length;
        _flags = flags;
    }

    // Lexes only source[start..end): token and trivia positions stay absolute in the full source and
    // nothing is copied. `_length` doubles as the end bound, so the regular path pays nothing for this.
    private Lexer(string source, int start, int end, SqlOptions flags)
    {
        _source = source;
        _length = end;
        _flags = flags;
        SeekTo(start);
    }

    private void SeekTo(int start)
    {
        _position = start;
    }

    // Lexes a sub-range (a routine body inside a dollar-quoted string). The trivia list is the
    // sub-lexer's own: the tokens' LeadingTriviaStart indices refer to it, never to the outer list.
    internal static (List<SyntaxToken> Tokens, IReadOnlyList<SyntaxTrivia> Trivia) LexRange(
        string source, int start, int end, SqlOptions flags)
    {
        var lexer = new Lexer(source, start, end, flags);
        var tokens = new List<SyntaxToken>(Math.Max(1, (end - start) / CharsPerTokenEstimate));
        if (lexer.IsSqlServer)
        {
            lexer.ReadTsqlTokens(tokens);
            return (tokens, lexer.Trivia);
        }

        SyntaxToken token;
        do
        {
            token = lexer.NextToken();
            tokens.Add(token);
        }
        while (token.Kind != SyntaxKind.EndOfFile);

        return (tokens, lexer.Trivia);
    }

    public IReadOnlyList<SyntaxTrivia> Trivia => _trivia;

    public static SqlLexResult LexAll(string source, SqlOptions flags = SqlOptions.None)
    {
        var lexer = new Lexer(source, flags);
        var tokens = new List<SyntaxToken>(Math.Max(1, source.Length / CharsPerTokenEstimate));
        try
        {
            // SQL Server runs its own token loop (Lexer.TsqlLexical.cs), so this one, shared by ISO and
            // Postgres, carries no T-SQL check per token.
            if (lexer.IsSqlServer)
            {
                lexer.ReadTsqlTokens(tokens);
            }
            else
            {
                SyntaxToken token;
                do
                {
                    token = lexer.NextToken();
                    tokens.Add(token);
                }
                while (token.Kind != SyntaxKind.EndOfFile);
            }

            return new SqlLexResult
            {
                Source = source,
                Options = flags,
                Tokens = tokens,
                Trivia = lexer.Trivia,
                Error = null,
            };
        }
        catch (SqlParseException ex)
        {
            return new SqlLexResult
            {
                Source = source,
                Options = flags,
                Tokens = tokens,
                Trivia = lexer.Trivia,
                Error = ex,
            };
        }
    }

    public SyntaxToken NextToken()
    {
        var triviaStart = _trivia.Count;
        ReadTrivia();
        var triviaCount = _trivia.Count - triviaStart;
        return TryReadSpecialToken(triviaStart, triviaCount) ?? ReadPunctuationToken(triviaStart, triviaCount);
    }

    private SyntaxToken? TryReadSpecialToken(int triviaStart, int triviaCount)
    {
        if (_position >= _length)
        {
            return new SyntaxToken(SyntaxKind.EndOfFile, _position, 0, triviaStart, triviaCount);
        }

        var ch = _source[_position];
        if (IsIdentifierStart(ch))
        {
            return Peek() is StringQuote or Ampersand
                ? ReadQuotePrefixedWord(ch, triviaStart, triviaCount)
                : ReadIdentifierOrKeyword(triviaStart, triviaCount);
        }

        if (ch == '$' && (_flags & SqlOptions.Postgres) != 0)
        {
            var dollarQuoted = char.IsAsciiDigit(Peek())
                ? ReadPositionalParameter(triviaStart, triviaCount)
                : TryReadDollarQuotedString(triviaStart, triviaCount);
            if (dollarQuoted is not null)
            {
                return dollarQuoted;
            }
        }

        if (char.IsAsciiDigit(ch))
        {
            return ReadNumber(triviaStart, triviaCount);
        }

        return null;
    }

    // A word directly followed by a quote or an ampersand, where the escape, national, binary and hex string
    // prefixes and the Unicode (U&) forms start. Every other such word is an ordinary word.
    private SyntaxToken ReadQuotePrefixedWord(char ch, int triviaStart, int triviaCount)
    {
        if (IsEscapeStringStart(ch))
        {
            return ReadEscapeString(triviaStart, triviaCount);
        }

        if (IsPrefixedStringStart(ch))
        {
            return ReadString(triviaStart, triviaCount, prefixed: true);
        }

        if (IsUnicodeDelimitedIdentifierStart(ch))
        {
            return ReadUnicodeDelimitedIdentifier(triviaStart, triviaCount);
        }

        return IsUnicodeStringStart(ch)
            ? ReadUnicodeString(triviaStart, triviaCount)
            : ReadIdentifierOrKeyword(triviaStart, triviaCount);
    }

    private SyntaxToken ReadPunctuationToken(int triviaStart, int triviaCount)
    {
        var ch = _source[_position];
        return TryReadPunctuationA(ch, triviaStart, triviaCount)
            ?? TryReadPunctuationB(ch, triviaStart, triviaCount)
            ?? TryReadPunctuationC(ch, triviaStart, triviaCount)
            ?? TryReadPunctuationD(ch, triviaStart, triviaCount)
            ?? TryReadPunctuationE(ch, triviaStart, triviaCount)
            ?? TryReadPgOnlyOperator(ch, triviaStart, triviaCount)
            ?? throw new SqlParseException($"Unexpected character '{ch}'", _position);
    }

    private SyntaxToken? TryReadPunctuationA(char ch, int triviaStart, int triviaCount) => ch switch
    {
        ',' => ReadSingle(SyntaxKind.Comma, triviaStart, triviaCount),
        '=' when IsPostgres => ReadPgOperator(triviaStart, triviaCount),
        '=' => ReadSingle(SyntaxKind.EqualsToken, triviaStart, triviaCount),
        '.' => ReadSingle(SyntaxKind.Dot, triviaStart, triviaCount),
        ';' => ReadSingle(SyntaxKind.Semicolon, triviaStart, triviaCount),
        ':' => ReadColon(triviaStart, triviaCount),
        '|' when IsPostgres => ReadPgOperator(triviaStart, triviaCount),
        '|' => Peek() == '|'
            ? ReadTwo(SyntaxKind.ConcatToken, triviaStart, triviaCount)
            : ReadSingle(SyntaxKind.BarToken, triviaStart, triviaCount),
        _ => null,
    };

    private SyntaxToken? TryReadPunctuationB(char ch, int triviaStart, int triviaCount) => ch switch
    {
        '{' => ReadSingle(SyntaxKind.OpenBrace, triviaStart, triviaCount),
        '}' => ReadSingle(SyntaxKind.CloseBrace, triviaStart, triviaCount),
        '^' when IsPostgres => ReadPgOperator(triviaStart, triviaCount),
        '^' => ReadSingle(SyntaxKind.CaretToken, triviaStart, triviaCount),
        '$' => ReadSingle(SyntaxKind.DollarToken, triviaStart, triviaCount),
        '(' => ReadSingle(SyntaxKind.OpenParen, triviaStart, triviaCount),
        ')' => ReadSingle(SyntaxKind.CloseParen, triviaStart, triviaCount),
        _ => null,
    };

    private SyntaxToken? TryReadPunctuationC(char ch, int triviaStart, int triviaCount) => ch switch
    {
        '[' => ReadSingle(SyntaxKind.OpenBracket, triviaStart, triviaCount),
        ']' => ReadSingle(SyntaxKind.CloseBracket, triviaStart, triviaCount),
        '>' when IsPostgres => ReadPgOperator(triviaStart, triviaCount),
        '>' => Peek() == '='
            ? ReadTwo(SyntaxKind.GreaterOrEqual, triviaStart, triviaCount)
            : ReadSingle(SyntaxKind.GreaterThan, triviaStart, triviaCount),
        '<' when IsPostgres => ReadPgOperator(triviaStart, triviaCount),
        '<' => ReadLessThan(triviaStart, triviaCount),
        '!' when IsPostgres => ReadPgOperator(triviaStart, triviaCount),
        '!' => Peek() == '='
            ? ReadTwo(SyntaxKind.NotEqualsToken, triviaStart, triviaCount)
            : throw new SqlParseException("Unexpected character '!'", _position),
        _ => null,
    };

    private SyntaxToken? TryReadPunctuationD(char ch, int triviaStart, int triviaCount) => ch switch
    {
        '*' or '+' or '-' or '/' or '?' when IsPostgres => ReadPgOperator(triviaStart, triviaCount),
        '*' => ReadSingle(SyntaxKind.Star, triviaStart, triviaCount),
        '+' => ReadSingle(SyntaxKind.PlusToken, triviaStart, triviaCount),
        '-' => ReadMinus(triviaStart, triviaCount),
        '/' => ReadSingle(SyntaxKind.SlashToken, triviaStart, triviaCount),
        '?' => ReadSingle(SyntaxKind.QuestionMark, triviaStart, triviaCount),
        _ => null,
    };

    private SyntaxToken? TryReadPunctuationE(char ch, int triviaStart, int triviaCount) => ch switch
    {
        AtSign when (_flags & SqlOptions.AtParameters) != 0 && IsIdentifierStart(Peek())
            => ReadEmbeddedHost(triviaStart, triviaCount),
        AtSign when IsPostgres => ReadPgOperator(triviaStart, triviaCount),
        AtSign => throw new SqlParseException($"Unexpected character '{AtSign}'", _position),
        StringQuote => ReadString(triviaStart, triviaCount, prefixed: false),
        IdentifierQuote => ReadQuotedIdentifier(triviaStart, triviaCount),
        _ => null,
    };

    private void ReadTrivia()
    {
        while (_position < _length)
        {
            var ch = _source[_position];
            if (IsWhitespace(ch))
            {
                SkipWhitespace();
                continue;
            }

            if (ch == '-' && Peek() == '-')
            {
                _trivia.Add(ReadLineComment());
                continue;
            }

            if (ch == '/' && Peek() == '*')
            {
                _trivia.Add(ReadBlockComment());
                continue;
            }

            break;
        }
    }

    private void SkipWhitespace()
    {
        while (_position < _length && IsWhitespace(_source[_position]))
        {
            _position++;
        }
    }

    private SyntaxTrivia ReadLineComment()
    {
        var start = _position;
        _position += CommentDelimiterLength;
        while (_position < _length && _source[_position] is not '\n' and not '\r')
        {
            _position++;
        }

        return new SyntaxTrivia(SyntaxKind.LineCommentTrivia, start, _position - start);
    }

    private SyntaxTrivia ReadBlockComment()
    {
        var start = _position;
        _position += CommentDelimiterLength;
        var depth = 1;
        while (_position < _length && depth > 0)
        {
            if (_source[_position] == '/' && Peek() == '*')
            {
                depth++;
                _position += CommentDelimiterLength;
                continue;
            }

            if (_source[_position] == '*' && Peek() == '/')
            {
                depth--;
                _position += CommentDelimiterLength;
                continue;
            }

            _position++;
        }

        if (depth != 0)
        {
            throw new SqlParseException("Unterminated block comment", start);
        }

        return new SyntaxTrivia(SyntaxKind.BlockCommentTrivia, start, _position - start);
    }

    private SyntaxToken ReadIdentifierOrKeyword(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position = SkipIdentifierPart(start + 1);
        var text = _source.AsSpan(start, _position - start);
        if (IsCharacterSetIntroducer(text))
        {
            if (_position < _length && _source[_position] == IdentifierQuote)
            {
                return ReadQuotedIdentifier(triviaStart, triviaCount, start);
            }

            if (_position < _length && _source[_position] == StringQuote)
            {
                return ReadString(triviaStart, triviaCount, start);
            }
        }

        return new SyntaxToken(Keyword.Classify(text), start, text.Length, triviaStart, triviaCount);
    }

    // Scans over a span of the lexed range, so the loop keeps its index in a register and needs no bounds check.
    private int SkipIdentifierPart(int position)
    {
        var source = _source.AsSpan(0, _length);
        var end = position;
        while (end < source.Length && IsIdentifierPart(source[end]))
        {
            end++;
        }

        return end;
    }

    private SyntaxToken ReadNumber(int triviaStart, int triviaCount)
    {
        var start = _position;
        while (_position < _length && char.IsAsciiDigit(_source[_position]))
        {
            _position++;
        }

        if (_position < _length
            && _source[_position] == '.'
            && _position + 1 < _length
            && char.IsAsciiDigit(_source[_position + 1]))
        {
            _position++;
            while (_position < _length && char.IsAsciiDigit(_source[_position]))
            {
                _position++;
            }
        }

        TryReadExponent();
        return new SyntaxToken(SyntaxKind.Number, start, _position - start, triviaStart, triviaCount);
    }

    private void TryReadExponent()
    {
        if (_position >= _length || _source[_position] is not 'E' and not 'e')
        {
            return;
        }

        var look = _position + 1;
        if (look < _length && _source[look] is '+' or '-')
        {
            look++;
        }

        if (look >= _length || !char.IsAsciiDigit(_source[look]))
        {
            return;
        }

        _position = look;
        while (_position < _length && char.IsAsciiDigit(_source[_position]))
        {
            _position++;
        }
    }

    private SyntaxToken ReadColon(int triviaStart, int triviaCount)
    {
        if (Peek() == ':')
        {
            return ReadTwo(SyntaxKind.DoubleColonToken, triviaStart, triviaCount);
        }

        if (IsPostgres)
        {
            return Peek() == '='
                ? ReadTwo(SyntaxKind.ColonEqualsToken, triviaStart, triviaCount)
                : ReadSingle(SyntaxKind.ColonToken, triviaStart, triviaCount);
        }

        if (IsIdentifierStart(Peek()))
        {
            return ReadEmbeddedHost(triviaStart, triviaCount);
        }

        return ReadSingle(SyntaxKind.ColonToken, triviaStart, triviaCount);
    }

    private SyntaxToken ReadLessThan(int triviaStart, int triviaCount)
    {
        if (Peek() == '=')
        {
            return ReadTwo(SyntaxKind.LessOrEqual, triviaStart, triviaCount);
        }

        if (Peek() == '>')
        {
            return ReadTwo(SyntaxKind.NotEqualsToken, triviaStart, triviaCount);
        }

        return ReadSingle(SyntaxKind.LessThan, triviaStart, triviaCount);
    }

    private SyntaxToken ReadMinus(int triviaStart, int triviaCount)
    {
        if (Peek() != '>')
        {
            return ReadSingle(SyntaxKind.MinusToken, triviaStart, triviaCount);
        }

        if (PeekTwo() == '>')
        {
            return ReadThree(SyntaxKind.JsonTextArrowToken, triviaStart, triviaCount);
        }

        return ReadTwo(SyntaxKind.JsonArrowToken, triviaStart, triviaCount);
    }

    // Postgres operator characters that ISO mode rejects outright; reached only after every ISO
    // punctuation table has declined the character, so ISO pays nothing for this table.
    private SyntaxToken? TryReadPgOnlyOperator(char ch, int triviaStart, int triviaCount) => ch switch
    {
        '~' or '#' or '%' or '&' or '`' when IsPostgres => ReadPgOperator(triviaStart, triviaCount),
        _ => null,
    };

    // Postgres operator lexing: the longest run of operator characters, stopping at a comment start.
    // A multi-character operator may not end in '+' or '-' unless it also holds one of ~ ! @ # % ^ & | ` ?,
    // so "=-" lexes as "=" followed by "-" (scan.l). Well-known spellings keep their ISO token kinds.
    private SyntaxToken ReadPgOperator(int triviaStart, int triviaCount)
    {
        var start = _position;
        var length = TrimPgOperatorLength(start, ScanPgOperatorEnd(start) - start);
        _position = start + length;
        var kind = ClassifyPgOperator(_source.AsSpan(start, length));
        return new SyntaxToken(kind, start, length, triviaStart, triviaCount);
    }

    private int ScanPgOperatorEnd(int start)
    {
        var end = start + 1;
        while (end < _length && IsPgOperatorChar(_source[end]) && !IsCommentStartAt(end))
        {
            end++;
        }

        return end;
    }

    private bool IsCommentStartAt(int index)
    {
        if (index + 1 >= _length)
        {
            return false;
        }

        var ch = _source[index];
        var next = _source[index + 1];
        return (ch == '-' && next == '-') || (ch == '/' && next == '*');
    }

    private int TrimPgOperatorLength(int start, int length)
    {
        if (length == 1 || !IsPlusOrMinus(_source[start + length - 1]) || ContainsPgSpecialChar(start, length))
        {
            return length;
        }

        var trimmed = length;
        while (trimmed > 1 && IsPlusOrMinus(_source[start + trimmed - 1]))
        {
            trimmed--;
        }

        return trimmed;
    }

    private bool ContainsPgSpecialChar(int start, int length)
    {
        var end = start + length;
        for (var index = start; index < end; index++)
        {
            if (IsPgSpecialOperatorChar(_source[index]))
            {
                return true;
            }
        }

        return false;
    }

    private static SyntaxKind ClassifyPgOperator(ReadOnlySpan<char> text) => text.Length switch
    {
        1 => ClassifyPgSingleOperator(text[0]),
        TwoCharTokenLength => ClassifyPgDoubleOperator(text[0], text[1]),
        ThreeCharTokenLength when text is "->>" => SyntaxKind.JsonTextArrowToken,
        _ => SyntaxKind.OperatorToken,
    };

    private static SyntaxKind ClassifyPgSingleOperator(char ch) => ch switch
    {
        '+' => SyntaxKind.PlusToken,
        '-' => SyntaxKind.MinusToken,
        '*' => SyntaxKind.Star,
        '/' => SyntaxKind.SlashToken,
        '<' => SyntaxKind.LessThan,
        '>' => SyntaxKind.GreaterThan,
        '=' => SyntaxKind.EqualsToken,
        '^' => SyntaxKind.CaretToken,
        '|' => SyntaxKind.BarToken,
        '?' => SyntaxKind.QuestionMark,
        '%' => SyntaxKind.PercentToken,
        _ => SyntaxKind.OperatorToken,
    };

    private static SyntaxKind ClassifyPgDoubleOperator(char first, char second) => (first, second) switch
    {
        ('<', '=') => SyntaxKind.LessOrEqual,
        ('>', '=') => SyntaxKind.GreaterOrEqual,
        ('<', '>') or ('!', '=') => SyntaxKind.NotEqualsToken,
        ('|', '|') => SyntaxKind.ConcatToken,
        ('-', '>') => SyntaxKind.JsonArrowToken,
        ('=', '>') => SyntaxKind.EqualsGreaterToken,
        _ => SyntaxKind.OperatorToken,
    };

    private SyntaxToken ReadPositionalParameter(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position++;
        while (_position < _length && char.IsAsciiDigit(_source[_position]))
        {
            _position++;
        }

        return new SyntaxToken(SyntaxKind.PositionalParameter, start, _position - start, triviaStart, triviaCount);
    }

    private SyntaxToken ReadSingle(SyntaxKind kind, int triviaStart, int triviaCount)
    {
        var start = _position;
        _position++;
        return new SyntaxToken(kind, start, _position - start, triviaStart, triviaCount);
    }

    private SyntaxToken ReadTwo(SyntaxKind kind, int triviaStart, int triviaCount)
    {
        var start = _position;
        _position += TwoCharTokenLength;
        return new SyntaxToken(kind, start, TwoCharTokenLength, triviaStart, triviaCount);
    }

    private SyntaxToken ReadThree(SyntaxKind kind, int triviaStart, int triviaCount)
    {
        var start = _position;
        _position += ThreeCharTokenLength;
        return new SyntaxToken(kind, start, ThreeCharTokenLength, triviaStart, triviaCount);
    }

    private SyntaxToken ReadEmbeddedHost(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position++;
        while (_position < _length && IsIdentifierPart(_source[_position]))
        {
            _position++;
        }

        return new SyntaxToken(SyntaxKind.EmbeddedHost, start, _position - start, triviaStart, triviaCount);
    }

    private bool IsPrefixedStringStart(char ch) =>
        (ch is 'N' or 'n' or 'B' or 'b' or 'X' or 'x') && Peek() == StringQuote;

    private bool IsUnicodeDelimitedIdentifierStart(char ch) =>
        (ch is 'U' or 'u') && Peek() == Ampersand && PeekTwo() == IdentifierQuote;

    private bool IsUnicodeStringStart(char ch) =>
        (ch is 'U' or 'u') && Peek() == Ampersand && PeekTwo() == StringQuote;

    private SyntaxToken ReadUnicodeDelimitedIdentifier(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position += UnicodeDelimitedPrefixLength;
        var token = ReadQuotedIdentifier(triviaStart, triviaCount, start);
        return ExtendWithUescapeClause(token, start);
    }

    private SyntaxToken ReadUnicodeString(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position += UnicodeDelimitedPrefixLength;
        var token = ReadString(triviaStart, triviaCount, start);
        return ExtendWithUescapeClause(token, start);
    }

    private bool IsEscapeStringStart(char ch) =>
        (_flags & SqlOptions.Postgres) != 0 && ch is 'E' or 'e' && Peek() == StringQuote;

    private SyntaxToken ReadEscapeString(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position += TwoCharTokenLength;
        while (_position < _length)
        {
            if (_source[_position] == '\\' && _position + 1 < _length)
            {
                _position += TwoCharTokenLength;
                continue;
            }

            if (_source[_position] != StringQuote)
            {
                _position++;
                continue;
            }

            if (Peek() == StringQuote)
            {
                _position += TwoCharTokenLength;
                continue;
            }

            _position++;
            return new SyntaxToken(SyntaxKind.String, start, _position - start, triviaStart, triviaCount);
        }

        throw new SqlParseException("Unterminated string", start);
    }

    private SyntaxToken? TryReadDollarQuotedString(int triviaStart, int triviaCount)
    {
        var start = _position;
        var tagEnd = FindDollarQuoteTagEnd(start);
        if (tagEnd is null)
        {
            return null;
        }

        var tag = _source[start..(tagEnd.Value + 1)];
        var contentStart = tagEnd.Value + 1;
        var closeIndex = _source.IndexOf(tag, contentStart, _length - contentStart, StringComparison.Ordinal);
        if (closeIndex < 0)
        {
            throw new SqlParseException("Unterminated dollar-quoted string", start);
        }

        _position = closeIndex + tag.Length;
        return new SyntaxToken(SyntaxKind.String, start, _position - start, triviaStart, triviaCount);
    }

    private int? FindDollarQuoteTagEnd(int dollarPosition)
    {
        var index = dollarPosition + 1;
        if (index >= _length)
        {
            return null;
        }

        if (_source[index] == '$')
        {
            return index;
        }

        if (!IsIdentifierStart(_source[index]))
        {
            return null;
        }

        index++;
        while (index < _length && _source[index] != '$' && IsIdentifierPart(_source[index]))
        {
            index++;
        }

        return index < _length && _source[index] == '$' ? index : null;
    }

    private SyntaxToken ExtendWithUescapeClause(SyntaxToken token, int start)
    {
        TryConsumeUescapeClause();
        var length = _position - start;
        return length == token.Length ? token : token with { Length = length };
    }

    private void TryConsumeUescapeClause()
    {
        if ((_flags & SqlOptions.Postgres) == 0)
        {
            return;
        }

        var resume = _position;
        SkipWhitespace();
        if (!TryMatchWordAhead("UESCAPE"))
        {
            _position = resume;
            return;
        }

        SkipWhitespace();
        if (!TryReadEscapeCharLiteralLength(out var length))
        {
            _position = resume;
            return;
        }

        _position += length;
    }

    private bool TryMatchWordAhead(string word)
    {
        if (_position + word.Length > _length
            || !_source.AsSpan(_position, word.Length).Equals(word, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var afterPosition = _position + word.Length;
        if (afterPosition < _length && IsIdentifierPart(_source[afterPosition]))
        {
            return false;
        }

        _position = afterPosition;
        return true;
    }

    private bool TryReadEscapeCharLiteralLength(out int length)
    {
        length = 0;
        const int EscapeCharLiteralLength = 3;
        if (_position + EscapeCharLiteralLength > _length
            || _source[_position] != StringQuote
            || _source[_position + 1] == StringQuote
            || _source[_position + 2] != StringQuote)
        {
            return false;
        }

        length = EscapeCharLiteralLength;
        return true;
    }

    private SyntaxToken ReadString(int triviaStart, int triviaCount, bool prefixed)
    {
        var start = _position;
        if (prefixed)
        {
            _position++;
        }

        return ReadString(triviaStart, triviaCount, start);
    }

    private SyntaxToken ReadString(int triviaStart, int triviaCount, int start)
    {
        _position++;
        while (_position < _length)
        {
            if (_source[_position] != StringQuote)
            {
                _position++;
                continue;
            }

            if (Peek() == StringQuote)
            {
                _position += TwoCharTokenLength;
                continue;
            }

            _position++;
            return new SyntaxToken(SyntaxKind.String, start, _position - start, triviaStart, triviaCount);
        }

        throw new SqlParseException("Unterminated string", start);
    }

    private static bool IsCharacterSetIntroducer(ReadOnlySpan<char> text) =>
        text.Length >= IntroducerNameMinLength && text[0] == Introducer;

    private SyntaxToken ReadQuotedIdentifier(int triviaStart, int triviaCount) =>
        ReadQuotedIdentifier(triviaStart, triviaCount, _position);

    private SyntaxToken ReadQuotedIdentifier(int triviaStart, int triviaCount, int start)
    {
        var quoteStart = _position;
        _position++;
        while (_position < _length)
        {
            if (_source[_position] != IdentifierQuote)
            {
                _position++;
                continue;
            }

            if (Peek() == IdentifierQuote)
            {
                _position += TwoCharTokenLength;
                continue;
            }

            _position++;
            if (_position - quoteStart == TwoCharTokenLength)
            {
                throw new SqlParseException("Empty quoted identifier", start);
            }

            return new SyntaxToken(SyntaxKind.Identifier, start, _position - start, triviaStart, triviaCount);
        }

        throw new SqlParseException("Unterminated quoted identifier", start);
    }

    private char Peek() => _position + 1 < _length ? _source[_position + 1] : '\0';
    private char PeekTwo() => _position + TwoCharTokenLength < _length ? _source[_position + TwoCharTokenLength] : '\0';

    private bool IsPostgres => (_flags & SqlOptions.Postgres) != 0;

    private bool IsSqlServer => (_flags & SqlOptions.SqlServer) != 0;

    private static bool IsPlusOrMinus(char ch) => ch is '+' or '-';

    private static bool IsPgOperatorChar(char ch) =>
        ch is '+' or '-' or '*' or '/' or '<' or '>' or '=' || IsPgSpecialOperatorChar(ch);

    private static bool IsPgSpecialOperatorChar(char ch) =>
        ch is '~' or '!' or '@' or '#' or '%' or '^' or '&' or '|' or '`' or '?';

    private static bool IsWhitespace(char ch) =>
        ch is ' ' or '\t' or '\n' or '\r' || char.IsWhiteSpace(ch);

    private static bool IsIdentifierStart(char ch) =>
        char.IsAsciiLetter(ch) || ch == Introducer || char.IsLetter(ch);

    private static bool IsIdentifierPart(char ch) =>
        char.IsAsciiLetterOrDigit(ch) || ch == Introducer || ch == '$' || char.IsLetterOrDigit(ch);
}
