namespace QlParse;

// SET QUOTED_IDENTIFIER OFF (workstream H): under SqlOptions.SqlServer | SqlOptions.QuotedIdentifierOff a
// double-quoted run is a string literal, not a delimited identifier. Without both flags `"` lexes as before.
internal sealed partial class Lexer
{
    internal bool IsQuotedIdentifierOff => (_flags & SqlOptions.QuotedIdentifierOff) != 0 && IsSqlServer;

    // "text" with `""` as an escaped quote; like '...', the String token keeps its delimiters and may be empty.
    internal SyntaxToken ReadDoubleQuotedString(int triviaStart, int triviaCount)
    {
        var start = _position;
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
            return new SyntaxToken(SyntaxKind.String, start, _position - start, triviaStart, triviaCount);
        }

        throw new SqlParseException("Unterminated string", start);
    }
}
