using System.Globalization;
using System.Runtime.CompilerServices;

namespace QlParse;

// SQL Server (T-SQL) lexing, reached only under SqlOptions.SqlServer: bracket-delimited identifiers,
// @variables and @@system functions, #temp names, binary and money literals, $pseudo-columns, the
// GO batch separator, and the T-SQL-only operators (compound assignment, !< !>, & ~ %, legacy =*).
// SQL Server has its own token loop: every character it lexes differently is read here first, the rest
// falls through to the ISO tables, and the ISO/Postgres loop carries no T-SQL check at all.
internal sealed partial class Lexer
{
    private const char CloseBracket = ']';
    private const int BatchSeparatorLength = 2;
    private const char LastAsciiChar = '\u007F';

    internal void ReadTsqlTokens(List<SyntaxToken> tokens)
    {
        SyntaxToken token;
        do
        {
            token = NextTsqlToken();
            tokens.Add(token);
        }
        while (token.Kind != SyntaxKind.EndOfFile);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SyntaxToken NextTsqlToken()
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
            return IsEscapeStringStart(ch) ? ReadEscapeString(triviaStart, triviaCount) : ReadTsqlWord(ch, triviaStart, triviaCount);
        }

        if (char.IsAsciiDigit(ch))
        {
            return ch == '0' && Peek() is 'x' or 'X' ? ReadBinaryLiteral(triviaStart, triviaCount) : ReadNumber(triviaStart, triviaCount);
        }

