namespace QlParse.Tests;

public sealed class SpanTests
{
    [Fact]
    public void Identifier_span_matches_token()
    {
        var result = Sql.Parse("select a");
        Assert.Null(result.Error);
        var select = Assert.IsType<SelectStatement>(result.Root);
        var ident = Assert.IsType<IdentifierExpression>(select.SelectList[0].Expression);
        var token = result.Tokens.First(t => t.Kind == SyntaxKind.Identifier);
        Assert.Equal(token.Span, ident.Span);
    }

    [Fact]
    public void Binary_span_covers_expression()
    {
        var result = Sql.Parse("select 1 + 2");
        Assert.Null(result.Error);
        var select = Assert.IsType<SelectStatement>(result.Root);
        var add = Assert.IsType<BinaryExpression>(select.SelectList[0].Expression);
        Assert.Equal("1 + 2", Text(result.Source, add.Span));
    }

    [Fact]
    public void Leading_comment_is_outside_span()
    {
        var result = Sql.Parse("-- c\nselect a");
        Assert.Null(result.Error);
        var select = Assert.IsType<SelectStatement>(result.Root);
        Assert.Equal("select a", Text(result.Source, select.Span));
    }

    [Fact]
    public void Paren_query_span_includes_parens()
    {
        var result = Sql.Parse("(select 1)");
        Assert.Null(result.Error);
        var paren = Assert.IsType<ParenQuery>(result.Root);
        Assert.Equal("(select 1)", Text(result.Source, paren.Span));
    }

    private static string Text(string source, SourceSpan span) =>
        source.Substring(span.Position, span.Length);
}
