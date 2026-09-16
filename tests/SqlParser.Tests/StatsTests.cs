namespace SqlParser.Tests;

public sealed class StatsTests
{
    private const int SimpleSelectTokenCount = 4;
    private const int CommentedSelectLineCount = 2;
    private const int TwoMilliseconds = 2;
    private const int OneHundredTwentyMicroseconds = 120;

    [Fact]
    public void Counts_tokens_chars_and_output_for_simple_select()
    {
        var sql = "select a from t";
        var trace = Sql.Trace(sql);

        Assert.Equal(sql.Length, trace.Stats.SourceChars);
        Assert.Equal(1, trace.Stats.SourceLines);
        Assert.Equal(SimpleSelectTokenCount, trace.Stats.TokenCount);
        Assert.Equal(0, trace.Stats.Comments);
        Assert.Equal(trace.Format!.Length, trace.Stats.OutputChars);
        Assert.Null(trace.Stats.FailedStage);
        Assert.NotNull(trace.Stats.Parser);
        Assert.NotNull(trace.Stats.Formatter);
    }

    [Fact]
    public void Counts_comments_and_lines()
    {
        var trace = Sql.Trace("select a -- c\nfrom t");

        Assert.Equal(CommentedSelectLineCount, trace.Stats.SourceLines);
        Assert.Equal(1, trace.Stats.Comments);
        Assert.Null(trace.Stats.FailedStage);
    }

    [Fact]
    public void Records_lexer_failure_without_parser_times()
    {
        var trace = Sql.Trace("select a from t where x ! 1");

        Assert.Equal(SqlStats.LexerStage, trace.Stats.FailedStage);
        Assert.True(trace.Stats.TokenCount > 0);
        Assert.Null(trace.Stats.Parser);
        Assert.Null(trace.Stats.Formatter);
        Assert.Null(trace.Stats.OutputChars);
    }

    [Fact]
    public void Records_parser_failure()
    {
        var trace = Sql.Trace("select from t");

        Assert.Equal(SqlStats.ParserStage, trace.Stats.FailedStage);
        Assert.NotNull(trace.Stats.Parser);
        Assert.Null(trace.Stats.Formatter);
        Assert.Null(trace.Stats.OutputChars);
    }

    [Fact]
    public void Formats_durations_as_ms_or_microseconds()
    {
        Assert.Equal("2 ms", SqlStats.FormatDuration(TimeSpan.FromMilliseconds(TwoMilliseconds)));
        Assert.Equal("1 ms", SqlStats.FormatDuration(TimeSpan.FromMilliseconds(1)));
        Assert.Equal(
            "120 µs",
            SqlStats.FormatDuration(TimeSpan.FromMicroseconds(OneHundredTwentyMicroseconds)));
    }
}