        // `$` that is no money literal or pseudo-column goes through the ISO (or Postgres) dollar handling.
        return TryReadTsqlPunctuation(ch, triviaStart, triviaCount)
            ?? (ch == '$' ? TryReadSpecialToken(triviaStart, triviaCount) : null)
            ?? ReadPunctuationToken(triviaStart, triviaCount);
    }

    // A word: T-SQL keeps only the N'...' prefix (x'..' and b'..' are a name followed by a string alias) and
    // has no U& forms (`U&'x'` is a bitwise AND of a name and a string). E'...' stays with Postgres.
    private SyntaxToken ReadTsqlWord(char ch, int triviaStart, int triviaCount) =>
        ch is 'N' or 'n' && Peek() == StringQuote
            ? ReadString(triviaStart, triviaCount, prefixed: true)
            : ReadTsqlIdentifier(triviaStart, triviaCount);

    // Reads a character T-SQL lexes differently from ISO, or returns null to leave it to the ISO tables. With
    // both dialect flags set, Postgres keeps its own reading of the operator characters the two share.
    private SyntaxToken? TryReadTsqlPunctuation(char ch, int triviaStart, int triviaCount) => ch switch
    {
        '[' => ReadBracketIdentifier(triviaStart, triviaCount),
        AtSign => ReadTsqlVariable(triviaStart, triviaCount),
        IdentifierQuote when IsQuotedIdentifierOff => ReadDoubleQuotedString(triviaStart, triviaCount),
        > LastAsciiChar when IsMoneyStart(ch) => ReadMoneyLiteral(triviaStart, triviaCount),
        _ when IsPostgres => null,
        '$' => IsMoneyAmountStart() ? ReadMoneyLiteral(triviaStart, triviaCount) : TryReadPseudoColumn(triviaStart, triviaCount),
        '=' => Peek() == '*'
            ? ReadTwo(SyntaxKind.EqualsStarToken, triviaStart, triviaCount)
            : ReadSingle(SyntaxKind.EqualsToken, triviaStart, triviaCount),
        '|' => ReadTsqlBar(triviaStart, triviaCount),
        '^' => ReadCompoundOr(SyntaxKind.CaretToken, triviaStart, triviaCount),
        '*' => ReadCompoundOr(SyntaxKind.Star, triviaStart, triviaCount),
        '+' => ReadCompoundOr(SyntaxKind.PlusToken, triviaStart, triviaCount),
        '-' when Peek() == '=' => ReadTwo(SyntaxKind.CompoundAssignmentToken, triviaStart, triviaCount),
        '/' => ReadCompoundOr(SyntaxKind.SlashToken, triviaStart, triviaCount),
        ':' when IsIdentifierStart(Peek()) => ReadSingle(SyntaxKind.ColonToken, triviaStart, triviaCount),
        '!' => TryReadTsqlNotComparison(triviaStart, triviaCount),
        '%' => ReadCompoundOr(SyntaxKind.PercentToken, triviaStart, triviaCount),
        '&' => ReadCompoundOr(SyntaxKind.AmpersandToken, triviaStart, triviaCount),
        '~' => ReadSingle(SyntaxKind.TildeToken, triviaStart, triviaCount),
        '#' => ReadTempObjectName(triviaStart, triviaCount),
        _ => null,
    };

    // [name] with `]]` as an escaped close bracket. Like "name", the token keeps its delimiters.
    internal SyntaxToken ReadBracketIdentifier(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position++;
        while (_position < _length)
        {
            if (_source[_position] != CloseBracket)
            {
                _position++;
                continue;
            }

            if (Peek() == CloseBracket)
            {
                _position += TwoCharTokenLength;
                continue;
            }

            _position++;
            if (_position - start == TwoCharTokenLength)
            {
                throw new SqlParseException("Empty bracket-delimited identifier", start);
            }

            return new SyntaxToken(SyntaxKind.Identifier, start, _position - start, triviaStart, triviaCount);
        }

        throw new SqlParseException("Unterminated bracket-delimited identifier", start);
    }

    // @name variables and parameters, and @@name system functions, as the EmbeddedHost token that
    // SqlOptions.AtParameters produces; T-SQL names may also hold @, # and $ after the first character.
    internal SyntaxToken ReadTsqlVariable(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position++;
        SkipTsqlIdentifierPart();
        if (_position - start == 1)
        {
            throw new SqlParseException($"Unexpected character '{AtSign}'", start);
        }

        return new SyntaxToken(SyntaxKind.EmbeddedHost, start, _position - start, triviaStart, triviaCount);
    }

    // A regular identifier, which in T-SQL may continue through @ and #. A line holding only `GO [count]`
    // (and an optional comment) is the client-side batch separator, never an identifier.
    private SyntaxToken ReadTsqlIdentifier(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position++;
        SkipTsqlIdentifierPart();
        var length = _position - start;
        if (length == BatchSeparatorLength && IsBatchSeparatorAt(start))
        {
            return new SyntaxToken(SyntaxKind.BatchSeparator, start, length, triviaStart, triviaCount);
        }

        var kind = Keyword.Classify(_source.AsSpan(start, length));
        return new SyntaxToken(kind == SyntaxKind.Identifier ? kind : Keyword.TsqlKind(kind), start, length, triviaStart, triviaCount);
    }

    private void SkipTsqlIdentifierPart()
    {
        while (_position < _length && IsTsqlIdentifierPart(_source[_position]))
        {
            _position++;
        }
    }

    private bool IsBatchSeparatorAt(int start) =>
        _source.AsSpan(start, BatchSeparatorLength).Equals(Keyword.TsqlGo, StringComparison.OrdinalIgnoreCase)
        && IsFirstOnLine(start)
        && IsBatchSeparatorLineEnd(start + BatchSeparatorLength);

    private bool IsFirstOnLine(int index)
    {
        var scan = index - 1;
        while (scan >= 0 && _source[scan] is ' ' or '\t')
        {
            scan--;
        }

        return scan < 0 || _source[scan] is '\n' or '\r';
    }

    // After GO: blanks, an optional repeat count, blanks, then a comment, a line break or the end of input.
    private bool IsBatchSeparatorLineEnd(int index)
    {
        var scan = SkipBlanks(index);
        var digits = scan;
        while (scan < _length && char.IsAsciiDigit(_source[scan]))
        {
            scan++;
        }

        if (scan > digits)
        {
            scan = SkipBlanks(scan);
        }

        return scan >= _length || _source[scan] is '\n' or '\r' || IsCommentStartAt(scan);
    }

    private int SkipBlanks(int index)
    {
        var scan = index;
        while (scan < _length && _source[scan] is ' ' or '\t')
        {
            scan++;
        }

        return scan;
    }

    // 0x0A1B binary constants (`0x` alone is the empty binary value).
    private SyntaxToken ReadBinaryLiteral(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position += TwoCharTokenLength;
        while (_position < _length && char.IsAsciiHexDigit(_source[_position]))
        {
            _position++;
        }

        return new SyntaxToken(SyntaxKind.BinaryLiteral, start, _position - start, triviaStart, triviaCount);
    }

    // `$` starts a money literal or a pseudo-column; otherwise (null) it is the ISO DollarToken.
    private bool IsMoneyAmountStart()
    {
        var next = Peek();
        return char.IsAsciiDigit(next) || (next == '.' && char.IsAsciiDigit(PeekTwo()));
    }

    // $12.50, ¥1000, £.5: a currency symbol directly followed by an unsigned decimal amount.
    private SyntaxToken ReadMoneyLiteral(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position++;
        SkipDigits();
        if (_position + 1 < _length && _source[_position] == '.' && char.IsAsciiDigit(_source[_position + 1]))
        {
            _position++;
            SkipDigits();
        }

        return new SyntaxToken(SyntaxKind.MoneyLiteral, start, _position - start, triviaStart, triviaCount);
    }

    private void SkipDigits()
    {
        while (_position < _length && char.IsAsciiDigit(_source[_position]))
        {
            _position++;
        }
    }

    // $IDENTITY, $ROWGUID, $ACTION, $PARTITION and the graph columns lex as identifiers so they work
    // wherever a column name does (`t.$node_id`, `insert into e ($from_id, $to_id)`).
    private SyntaxToken? TryReadPseudoColumn(int triviaStart, int triviaCount)
    {
        var start = _position;
        var end = start + 1;
        while (end < _length && IsIdentifierPart(_source[end]))
        {
            end++;
        }

        if (!Keyword.IsTsqlPseudoColumn(_source.AsSpan(start, end - start)))
        {
            return null;
        }

        _position = end;
        return new SyntaxToken(SyntaxKind.Identifier, start, end - start, triviaStart, triviaCount);
    }

    // `|=` and `||=` are compound assignments; `|` and `||` keep their ISO meaning.
    private SyntaxToken ReadTsqlBar(int triviaStart, int triviaCount)
    {
        if (Peek() == '=')
        {
            return ReadTwo(SyntaxKind.CompoundAssignmentToken, triviaStart, triviaCount);
        }

        if (Peek() != '|')
        {
            return ReadSingle(SyntaxKind.BarToken, triviaStart, triviaCount);
        }

        return PeekTwo() == '='
            ? ReadThree(SyntaxKind.CompoundAssignmentToken, triviaStart, triviaCount)
            : ReadTwo(SyntaxKind.ConcatToken, triviaStart, triviaCount);
    }

    // `op=` is a compound assignment (`*=` doubles as the legacy outer-join comparison); otherwise `op`.
    private SyntaxToken ReadCompoundOr(SyntaxKind single, int triviaStart, int triviaCount) =>
        Peek() == '='
            ? ReadTwo(SyntaxKind.CompoundAssignmentToken, triviaStart, triviaCount)
            : ReadSingle(single, triviaStart, triviaCount);

    // `!<` and `!>` (not less than, not greater than); `!=` is lexed by the ISO table.
    private SyntaxToken? TryReadTsqlNotComparison(int triviaStart, int triviaCount) => Peek() switch
    {
        '<' => ReadTwo(SyntaxKind.NotLessToken, triviaStart, triviaCount),
        '>' => ReadTwo(SyntaxKind.NotGreaterToken, triviaStart, triviaCount),
        _ => null,
    };

    private bool IsMoneyStart(char ch) =>
        CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.CurrencySymbol && IsMoneyAmountStart();

    // #local and ##global temporary object names (a lone `#` is a valid temporary table name).
    private SyntaxToken ReadTempObjectName(int triviaStart, int triviaCount)
    {
        var start = _position;
        _position++;
        SkipTsqlIdentifierPart();
        return new SyntaxToken(SyntaxKind.Identifier, start, _position - start, triviaStart, triviaCount);
    }

    private static bool IsTsqlIdentifierPart(char ch) => IsIdentifierPart(ch) || ch is AtSign or '#';
}
