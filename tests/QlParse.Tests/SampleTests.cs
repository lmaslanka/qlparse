namespace QlParse.Tests;

public sealed class SampleTests
{
    [Fact]
    public void Parses_sample_sql()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sample.sql"));
        var result = Sql.Parse(source);
        Assert.Null(result.Error);
        Assert.NotNull(result.Root);
        new SqlVisitor().Visit(result.Root);
        Assert.Equal(source.IndexOf("SELECT", StringComparison.Ordinal), result.Root.Span.Position);
    }
}
