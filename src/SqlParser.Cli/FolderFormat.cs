using SqlParser;

namespace SqlParser.Cli;

public static class FolderFormat
{
    private const string SqlPattern = "*.sql";

    public static int Run(string directory, TextWriter errors)
    {
        var files = Directory.GetFiles(directory, SqlPattern, new EnumerationOptions
        {
            RecurseSubdirectories = true,
            MatchCasing = MatchCasing.CaseInsensitive,
            IgnoreInaccessible = true,
        });

        var changed = 0;
        var failed = 0;
        var errorLock = new object();

        Parallel.ForEach(
            files,
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            file =>
            {
                var sql = File.ReadAllText(file);
                string formatted;
                try
                {
                    formatted = Sql.Format(sql);
                }
                catch (SqlParseException ex)
                {
                    Interlocked.Increment(ref failed);
                    lock (errorLock)
                    {
                        errors.WriteLine($"{file}: {ex.Message} at {ex.Position}");
                    }

                    return;
                }

                if (formatted == sql)
                {
                    return;
                }

                File.WriteAllText(file, formatted);
                Interlocked.Increment(ref changed);
            });

        errors.WriteLine($"Formatted {files.Length} files ({changed} changed, {failed} failed).");
        return failed == 0 ? 0 : 1;
    }
}
