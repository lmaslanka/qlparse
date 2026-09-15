namespace SqlParser.Tests;

public sealed class FormatTests
{
    [Fact]
    public void Formats_select_from()
    {
        AssertFormatted(
            "select a from t",
            """
            SELECT
                a
            FROM t
            """);
    }

    [Fact]
    public void Formats_multiple_select_columns()
    {
        AssertFormatted(
            "select a,b from t",
            """
            SELECT
                a,
                b
            FROM t
            """);
    }

    [Fact]
    public void Formats_where_equality()
    {
        AssertFormatted(
            "select a from t where x=1",
            """
            SELECT
                a
            FROM t
            WHERE x = 1
            """);
    }

    [Fact]
    public void Formats_where_and_or_with_precedence()
    {
        AssertFormatted(
            "select a from t where x=1 or y=2 and z=3",
            """
            SELECT
                a
            FROM t
            WHERE x = 1 OR y = 2 AND z = 3
            """);
    }

    [Fact]
    public void Skips_line_and_block_comments()
    {
        AssertFormatted(
            """
            select a -- col
            from /* table */ t
            """,
            """
            SELECT
                a
            FROM t
            """);
    }

    [Fact]
    public void Uppercases_keywords_and_lowercases_identifiers()
    {
        AssertFormatted(
            "Select A From T Where X=1",
            """
            SELECT
                a
            FROM t
            WHERE x = 1
            """);
    }

    private static void AssertFormatted(string sql, string expected) =>
        Assert.Equal(expected, Sql.Format(sql));
}
