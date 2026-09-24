namespace QlParse;

internal sealed partial class Parser
{
    private readonly IReadOnlyList<SyntaxToken> _tokens;
    private readonly string _source;
    private int _index;
    private SyntaxToken _current;

    private Parser(IReadOnlyList<SyntaxToken> tokens, string source)
    {
        _tokens = tokens;
        _source = source;
        _current = tokens[0];
        _index = 1;
    }

    public static Query Parse(IReadOnlyList<SyntaxToken> tokens, string source)
    {
        var parser = new Parser(tokens, source);
        var statements = new List<Query> { parser.ParseStatement() };
        var semicolons = new List<SyntaxToken>();
        while (parser._current.Kind == SyntaxKind.Semicolon)
        {
            var semicolon = parser.Advance();
            if (parser._current.Kind == SyntaxKind.EndOfFile)
            {
                if (statements.Count == 1)
                {
                    return statements[0];
                }

                semicolons.Add(semicolon);
                break;
            }

            if (parser._current.Kind == SyntaxKind.Semicolon)
            {
                throw new SqlParseException("Unexpected semicolon", parser._current.Position);
            }

            semicolons.Add(semicolon);
            statements.Add(parser.ParseStatement());
        }

        parser.Expect(SyntaxKind.EndOfFile);
        if (statements.Count == 1)
        {
            return statements[0];
        }

        return new DirectSqlScript
        {
            Span = SourceSpan.From(statements[0].Span, statements[^1].Span),
            Statements = statements,
            Semicolons = semicolons,
        };
    }

    private SyntaxKind NextKind =>
        _index < _tokens.Count ? _tokens[_index].Kind : SyntaxKind.EndOfFile;

    private SyntaxToken Advance()
    {
        var current = _current;
        if (_index < _tokens.Count)
        {
            _current = _tokens[_index];
            _index++;
        }

        return current;
    }

    private SyntaxToken Expect(SyntaxKind kind)
    {
        if (_current.Kind != kind)
        {
            throw new SqlParseException($"Expected {kind}, found {_current.Kind}", _current.Position);
        }

        return Advance();
    }

    private StarExpression ParseStar()
    {
        var star = Advance();
        return new StarExpression { Star = star, Span = star.Span };
    }
}
