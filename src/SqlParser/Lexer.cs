using System.Collections.Immutable;

namespace SqlParser;

internal static class Lexer
{
    private const int CommentDelimiterLength = 2;

    private static readonly Dictionary<string, SyntaxKind> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        [Keyword.Select] = SyntaxKind.SelectKeyword,
        [Keyword.From] = SyntaxKind.FromKeyword,
        [Keyword.Where] = SyntaxKind.WhereKeyword,
        [Keyword.And] = SyntaxKind.AndKeyword,
        [Keyword.Or] = SyntaxKind.OrKeyword,
    };

    public static ImmutableArray<SyntaxToken> Lex(string source)
    {
        var reader = new Reader(source);
        var tokens = ImmutableArray.CreateBuilder<SyntaxToken>();
        SyntaxToken token;
        do
        {
            token = reader.Next();
            tokens.Add(token);
        } while (token.Kind != SyntaxKind.EndOfFile);

        return tokens.ToImmutable();
    }

    private sealed class Reader(string source)
    {
        private int _position;

        public SyntaxToken Next()
        {
            var trivia = ReadTrivia();
            if (_position >= source.Length)
            {
                return new SyntaxToken(SyntaxKind.EndOfFile, string.Empty, _position, trivia);
            }

            var ch = source[_position];
            if (IsIdentifierStart(ch))
            {
                return ReadIdentifierOrKeyword(trivia);
            }

            if (char.IsAsciiDigit(ch))
            {
                return ReadNumber(trivia);
            }

            return ch switch
            {
                ',' => ReadSingle(SyntaxKind.Comma, trivia),
                '=' => ReadSingle(SyntaxKind.EqualsToken, trivia),
                _ => throw new SqlParseException($"Unexpected character '{ch}'", _position),
            };
        }

        private ImmutableArray<SyntaxTrivia> ReadTrivia()
        {
            var trivia = ImmutableArray.CreateBuilder<SyntaxTrivia>();
            while (_position < source.Length)
            {
                var ch = source[_position];
                if (char.IsWhiteSpace(ch))
                {
                    trivia.Add(ReadWhitespace());
                    continue;
                }

                if (ch == '-' && Peek() == '-')
                {
                    trivia.Add(ReadLineComment());
                    continue;
                }

                if (ch == '/' && Peek() == '*')
                {
                    trivia.Add(ReadBlockComment());
                    continue;
                }

                break;
            }

            return trivia.ToImmutable();
        }

        private SyntaxTrivia ReadWhitespace()
        {
            var start = _position;
            while (_position < source.Length && char.IsWhiteSpace(source[_position]))
            {
                _position++;
            }

            return new SyntaxTrivia(SyntaxKind.WhitespaceTrivia, source[start.._position]);
        }

        private SyntaxTrivia ReadLineComment()
        {
            var start = _position;
            _position += CommentDelimiterLength;
            while (_position < source.Length && source[_position] is not '\n' and not '\r')
            {
                _position++;
            }

            return new SyntaxTrivia(SyntaxKind.LineCommentTrivia, source[start.._position]);
        }

        private SyntaxTrivia ReadBlockComment()
        {
            var start = _position;
            _position += CommentDelimiterLength;
            var depth = 1;
            while (_position < source.Length && depth > 0)
            {
                if (source[_position] == '/' && Peek() == '*')
                {
                    depth++;
                    _position += CommentDelimiterLength;
                    continue;
                }

                if (source[_position] == '*' && Peek() == '/')
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

            return new SyntaxTrivia(SyntaxKind.BlockCommentTrivia, source[start.._position]);
        }

        private SyntaxToken ReadIdentifierOrKeyword(ImmutableArray<SyntaxTrivia> trivia)
        {
            var start = _position;
            _position++;
            while (_position < source.Length && IsIdentifierPart(source[_position]))
            {
                _position++;
            }

            var text = source[start.._position];
            var kind = Keywords.GetValueOrDefault(text, SyntaxKind.Identifier);
            return new SyntaxToken(kind, text, start, trivia);
        }

        private SyntaxToken ReadNumber(ImmutableArray<SyntaxTrivia> trivia)
        {
            var start = _position;
            while (_position < source.Length && char.IsAsciiDigit(source[_position]))
            {
                _position++;
            }

            return new SyntaxToken(SyntaxKind.Number, source[start.._position], start, trivia);
        }

        private SyntaxToken ReadSingle(SyntaxKind kind, ImmutableArray<SyntaxTrivia> trivia)
        {
            var start = _position;
            _position++;
            return new SyntaxToken(kind, source[start.._position], start, trivia);
        }

        private char Peek() => _position + 1 < source.Length ? source[_position + 1] : '\0';

        private static bool IsIdentifierStart(char ch) => char.IsLetter(ch) || ch == '_';

        private static bool IsIdentifierPart(char ch) =>
            char.IsLetterOrDigit(ch) || ch is '_' or '$';
    }
}
