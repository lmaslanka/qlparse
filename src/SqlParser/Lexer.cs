namespace SqlParser;

internal sealed class Lexer
{
    private const int CommentDelimiterLength = 2;
    private const int TwoCharTokenLength = 2;
    private const int CharsPerTokenEstimate = 4;
    private const char StringQuote = '\'';

    private readonly string _source;
    private readonly int _length;
    private readonly List<SyntaxTrivia> _trivia = [];
    private int _position;

    public Lexer(string source)
    {
        _source = source;
        _length = source.Length;
    }

    public IReadOnlyList<SyntaxTrivia> Trivia => _trivia;

    public static LexResult LexAll(string source)
    {
        var lexer = new Lexer(source);
        var tokens = new List<SyntaxToken>(Math.Max(1, source.Length / CharsPerTokenEstimate));
        try
        {
            SyntaxToken token;
            do
            {
                token = lexer.Next();
                tokens.Add(token);
            } while (token.Kind != SyntaxKind.EndOfFile);

            return new LexResult
            {
                Tokens = tokens,
                Trivia = lexer.Trivia,
                Error = null,
            };
        }
        catch (SqlParseException ex)
        {
            return new LexResult
            {
                Tokens = tokens,
                Trivia = lexer.Trivia,
                Error = ex,
            };
        }
    }

    public SyntaxToken Next()
    {
        var triviaStart = _trivia.Count;
        ReadTrivia();
        var triviaCount = _trivia.Count - triviaStart;

        if (_position >= _length)
        {
            return new SyntaxToken(SyntaxKind.EndOfFile, _position, 0, triviaStart, triviaCount);
        }

        var ch = _source[_position];
        if (IsIdentifierStart(ch))
        {
            return ReadIdentifierOrKeyword(triviaStart, triviaCount);
        }

        if (char.IsAsciiDigit(ch))
        {
            return ReadNumber(triviaStart, triviaCount);
        }

        return ch switch
        {
            ',' => ReadSingle(SyntaxKind.Comma, triviaStart, triviaCount),
            '=' => ReadSingle(SyntaxKind.EqualsToken, triviaStart, triviaCount),
            '.' => ReadSingle(SyntaxKind.Dot, triviaStart, triviaCount),
            ';' => ReadSingle(SyntaxKind.Semicolon, triviaStart, triviaCount),
            ':' => Peek() == ':'
                ? ReadTwo(SyntaxKind.DoubleColonToken, triviaStart, triviaCount)
                : throw new SqlParseException("Unexpected character ':'", _position),
            '|' => Peek() == '|'
                ? ReadTwo(SyntaxKind.ConcatToken, triviaStart, triviaCount)
                : throw new SqlParseException("Unexpected character '|'", _position),
            '(' => ReadSingle(SyntaxKind.OpenParen, triviaStart, triviaCount),
            ')' => ReadSingle(SyntaxKind.CloseParen, triviaStart, triviaCount),
            '[' => ReadSingle(SyntaxKind.OpenBracket, triviaStart, triviaCount),
            ']' => ReadSingle(SyntaxKind.CloseBracket, triviaStart, triviaCount),
            '>' => Peek() == '='
                ? ReadTwo(SyntaxKind.GreaterOrEqual, triviaStart, triviaCount)
                : ReadSingle(SyntaxKind.GreaterThan, triviaStart, triviaCount),
            '<' => Peek() == '='
                ? ReadTwo(SyntaxKind.LessOrEqual, triviaStart, triviaCount)
                : Peek() == '>'
                    ? ReadTwo(SyntaxKind.NotEqualsToken, triviaStart, triviaCount)
                    : ReadSingle(SyntaxKind.LessThan, triviaStart, triviaCount),
            '!' => Peek() == '='
                ? ReadTwo(SyntaxKind.NotEqualsToken, triviaStart, triviaCount)
                : throw new SqlParseException("Unexpected character '!'", _position),
            '*' => ReadSingle(SyntaxKind.Star, triviaStart, triviaCount),
            '+' => ReadSingle(SyntaxKind.PlusToken, triviaStart, triviaCount),
            '-' => ReadSingle(SyntaxKind.MinusToken, triviaStart, triviaCount),
            '/' => ReadSingle(SyntaxKind.SlashToken, triviaStart, triviaCount),
            StringQuote => ReadString(triviaStart, triviaCount),
            _ => throw new SqlParseException($"Unexpected character '{ch}'", _position),
        };
    }

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
        _position++;
        while (_position < _length && IsIdentifierPart(_source[_position]))
        {
            _position++;
        }

