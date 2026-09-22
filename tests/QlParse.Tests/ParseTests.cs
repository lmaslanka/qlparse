namespace QlParse.Tests;

public sealed class ParseTests
{
    [Fact]
    public void Parses_select_from()
    {
        var tree = Sql.Parse("select a from t");
        Assert.Null(tree.Error);
        Assert.IsType<SelectStatement>(tree.Root);
        Assert.Equal("select a from t", tree.Source);
        Assert.NotEmpty(tree.Tokens);
    }

    [Fact]
    public void Lex_error_sets_error()
    {
        var result = Sql.Parse("select a from t where x ! 1");
        Assert.NotNull(result.Error);
        Assert.Null(result.Root);
    }

    [Fact]
    public void Parse_error_sets_error()
    {
        var result = Sql.Parse("select from t");
        Assert.NotNull(result.Error);
        Assert.Null(result.Root);
    }
}
