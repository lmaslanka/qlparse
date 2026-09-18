namespace QlParse.Tests;

public sealed class ParseTests
{
    [Fact]
    public void Parses_select_from()
    {
        var tree = Sql.Parse("select a from t");
        Assert.IsType<SelectStatement>(tree.Root);
        Assert.Equal("select a from t", tree.Source);
        Assert.NotEmpty(tree.Tokens);
    }

    [Fact]
    public void Lex_error_throws()
    {
        Assert.Throws<SqlParseException>(() => Sql.Parse("select a from t where x ! 1"));
    }

    [Fact]
    public void Parse_error_throws()
    {
        Assert.Throws<SqlParseException>(() => Sql.Parse("select from t"));
    }
}
