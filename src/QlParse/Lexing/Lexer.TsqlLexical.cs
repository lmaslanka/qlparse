using System.Globalization;

namespace QlParse;

// SQL Server (T-SQL) lexing, reached only under SqlOptions.SqlServer: bracket-delimited identifiers,
// @variables and @@system functions, #temp names, binary and money literals, $pseudo-columns, the
// GO batch separator, and the T-SQL-only operators (compound assignment, !< !>, & ~ %, legacy =*).
internal sealed partial class Lexer
{
    private const char CloseBracket = ']';
    private const int BatchSeparatorLength = 2;

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

    // The rest of a regular identifier, which in T-SQL may continue through @ and #. A line holding only
    // `GO [count]` (and an optional comment) is the client-side batch separator, never an identifier.
    internal SyntaxToken ReadTsqlIdentifierTail(int start, int triviaStart, int triviaCount)
    {
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

    // 0x0A1B binary constants (`0x` alone is the empty binary value), checked once a number has read its
    // leading digits, so other numbers pay one length test.
    internal bool IsBinaryLiteralMarker(int start) =>
        _position - start == 1
        && _source[start] == '0'
        && _position < _length
        && _source[_position] is 'x' or 'X'
        && IsSqlServer;

    internal SyntaxToken ReadBinaryLiteral(int start, int triviaStart, int triviaCount)
    {
        _position++;
        while (_position < _length && char.IsAsciiHexDigit(_source[_position]))
        {
            _position++;
        }

        return new SyntaxToken(SyntaxKind.BinaryLiteral, start, _position - start, triviaStart, triviaCount);
    }

    // `$` is a Postgres positional parameter or dollar-quoted string, or a T-SQL money literal or
    // pseudo-column; otherwise (null here) the ISO DollarToken.
    internal SyntaxToken? TryReadDialectDollar(int triviaStart, int triviaCount)
    {
        if (IsPostgres)
        {
            return char.IsAsciiDigit(Peek())
                ? ReadPositionalParameter(triviaStart, triviaCount)
                : TryReadDollarQuotedString(triviaStart, triviaCount);
        }

        return IsMoneyAmountStart() ? ReadMoneyLiteral(triviaStart, triviaCount) : TryReadPseudoColumn(triviaStart, triviaCount);
    }

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

    internal SyntaxToken ReadDialectEquals(int triviaStart, int triviaCount)
    {
        if (IsPostgres)
        {
            return ReadPgOperator(triviaStart, triviaCount);
        }

        return Peek() == '*'
            ? ReadTwo(SyntaxKind.EqualsStarToken, triviaStart, triviaCount)
            : ReadSingle(SyntaxKind.EqualsToken, triviaStart, triviaCount);
    }

    internal SyntaxToken ReadDialectBar(int triviaStart, int triviaCount)
    {
        if (IsPostgres)
        {
            return ReadPgOperator(triviaStart, triviaCount);
        }

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

    internal SyntaxToken ReadDialectCaret(int triviaStart, int triviaCount)
    {
        if (IsPostgres)
        {
            return ReadPgOperator(triviaStart, triviaCount);
        }

        return ReadCompoundOr(SyntaxKind.CaretToken, triviaStart, triviaCount);
    }

    internal SyntaxToken ReadDialectArithmetic(char ch, int triviaStart, int triviaCount)
    {
        if (IsPostgres)
        {
            return ReadPgOperator(triviaStart, triviaCount);
        }

        return ch switch
        {
            '*' => ReadCompoundOr(SyntaxKind.Star, triviaStart, triviaCount),
            '+' => ReadCompoundOr(SyntaxKind.PlusToken, triviaStart, triviaCount),
            '-' => Peek() == '=' ? ReadTwo(SyntaxKind.CompoundAssignmentToken, triviaStart, triviaCount) : ReadMinus(triviaStart, triviaCount),
            '/' => ReadCompoundOr(SyntaxKind.SlashToken, triviaStart, triviaCount),
            _ => ReadSingle(SyntaxKind.QuestionMark, triviaStart, triviaCount),
        };
    }

    // `op=` is a compound assignment (`*=` doubles as the legacy outer-join comparison); otherwise `op`.
    private SyntaxToken ReadCompoundOr(SyntaxKind single, int triviaStart, int triviaCount) =>
        Peek() == '='
            ? ReadTwo(SyntaxKind.CompoundAssignmentToken, triviaStart, triviaCount)
            : ReadSingle(single, triviaStart, triviaCount);

    // `!<` and `!>` (not less than, not greater than); `!=` is lexed by the ISO table.
    internal SyntaxToken ReadTsqlNotComparison(int triviaStart, int triviaCount)
    {
        if (IsSqlServer)
        {
            var next = Peek();
            if (next == '<')
            {
                return ReadTwo(SyntaxKind.NotLessToken, triviaStart, triviaCount);
            }

            if (next == '>')
            {
                return ReadTwo(SyntaxKind.NotGreaterToken, triviaStart, triviaCount);
            }
        }

        throw new SqlParseException("Unexpected character '!'", _position);
    }

    // Characters ISO rejects outright; reached only after every ISO and Postgres table has declined the
    // character, so the other dialects pay nothing for this table.
    internal SyntaxToken? TryReadTsqlOnlyToken(char ch, int triviaStart, int triviaCount)
    {
        if (!IsSqlServer)
        {
            return null;
        }

        return ch switch
        {
            '%' => ReadCompoundOr(SyntaxKind.PercentToken, triviaStart, triviaCount),
            '&' => ReadCompoundOr(SyntaxKind.AmpersandToken, triviaStart, triviaCount),
            '~' => ReadSingle(SyntaxKind.TildeToken, triviaStart, triviaCount),
            '#' => ReadTempObjectName(triviaStart, triviaCount),
            _ when IsMoneyStart(ch) => ReadMoneyLiteral(triviaStart, triviaCount),
            _ => null,
        };
    }

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
