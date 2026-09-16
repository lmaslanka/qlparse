using SqlParser.Cli;

namespace SqlParser.Tests;

public sealed class FolderFormatTests
{
    private const string NestedFolder = "nested";

    [Fact]
    public void Formats_sql_files_recursively()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "a.sql"), "select a from t");
            Directory.CreateDirectory(Path.Combine(dir.FullName, NestedFolder));
            File.WriteAllText(Path.Combine(dir.FullName, NestedFolder, "b.sql"), "select b from u");
            File.WriteAllText(Path.Combine(dir.FullName, "skip.txt"), "select a from t");

            var errors = new StringWriter();
            var code = FolderFormat.Run(dir.FullName, errors);

            Assert.Equal(0, code);
            Assert.Equal(
                """
                SELECT
                    a
                FROM t
                """,
                File.ReadAllText(Path.Combine(dir.FullName, "a.sql")));
            Assert.Equal(
                """
                SELECT
                    b
                FROM u
                """,
                File.ReadAllText(Path.Combine(dir.FullName, NestedFolder, "b.sql")));
            Assert.Equal("select a from t", File.ReadAllText(Path.Combine(dir.FullName, "skip.txt")));
            Assert.Contains("Formatted 2 files (2 changed, 0 failed).", errors.ToString());
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [Fact]
    public void Continues_after_parse_error()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var good = Path.Combine(dir.FullName, "good.sql");
            var bad = Path.Combine(dir.FullName, "bad.sql");
            File.WriteAllText(good, "select a from t");
            File.WriteAllText(bad, "select from t");

            var errors = new StringWriter();
            var code = FolderFormat.Run(dir.FullName, errors);

            Assert.Equal(1, code);
            Assert.Equal(
                """
                SELECT
                    a
                FROM t
                """,
                File.ReadAllText(good));
            Assert.Equal("select from t", File.ReadAllText(bad));
            Assert.Contains("bad.sql", errors.ToString());
            Assert.Contains("Formatted 2 files (1 changed, 1 failed).", errors.ToString());
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [Fact]
    public void Reports_empty_directory()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var errors = new StringWriter();
            var code = FolderFormat.Run(dir.FullName, errors);

            Assert.Equal(0, code);
            Assert.Contains("Formatted 0 files (0 changed, 0 failed).", errors.ToString());
        }
        finally
        {
            dir.Delete(true);
        }
    }
}
