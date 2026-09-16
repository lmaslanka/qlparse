namespace SqlParser.Tests;

public sealed class TraceTests
{
    [Fact]
    public void Traces_lexer_parser_and_format()
    {
        var trace = Sql.Trace("select a from t");

        Assert.Equal(
            """
            SelectKeyword "select" @0+6
            Identifier "a" @7+1
            FromKeyword "from" @9+4
            Identifier "t" @14+1
            EndOfFile "" @15+0
            """,
            trace.Lexer);
        Assert.Equal(
            """
            SelectStatement
              SelectKeyword "select" @0+6
              SelectList
                IdentifierExpression "a" @7+1
              FromKeyword "from" @9+4
              TableReference
                Name "t" @14+1
            """,
            trace.Parser);
        Assert.Equal(
            """
            SELECT
                a
            FROM t
            """,
            trace.Format);
        Assert.Null(trace.Error);
    }

    [Fact]
    public void Traces_derived_table()
    {
        var trace = Sql.Trace("select a from (select b from t) x");

        Assert.Equal(
            """
            SelectStatement
              SelectKeyword "select" @0+6
              SelectList
                IdentifierExpression "a" @7+1
              FromKeyword "from" @9+4
              DerivedTable
                Alias "x" @32+1
            """,
            trace.Parser);
        Assert.Null(trace.Error);
    }

    [Fact]
    public void Attaches_comments_to_the_next_token()
    {
        var trace = Sql.Trace("select a -- c\nfrom t");

        Assert.Equal(
            """
            SelectKeyword "select" @0+6
            Identifier "a" @7+1
            FromKeyword "from" @14+4
              LineCommentTrivia "-- c" @9+4
            Identifier "t" @19+1
            EndOfFile "" @20+0
            """,
            trace.Lexer);
        Assert.Null(trace.Error);
    }

    [Fact]
    public void Skips_dumps_when_not_requested()
    {
        var trace = Sql.Trace("select a from t", dumpLexer: false, dumpParser: false, format: true, stats: false);

        Assert.Equal(string.Empty, trace.Lexer);
        Assert.Null(trace.Parser);
        Assert.Equal(
            """
            SELECT
                a
            FROM t
            """,
            trace.Format);
        Assert.Null(trace.Error);
    }

    [Fact]
    public void Lexer_only_does_not_parse()
    {
        var trace = Sql.Trace("select a", dumpLexer: true, dumpParser: false, format: false, stats: false);

        Assert.False(string.IsNullOrEmpty(trace.Lexer));
        Assert.Null(trace.Parser);
        Assert.Null(trace.Format);
        Assert.Null(trace.Error);
    }

    [Fact]
    public void Keeps_lexer_dump_when_parse_fails()
    {
        var trace = Sql.Trace("select from t");

        Assert.False(string.IsNullOrEmpty(trace.Lexer));
        Assert.Null(trace.Parser);
        Assert.Null(trace.Format);
        Assert.NotNull(trace.Error);
    }

    [Fact]
    public void Keeps_lexer_dump_when_lex_fails()
    {
        var trace = Sql.Trace("select a from t where x ! 1");

        Assert.False(string.IsNullOrEmpty(trace.Lexer));
        Assert.Null(trace.Parser);
        Assert.Null(trace.Format);
        Assert.NotNull(trace.Error);
    }
}