        var text = _source.AsSpan(start, _position - start);
        return new SyntaxToken(Classify(text), start, text.Length, triviaStart, triviaCount);
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

        return new SyntaxToken(SyntaxKind.Number, start, _position - start, triviaStart, triviaCount);
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

    private SyntaxToken ReadString(int triviaStart, int triviaCount)
    {
        var start = _position;
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

    private char Peek() => _position + 1 < _length ? _source[_position + 1] : '\0';

    private static SyntaxKind Classify(ReadOnlySpan<char> text)
    {
        var length = text.Length;
        if (length == Keyword.Distinct.Length)
        {
            return EqualsKeyword(text, Keyword.Distinct) ? SyntaxKind.DistinctKeyword : SyntaxKind.Identifier;
        }

        if (length == Keyword.Intersect.Length)
        {
            if (EqualsKeyword(text, Keyword.Intersect))
            {
                return SyntaxKind.IntersectKeyword;
            }

            return EqualsKeyword(text, Keyword.Recursive) ? SyntaxKind.RecursiveKeyword : SyntaxKind.Identifier;
        }

        if (length == Keyword.Between.Length)
        {
            if (EqualsKeyword(text, Keyword.Between))
            {
                return SyntaxKind.BetweenKeyword;
            }

            return EqualsKeyword(text, Keyword.Natural) ? SyntaxKind.NaturalKeyword : SyntaxKind.Identifier;
        }

        if (length == Keyword.Select.Length)
        {
            if (EqualsKeyword(text, Keyword.Select))
            {
                return SyntaxKind.SelectKeyword;
            }

            if (EqualsKeyword(text, Keyword.Having))
            {
                return SyntaxKind.HavingKeyword;
            }

            if (EqualsKeyword(text, Keyword.Offset))
            {
                return SyntaxKind.OffsetKeyword;
            }

            if (EqualsKeyword(text, Keyword.Exists))
            {
                return SyntaxKind.ExistsKeyword;
            }

            return EqualsKeyword(text, Keyword.Except) ? SyntaxKind.ExceptKeyword : SyntaxKind.Identifier;
        }

        if (length == Keyword.And.Length)
        {
            if (EqualsKeyword(text, Keyword.And))
            {
                return SyntaxKind.AndKeyword;
            }

            if (EqualsKeyword(text, Keyword.Asc))
            {
                return SyntaxKind.AscKeyword;
            }

            if (EqualsKeyword(text, Keyword.Not))
            {
                return SyntaxKind.NotKeyword;
            }

            if (EqualsKeyword(text, Keyword.End))
            {
                return SyntaxKind.EndKeyword;
            }

            if (EqualsKeyword(text, Keyword.All))
            {
                return SyntaxKind.AllKeyword;
            }

            return EqualsKeyword(text, Keyword.Any) ? SyntaxKind.AnyKeyword : SyntaxKind.Identifier;
        }

        if (length == Keyword.Where.Length)
        {
            if (EqualsKeyword(text, Keyword.Where))
            {
                return SyntaxKind.WhereKeyword;
            }

            if (EqualsKeyword(text, Keyword.Array))
            {
                return SyntaxKind.ArrayKeyword;
            }

            if (EqualsKeyword(text, Keyword.Inner))
            {
                return SyntaxKind.InnerKeyword;
            }

            if (EqualsKeyword(text, Keyword.Right))
            {
                return SyntaxKind.RightKeyword;
            }

            if (EqualsKeyword(text, Keyword.Cross))
            {
                return SyntaxKind.CrossKeyword;
            }

            if (EqualsKeyword(text, Keyword.Outer))
            {
                return SyntaxKind.OuterKeyword;
            }

            if (EqualsKeyword(text, Keyword.Using))
            {
                return SyntaxKind.UsingKeyword;
            }

            if (EqualsKeyword(text, Keyword.Group))
            {
                return SyntaxKind.GroupKeyword;
            }

            if (EqualsKeyword(text, Keyword.Order))
            {
                return SyntaxKind.OrderKeyword;
            }

            if (EqualsKeyword(text, Keyword.Limit))
            {
                return SyntaxKind.LimitKeyword;
            }

            return EqualsKeyword(text, Keyword.Union) ? SyntaxKind.UnionKeyword : SyntaxKind.Identifier;
        }

        if (length == Keyword.From.Length)
        {
            if (EqualsKeyword(text, Keyword.From))
            {
                return SyntaxKind.FromKeyword;
            }

            if (EqualsKeyword(text, Keyword.Join))
            {
                return SyntaxKind.JoinKeyword;
            }

            if (EqualsKeyword(text, Keyword.Left))
            {
                return SyntaxKind.LeftKeyword;
            }

            if (EqualsKeyword(text, Keyword.Like))
            {
                return SyntaxKind.LikeKeyword;
            }

            if (EqualsKeyword(text, Keyword.Null))
            {
                return SyntaxKind.NullKeyword;
            }

            if (EqualsKeyword(text, Keyword.Full))
            {
                return SyntaxKind.FullKeyword;
            }

            if (EqualsKeyword(text, Keyword.Desc))
            {
                return SyntaxKind.DescKeyword;
            }

            if (EqualsKeyword(text, Keyword.Case))
            {
                return SyntaxKind.CaseKeyword;
            }

            if (EqualsKeyword(text, Keyword.Cast))
            {
                return SyntaxKind.CastKeyword;
            }

            if (EqualsKeyword(text, Keyword.When))
            {
                return SyntaxKind.WhenKeyword;
            }

            if (EqualsKeyword(text, Keyword.Then))
            {
                return SyntaxKind.ThenKeyword;
            }

            if (EqualsKeyword(text, Keyword.Else))
            {
                return SyntaxKind.ElseKeyword;
            }

            if (EqualsKeyword(text, Keyword.Some))
            {
                return SyntaxKind.SomeKeyword;
            }

            return EqualsKeyword(text, Keyword.With) ? SyntaxKind.WithKeyword : SyntaxKind.Identifier;
        }

        if (length == Keyword.Or.Length)
        {
            if (EqualsKeyword(text, Keyword.Or))
            {
                return SyntaxKind.OrKeyword;
            }

            if (EqualsKeyword(text, Keyword.As))
            {
                return SyntaxKind.AsKeyword;
            }

            if (EqualsKeyword(text, Keyword.In))
            {
                return SyntaxKind.InKeyword;
            }

            if (EqualsKeyword(text, Keyword.Is))
            {
                return SyntaxKind.IsKeyword;
            }

            if (EqualsKeyword(text, Keyword.On))
            {
                return SyntaxKind.OnKeyword;
            }

            return EqualsKeyword(text, Keyword.By) ? SyntaxKind.ByKeyword : SyntaxKind.Identifier;
        }

        return SyntaxKind.Identifier;
    }

    private static bool EqualsKeyword(ReadOnlySpan<char> text, string keyword) =>
        text.Equals(keyword.AsSpan(), StringComparison.OrdinalIgnoreCase);

    private static bool IsWhitespace(char ch) =>
        ch is ' ' or '\t' or '\n' or '\r' || char.IsWhiteSpace(ch);

    private static bool IsIdentifierStart(char ch) =>
        char.IsAsciiLetter(ch) || ch == '_' || char.IsLetter(ch);

    private static bool IsIdentifierPart(char ch) =>
        char.IsAsciiLetterOrDigit(ch) || ch is '_' or '$' || char.IsLetterOrDigit(ch);
}
