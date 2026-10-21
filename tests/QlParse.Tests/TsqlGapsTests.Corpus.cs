namespace QlParse.Tests;

// A corpus of real-world-style SQL Server scripts in TsqlCorpus: SSMS "Script As" output, stored procedures with
// TRY/CATCH and transactions, migrations, dynamic SQL, MERGE upserts, reporting queries, cursors, triggers,
// functions, security, temporal / graph / JSON / vector usage. Every script's first line names its expected shape:
// the top-level statement types, `,` between the statements of a batch and ` | ` between GO-separated batches.
// idioms.sql holds short single-batch cases, each after a `-- case: shape` line. The default visitor walks every tree.
public sealed partial class TsqlGapsTests
{
    private const string CorpusFolder = "TsqlCorpus";
    private const string IdiomsFile = "idioms.sql";
    private const string ExpectPrefix = "-- expect: ";
    private const string CasePrefix = "-- case: ";
    private const int CorpusScripts = 45;
    private const int HeadAndRest = 2;

    [Fact]
    public void Parses_the_script_corpus()
    {
        var files = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, CorpusFolder), "*.sql")
            .Where(file => Path.GetFileName(file) != IdiomsFile)
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.Equal(CorpusScripts, files.Count);
        var failures = new List<string>();
        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            var expected = source.Split('\n', HeadAndRest)[0][ExpectPrefix.Length..].TrimEnd();
            var actual = Shape(source);
            if (actual != expected)
            {
                failures.Add($"{Path.GetFileName(file)}: expected {expected}, got {actual}");
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void Parses_the_idiom_cases()
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, CorpusFolder, IdiomsFile));
        var cases = text.Split($"\n{CasePrefix}", StringSplitOptions.RemoveEmptyEntries).Skip(1).ToList();
        Assert.True(cases.Count > 150);
        var failures = new List<string>();
        foreach (var item in cases)
        {
            var lines = item.Split('\n', HeadAndRest);
            var expected = lines[0].TrimEnd();
            var sql = lines[1];
            var actual = Shape(sql);
            if (actual != expected)
            {
                failures.Add($"{sql.Trim()}: expected {expected}, got {actual}");
            }
        }

        Assert.Empty(failures);
    }

    // The parsed shape; the default visitor also walks every parsed tree.
    private static string Shape(string sql)
    {
        var result = Sql.Parse(sql, SqlOptions.SqlServer);
        if (result.Error is { } error)
        {
            return $"error at {error.Position}: {error.Message}";
        }

        new SqlVisitor().Visit(result.Root!);
        return result.Root switch
        {
            TsqlScript script => $"Script({string.Join(" | ", script.Batches.Select(batch => Names(batch.Statements)))})",
            TsqlBatch batch => $"Batch({Names(batch.Statements)})",
            { } root => root.GetType().Name,
            _ => string.Empty,
        };
    }

    private static string Names(IEnumerable<Query> statements) => string.Join(",", statements.Select(statement => statement.GetType().Name));
}
