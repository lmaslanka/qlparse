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
        var query = parser.ParseQuery();
        if (parser._current.Kind == SyntaxKind.Semicolon)
        {
            parser.Advance();
        }

        parser.Expect(SyntaxKind.EndOfFile);

        return query;
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
}
